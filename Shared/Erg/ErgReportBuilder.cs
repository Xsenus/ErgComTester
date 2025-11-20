using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestDocument = QuestPDF.Fluent.Document;
using WordColor = DocumentFormat.OpenXml.Wordprocessing.Color;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using SkiaSharp;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using DrawingColor = System.Drawing.Color;
using DrawingFont = System.Drawing.Font;
using DrawingImage = System.Drawing.Image;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace ErgData;

public static class ErgReportBuilder
{
    private const string DefaultClientDeviceName = "Электроретинограф  Ветеринарный  МЛ-210 VET \"Микролюкс\"";
    private const int GraphImagePixelHeight = 540;
    private const int GraphImagePixelWidth = 900;
    private const float GraphImageTargetHeightInches = 3.0f;
    private const float GraphImageTargetWidthInches = 3.6f;
    private const float GraphRenderDpi = GraphImagePixelWidth / GraphImageTargetWidthInches;
    private const double HeaderLineSpacingPoints = 2d;
    private const double HeaderTitleSpacingPoints = 12d;
    private const string MissingMeasurementText = "— —";

    private static SKTypeface? _skTypeface;

    static ErgReportBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static void AppendClientDescription(Body body, string description)
    {
        body.Append(CreateParagraph("Заключение:", fontSizePt: 12, bold: true, spacingBefore: TwipsFromPoints(18), spacingAfter: TwipsFromPoints(4)));
        body.Append(CreateParagraph(description, fontSizePt: 11));
    }

    private static void AppendClientNormParagraph(TableCell cell, string value)
    {
        var formatted = FormatNormForClient(value);
        if (formatted == null)
            return;

        cell.Append(CreateParagraph(formatted, fontSizePt: 9, colorHex: "666666", justification: JustificationValues.Center));
    }

    private static void AppendClientWaveParagraphs(TableCell cell, string label, WaveDisplay display)
    {
        cell.Append(CreateClientWaveValuesTable(label, display));
    }

    private static void AppendGraphSection(Body body, MainDocumentPart mainPart, ErgTest test, ref uint imageId)
    {
        body.Append(CreateParagraph("Графические данные", fontSizePt: 11, bold: true, spacingBefore: TwipsFromPoints(12), spacingAfter: TwipsFromPoints(4)));
        body.Append(CreateParagraph($"Правый глаз: {test.RightEye.GraphCount} граф., левый глаз: {test.LeftEye.GraphCount} граф.", fontSizePt: 11));

        var styleDescriptions = DescribeGraphStyles(test);
        if (styleDescriptions.Length > 0)
        {
            body.Append(CreateParagraph("Стили графиков: " + string.Join("; ", styleDescriptions), fontSizePt: 10));
        }

        var rightGraph = TryRenderGraphImage(test, test.RightEye);
        var leftGraph = TryRenderGraphImage(test, test.LeftEye);

        if (rightGraph == null && leftGraph == null)
        {
            body.Append(CreateParagraph("Графические данные недоступны.", fontSizePt: 10, italic: true, colorHex: "666666"));
        }
        else
        {
            body.Append(CreateGraphTable(mainPart, rightGraph, leftGraph, ref imageId));
        }

        var rightPreview = BuildGraphPreview(test.RightEye.GraphsNormalized, test.GraphNumPoints);
        if (!string.Equals(rightPreview, "нет данных", StringComparison.OrdinalIgnoreCase))
        {
            body.Append(CreateParagraph("Первые 10 точек (правый глаз, график 1): " + rightPreview, fontSizePt: 10));
        }

        var leftPreview = BuildGraphPreview(test.LeftEye.GraphsNormalized, test.GraphNumPoints);
        if (!string.Equals(leftPreview, "нет данных", StringComparison.OrdinalIgnoreCase))
        {
            body.Append(CreateParagraph("Первые 10 точек (левый глаз, график 1): " + leftPreview, fontSizePt: 10));
        }
    }

    private static void AppendTextWithBreaks(Run run, string text)
    {
        var cleaned = (text ?? string.Empty).Replace("\r", string.Empty);
        var lines = cleaned.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            run.Append(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
            if (i < lines.Length - 1)
                run.Append(new Break());
        }
    }

    private static void ApplyPageMargins(Body body, double leftCm, double rightCm, double? topCm, double? bottomCm)
    {
        if (body == null)
            return;

        var sectionProps = body.Elements<SectionProperties>().LastOrDefault();
        if (sectionProps == null)
        {
            sectionProps = new SectionProperties();
            body.Append(sectionProps);
        }

        var pageMargin = sectionProps.GetFirstChild<PageMargin>();
        if (pageMargin == null)
        {
            pageMargin = new PageMargin();
            sectionProps.Append(pageMargin);
        }

        pageMargin.Left = UInt32Value.FromUInt32((uint)TwipsFromCentimeters(leftCm));
        pageMargin.Right = UInt32Value.FromUInt32((uint)TwipsFromCentimeters(rightCm));

        if (topCm.HasValue)
            pageMargin.Top = new Int32Value(TwipsFromCentimeters(topCm.Value));

        if (bottomCm.HasValue)
            pageMargin.Bottom = new Int32Value(TwipsFromCentimeters(bottomCm.Value));

        if (!body.Elements<SectionProperties>().Contains(sectionProps))
            body.Append(sectionProps);
    }

    private static string BoolText(bool value) => value ? "Да" : "Нет";

    private static AxisTickSet BuildAxisTickSet(double min, double max, double anchor, int valueStep, int lineStep, bool allowNegativeMajorTicks = true)
    {
        if (double.IsNaN(min) || double.IsNaN(max) || double.IsInfinity(min) || double.IsInfinity(max))
            return new AxisTickSet(Array.Empty<AxisTick>(), Array.Empty<AxisTick>(), Array.Empty<double>());

        double range = max - min;
        if (range <= 0)
        {
            var anchorTick = new AxisTick(anchor, 0, true, true);
            return new AxisTickSet(new[] { anchorTick }, new[] { anchorTick }, new[] { anchor });
        }

        var ticks = new Dictionary<long, AxisTick>();
        const double keyScale = 1_000_000d;

        void RegisterTick(double position, double displayValue, bool isMajor, bool isAnchor)
        {
            if (!allowNegativeMajorTicks && !isAnchor && displayValue < 0)
            {
                isMajor = false;
            }

            long key = (long)Math.Round(position * keyScale);
            if (ticks.TryGetValue(key, out var existing))
            {
                var updated = existing with
                {
                    IsMajor = existing.IsMajor || isMajor,
                    IsAnchor = existing.IsAnchor || isAnchor,
                    DisplayValue = (existing.IsAnchor || isAnchor) ? 0 : existing.DisplayValue
                };
                ticks[key] = updated;
                return;
            }

            double display = isAnchor ? 0 : displayValue;
            ticks[key] = new AxisTick(position, display, isMajor || isAnchor, isAnchor);
        }

        RegisterTick(anchor, 0, true, true);

        double majorStep = valueStep > 0 ? valueStep : DetermineAxisStep(min, max, valueStep, lineStep);
        if (majorStep <= 0)
        {
            majorStep = range > 0 ? CalculateNiceStep(range / 8.0) : 0;
        }
        if (majorStep <= 0)
            majorStep = range / 6.0;
        if (majorStep <= 0)
            majorStep = 1;

        double minorStep = lineStep > 0 ? lineStep : 0;
        if (minorStep > 0 && minorStep >= majorStep)
        {
            minorStep = majorStep / Math.Max(2, Math.Round(minorStep / majorStep));
        }
        else if (minorStep <= 0 && majorStep > 0)
        {
            minorStep = majorStep / 5.0;
        }
        if (minorStep <= 0 || double.IsInfinity(minorStep) || double.IsNaN(minorStep))
            minorStep = 0;

        void AddTicks(double step, bool positiveDirection, bool markMajor)
        {
            if (step <= 0)
                return;

            double limit = positiveDirection ? max : min;
            double direction = positiveDirection ? 1.0 : -1.0;
            double value = anchor;
            int guard = 0;
            while (guard++ < 4096)
            {
                value += step * direction;
                if (positiveDirection)
                {
                    if (value > limit + 1e-6)
                        break;
                }
                else
                {
                    if (value < limit - 1e-6)
                        break;
                }

                double display = value - anchor;
                RegisterTick(value, display, markMajor, false);
            }
        }

        if (majorStep > 0)
        {
            AddTicks(majorStep, true, true);
            AddTicks(majorStep, false, true);
        }

        if (minorStep > 0)
        {
            AddTicks(minorStep, true, false);
            AddTicks(minorStep, false, false);
        }

        var ordered = ticks.Values
            .Where(t => IsWithinAxis(t.Position, min, max))
            .OrderBy(t => t.Position)
            .ToArray();

        var majorTicks = ordered.Where(t => t.IsMajor).ToArray();
        var gridLines = majorTicks.Select(t => t.Position).ToArray();

        return new AxisTickSet(ordered, majorTicks, gridLines);
    }

    private static string[] BuildClinicHeaderLines(string? clinicName)
    {
        const int headerLineCount = 4;
        var lines = new string[headerLineCount];
        Array.Fill(lines, string.Empty);

        if (string.IsNullOrWhiteSpace(clinicName))
            return lines;

        var normalized = clinicName.Replace("\r\n", "\n").Replace('\r', '\n');
        var parts = normalized.Split(new[] { '\n' }, headerLineCount, StringSplitOptions.None);
        for (int i = 0; i < headerLineCount && i < parts.Length; i++)
        {
            lines[i] = parts[i] ?? string.Empty;
        }

        return lines;
    }

    private static XDocument BuildContentTypesManifest(IReadOnlyCollection<ZipArchiveEntry> entries)
    {
        var ns = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types");
        var root = new XElement(ns + "Types");

        root.Add(new XElement(ns + "Default",
            new XAttribute("Extension", "rels"),
            new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")));

        root.Add(new XElement(ns + "Default",
            new XAttribute("Extension", "xml"),
            new XAttribute("ContentType", "application/xml")));

        foreach (var extension in GetImageExtensions(entries))
        {
            if (TryGetImageContentType(extension, out var contentType))
            {
                root.Add(new XElement(ns + "Default",
                    new XAttribute("Extension", extension),
                    new XAttribute("ContentType", contentType)));
            }
        }

        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        overrides[NormalizePartName("/word/document.xml")] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";

        foreach (var (part, contentType) in GetWellKnownOverrides())
        {
            if (PackagePartExists(entries, part))
            {
                overrides[NormalizePartName(part)] = contentType;
            }
        }

        foreach (var (part, contentType) in overrides.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            root.Add(new XElement(ns + "Override",
                new XAttribute("PartName", part),
                new XAttribute("ContentType", contentType)));
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root);
    }

    private static XDocument BuildCorePropertiesDocument(string? title)
    {
        var cp = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/metadata/core-properties");
        var dc = XNamespace.Get("http://purl.org/dc/elements/1.1/");
        var dcterms = XNamespace.Get("http://purl.org/dc/terms/");
        var dcmitype = XNamespace.Get("http://purl.org/dc/dcmitype/");
        var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

        var now = DateTime.UtcNow;
        var formatted = now.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        var root = new XElement(cp + "coreProperties",
            new XAttribute(XNamespace.Xmlns + "cp", cp),
            new XAttribute(XNamespace.Xmlns + "dc", dc),
            new XAttribute(XNamespace.Xmlns + "dcterms", dcterms),
            new XAttribute(XNamespace.Xmlns + "dcmitype", dcmitype),
            new XAttribute(XNamespace.Xmlns + "xsi", xsi),
            new XElement(dc + "title", string.IsNullOrWhiteSpace(title) ? "ERG Report" : title),
            new XElement(dc + "creator", "MicroluxErgConnect"),
            new XElement(cp + "lastModifiedBy", "MicroluxErgConnect"),
            new XElement(dcterms + "created",
                new XAttribute(xsi + "type", "dcterms:W3CDTF"),
                formatted),
            new XElement(dcterms + "modified",
                new XAttribute(xsi + "type", "dcterms:W3CDTF"),
                formatted)
        );

        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root);
    }

    private static XDocument BuildExtendedPropertiesDocument()
    {
        var ep = XNamespace.Get("http://schemas.openxmlformats.org/officeDocument/2006/extended-properties");
        var vt = XNamespace.Get("http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes");

        var root = new XElement(ep + "Properties",
            new XAttribute(XNamespace.Xmlns + "vt", vt),
            new XElement(ep + "Application", "MicroluxErgConnect"),
            new XElement(ep + "DocSecurity", "0"),
            new XElement(ep + "ScaleCrop", "false"),
            new XElement(ep + "Company", string.Empty),
            new XElement(ep + "LinksUpToDate", "false"),
            new XElement(ep + "SharedDoc", "false"),
            new XElement(ep + "HyperlinksChanged", "false"),
            new XElement(ep + "AppVersion", "16.0000")
        );

        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root);
    }

    private static string BuildGraphPreview(double[][]? graphs, int declaredPoints)
    {
        if (graphs == null || graphs.Length == 0 || graphs[0] == null)
            return "нет данных";

        var samples = graphs[0];
        if (samples == null || samples.Length == 0)
            return "нет данных";

        var count = declaredPoints <= 0 ? samples.Length : Math.Min(samples.Length, declaredPoints);
        count = Math.Min(count, 10);
        if (count <= 0)
            return "нет данных";

        return string.Join(", ", samples.Take(count).Select(v => v.ToString("0.###", CultureInfo.InvariantCulture)));
    }

    private static GraphMarker[] BuildMarkers(EyeData eye, double xMin, double xMax)
    {
        static bool TryCreateMarker(byte? value, double xMin, double xMax, GraphMarkerKind kind, out GraphMarker marker)
        {
            marker = default!;
            if (!value.HasValue)
                return false;

            double position = value.Value;
            if (position < xMin || position > xMax)
                return false;

            marker = new GraphMarker(kind, position);
            return true;
        }

        var markers = new List<GraphMarker>(2);
        if (TryCreateMarker(eye.AWaveMarker, xMin, xMax, GraphMarkerKind.AWave, out var aMarker))
            markers.Add(aMarker);
        if (TryCreateMarker(eye.BWaveMarker, xMin, xMax, GraphMarkerKind.BWave, out var bMarker))
            markers.Add(bMarker);

        return markers.ToArray();
    }

    private static GraphWaveLevel[] BuildWaveLevels(EyeData eye, double[][] graphs, int curves, double graphDt, double flashOffset)
    {
        static bool TryCreateLevel(GraphMarkerKind kind, byte? markerMs, double[][] graphs, int curves, double graphDt, double flashOffset, out GraphWaveLevel level)
        {
            level = default!;
            if (!markerMs.HasValue)
                return false;

            if (graphDt <= 0)
                return false;

            var series = SelectSeries(graphs, curves);
            if (series == null || series.Length == 0)
                return false;

            double rawIndex = (markerMs.Value + flashOffset) / graphDt;
            if (double.IsNaN(rawIndex) || double.IsInfinity(rawIndex))
                return false;

            int length = series.Length;
            if (rawIndex < 0 || rawIndex > length - 1)
                return false;

            int i0 = (int)Math.Floor(rawIndex);
            int i1 = (int)Math.Ceiling(rawIndex);
            i0 = Math.Clamp(i0, 0, length - 1);
            i1 = Math.Clamp(i1, 0, length - 1);

            double v0 = series[i0];
            double v1 = series[i1];
            if (double.IsNaN(v0) || double.IsInfinity(v0) || double.IsNaN(v1) || double.IsInfinity(v1))
                return false;

            double t = i1 == i0 ? 0 : rawIndex - i0;
            double value = v0 + (v1 - v0) * t;
            level = new GraphWaveLevel(kind, value);
            return true;
        }

        static double[]? SelectSeries(double[][] graphs, int curves)
        {
            int limit = Math.Min(curves, graphs.Length);
            for (int i = 0; i < limit; i++)
            {
                var series = graphs[i];
                if (series is { Length: > 0 })
                    return series;
            }

            return null;
        }

        var result = new List<GraphWaveLevel>(2);
        if (TryCreateLevel(GraphMarkerKind.AWave, eye.AWaveMarker, graphs, curves, graphDt, flashOffset, out var aLevel))
            result.Add(aLevel);
        if (TryCreateLevel(GraphMarkerKind.BWave, eye.BWaveMarker, graphs, curves, graphDt, flashOffset, out var bLevel))
            result.Add(bLevel);

        return result.ToArray();
    }

    private static void BuildPatientReportLegacyPdf(ErgPatient patient, string pdfPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath, ReportTemplate template)
    {
        using var renderer = new LegacyPdfRenderer(patient, pdfPath, deviceInfo, clinicName, rawFilePath, template);
        renderer.Build();
    }

    private static void BuildPatientReportQuestPdfClassic(ErgPatient patient, string pdfPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath)
    {
        var headerTitle = string.IsNullOrWhiteSpace(clinicName)
            ? "Отчет по результатам ЭРГ-исследования сетчатки"
            : clinicName;

        QuestDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Spacing(4);
                    column.Item().Text(headerTitle).FontSize(16).SemiBold();
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"ID пациента: {patient.PatientId}");
                        row.RelativeItem().AlignRight().Text($"Животное: {FormatAnimal(patient.Animal)}");
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Дата/время исследования: {patient.TestDateTime}");
                        var device = deviceInfo != null
                            ? $"Прибор: {deviceInfo.DeviceName}, ПО: {deviceInfo.SoftwareRev}"
                            : string.Empty;
                        if (!string.IsNullOrWhiteSpace(device))
                        {
                            row.RelativeItem().AlignRight().Text(device);
                        }
                    });
                    column.Item().Text($"Количество тестов: {patient.Tests.Count} (в блоке указано: {patient.TotalNumTests})");
                });

                page.Content().Column(column =>
                {
                    column.Spacing(18);

                    for (int i = 0; i < patient.Tests.Count; i++)
                    {
                        var test = patient.Tests[i];
                        column.Item().Component(new TestComponent(i + 1, test));
                    }

                    if (!string.IsNullOrWhiteSpace(patient.Description))
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(desc =>
                        {
                            desc.Item().Text("Автоматическое заключение").SemiBold();
                            desc.Item().Text(patient.Description).FontSize(10).WrapAnywhere();
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(pdfPath);
    }

    private static void BuildPatientReportQuestPdfClient(ErgPatient patient, string pdfPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath)
    {
        var reportTitle = !string.IsNullOrWhiteSpace(deviceInfo?.ReportName)
            ? deviceInfo!.ReportName!
            : "Отчет по результатам ЭРГ-исследования сетчатки";

        var reportVersion = GetApplicationVersion();

        QuestDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                const float mmToPoints = 72f / 25.4f;
                var margin = 20f * mmToPoints;
                page.Margin(margin);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(11));

                var clinicHeaderLines = PrepareClinicHeaderLines(BuildClinicHeaderLines(clinicName));

                page.Header().Column(column =>
                {
                    column.Spacing(4);

                    column.Item().Element(header =>
                    {
                        header.ShowOnce().Column(headerColumn =>
                        {
                            headerColumn.Spacing(0);

                            for (int i = 0; i < clinicHeaderLines.Length; i++)
                            {
                                var line = clinicHeaderLines[i];
                                var bottomPadding = i == clinicHeaderLines.Length - 1
                                    ? (float)HeaderTitleSpacingPoints
                                    : (float)HeaderLineSpacingPoints;

                                headerColumn.Item().MinHeight(12)
                                    .PaddingBottom(bottomPadding)
                                    .AlignCenter().AlignMiddle()
                                    .Text(text =>
                                    {
                                        text.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(10));
                                        text.AlignCenter();
                                        text.Span(string.IsNullOrWhiteSpace(line) ? "\u00A0" : line);
                                    });
                            }

                            headerColumn.Item().AlignCenter().Text(reportTitle).FontSize(14).SemiBold();
                        });
                    });

                    column.Item().PaddingBottom(6).Component(new ClientInfoComponent(patient, deviceInfo));
                });

                page.Content().Column(column =>
                {
                    column.Spacing(12);

                    for (int i = 0; i < patient.Tests.Count; i++)
                    {
                        column.Item().Component(new ClientTestComponent(i + 1, patient.Tests[i]));
                    }

                    if (!string.IsNullOrWhiteSpace(patient.Description))
                    {
                        column.Item().Component(new ClientDescriptionComponent(patient.Description));
                    }

                });

                page.Footer().Row(row =>
                {
                    row.Spacing(6);

                    var versionText = !string.IsNullOrWhiteSpace(reportVersion) && reportVersion != "—"
                        ? $"Версия отчета: {reportVersion}"
                        : string.Empty;

                    row.RelativeItem().AlignLeft().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                        text.Span(string.IsNullOrEmpty(versionText) ? string.Empty : versionText);
                    });

                    row.RelativeItem().AlignRight().Text(txt =>
                    {
                        txt.DefaultTextStyle(style => style.FontSize(9).FontColor(Colors.Grey.Darken1));
                        txt.Span("Стр. ");
                        txt.CurrentPageNumber();
                        txt.Span(" из ");
                        txt.TotalPages();
                    });
                });
            });
        }).GeneratePdf(pdfPath);
    }

    private static void BuildPatientWordReportClassic(ErgPatient patient, string docxPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath)
    {
        var headerTitle = string.IsNullOrWhiteSpace(clinicName)
            ? "Отчет по результатам ЭРГ-исследования сетчатки"
            : clinicName;

        using (var document = WordprocessingDocument.Create(docxPath, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new WordDocument(new Body());
            EnsureDefaultStyles(mainPart);
            var body = mainPart.Document.Body ?? throw new InvalidOperationException("Не удалось создать тело документа Word.");

            body.Append(CreateParagraph(headerTitle, fontSizePt: 16, bold: true, justification: JustificationValues.Center, spacingAfter: TwipsFromPoints(12)));
            body.Append(CreateHeaderTable(patient, deviceInfo));

            var classicHeaderInfo = CreateClientInfoTable(patient, deviceInfo);
            EnsureClientHeaderRepeat(mainPart, body, classicHeaderInfo);

            uint imageId = 1;

            for (int i = 0; i < patient.Tests.Count; i++)
            {
                var test = patient.Tests[i];

                body.Append(CreateParagraph($"Тест №{i + 1}: {test.TestName}", fontSizePt: 13, bold: true, spacingBefore: TwipsFromPoints(16), spacingAfter: TwipsFromPoints(4)));
                body.Append(CreateParagraph($"Точек: {test.GraphNumPoints}, Δt: {test.GraphDt} мс, дискрет/мкВ: {test.GraphDiscrPerMkV}", fontSizePt: 11));
                body.Append(CreateParagraph($"Вспышка: {test.GraphFlashPosition} мс", fontSizePt: 11));
                body.Append(CreateParagraph($"Диапазон X: {test.GraphXScaleMin}…{test.GraphXScaleMax} мс (шаг {test.GraphXValueStep})", fontSizePt: 11));
                body.Append(CreateParagraph($"Диапазон Y: {test.GraphYScaleMin}…{test.GraphYScaleMax} мкВ (шаг {test.GraphYValueStep})", fontSizePt: 11));
                body.Append(CreateParagraph($"a-волна: {(test.AWaveExists ? "есть" : "нет")}, нормы ms: {FormatRange(test.AWaveMsNormalMin, test.AWaveMsNormalMax)}, мкВ: {FormatRange(test.AWaveMkVNormalMin, test.AWaveMkVNormalMax)}", fontSizePt: 11));
                body.Append(CreateParagraph($"b-волна нормы ms: {FormatRange(test.BWaveMsNormalMin, test.BWaveMsNormalMax)}, мкВ: {FormatRange(test.BWaveMkVNormalMin, test.BWaveMkVNormalMax)}", fontSizePt: 11));

                body.Append(CreateMeasurementTable(test));
                AppendGraphSection(body, mainPart, test, ref imageId);
            }

            if (!string.IsNullOrWhiteSpace(patient.Description))
            {
                body.Append(CreateDescriptionTable(patient.Description));
            }

            ApplyPageMargins(body, leftCm: 2.0, rightCm: 2.0, topCm: 2.0, bottomCm: 2.0);

            EnsureDocumentPropertiesParts(document, headerTitle);

            mainPart.Document.Save();
        }

        NormalizeWordContentTypes(docxPath);
    }

    private static void BuildPatientWordReportClient(ErgPatient patient, string docxPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath)
    {
        using (var document = WordprocessingDocument.Create(docxPath, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new WordDocument(new Body());
            EnsureDefaultStyles(mainPart);
            var body = mainPart.Document.Body ?? throw new InvalidOperationException("Не удалось создать тело документа Word.");

            var clinicHeaderLines = PrepareClinicHeaderLines(BuildClinicHeaderLines(clinicName));

            for (int i = 0; i < clinicHeaderLines.Length; i++)
            {
                var line = clinicHeaderLines[i];
                var spacingAfter = i == clinicHeaderLines.Length - 1
                    ? 0
                    : TwipsFromPoints(HeaderLineSpacingPoints);
                var text = string.IsNullOrWhiteSpace(line) ? "\u00A0" : line;
                body.Append(CreateParagraph(
                    text,
                    fontSizePt: 10,
                    bold: false,
                    justification: JustificationValues.Center,
                    spacingAfter: spacingAfter));
            }

            var reportTitle = !string.IsNullOrWhiteSpace(deviceInfo?.ReportName)
                ? deviceInfo!.ReportName!
                : "Отчет по результатам ЭРГ-исследования сетчатки";
            // Между шапкой и заголовком оставляем ровно одну пустую строку для совпадения с PDF.
            body.Append(CreateParagraph(
                reportTitle,
                fontSizePt: 14,
                bold: true,
                justification: JustificationValues.Center,
                spacingBefore: TwipsFromPoints(HeaderTitleSpacingPoints),
                spacingAfter: TwipsFromPoints(8)));
            var infoTable = CreateClientInfoTable(patient, deviceInfo);
            EnsureClientHeaderRepeat(mainPart, body, infoTable);
            body.Append(infoTable);

            uint imageId = 1;

            for (int i = 0; i < patient.Tests.Count; i++)
            {
                var testTable = CreateClientTestTable(mainPart, patient.Tests[i], i + 1, ref imageId);
                body.Append(testTable);
            }

            if (!string.IsNullOrWhiteSpace(patient.Description))
            {
                AppendClientDescription(body, patient.Description);
            }

            var version = GetApplicationVersion();
            if (!string.IsNullOrWhiteSpace(version) && version != "—")
            {
                body.Append(CreateParagraph($"Версия отчета: {version}", fontSizePt: 8, colorHex: "777777", justification: JustificationValues.Center, spacingBefore: TwipsFromPoints(12)));
            }

            ApplyPageMargins(body, leftCm: 2.0, rightCm: 2.0, topCm: 2.0, bottomCm: 2.0);

            EnsureDocumentPropertiesParts(document, reportTitle);

            mainPart.Document.Save();
        }

        NormalizeWordContentTypes(docxPath);
    }

    private static WaveDisplay BuildWaveDisplay(ErgTest test, EyeData eye, WaveKind wave)
    {
        var measurement = wave == WaveKind.A
            ? new WaveMeasurement(GetFirstValue(eye.AWaveMs), GetFirstValue(eye.AWaveMkV))
            : new WaveMeasurement(GetFirstValue(eye.BWaveMs), GetFirstValue(eye.BWaveMkV));

        var msNorm = wave == WaveKind.A
            ? FormatRange(test.AWaveMsNormalMin, test.AWaveMsNormalMax)
            : FormatRange(test.BWaveMsNormalMin, test.BWaveMsNormalMax);
        var mkvNorm = wave == WaveKind.A
            ? FormatRange(test.AWaveMkVNormalMin, test.AWaveMkVNormalMax)
            : FormatRange(test.BWaveMkVNormalMin, test.BWaveMkVNormalMax);

        var msText = FormatPrimaryMeasurement(measurement.Ms, "мс");
        var mkvText = FormatPrimaryMeasurement(measurement.MkV, "мкВ");

        return new WaveDisplay(eye.IsFlat, msText, mkvText, msNorm, mkvNorm);
    }

    private static double CalculateNiceStep(double roughStep)
    {
        if (roughStep <= 0)
            return 0;

        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
        var normalized = roughStep / magnitude;
        double stepNormalized = normalized switch
        {
            <= 1 => 1,
            <= 2 => 2,
            <= 5 => 5,
            _ => 10
        };

        return stepNormalized * magnitude;
    }

    private static TableCell CreateClientEyeGraphCell(MainDocumentPart mainPart, ErgTest test, EyeData eye, int index, string suffix, ref uint imageId)
    {
        var props = new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top });
        props.Append(new TableCellMargin
        {
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            TopMargin = new TopMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "80", Type = TableWidthUnitValues.Dxa }
        });

        var cell = new TableCell(props);

        var graph = TryRenderGraphImage(test, eye);
        if (graph != null)
        {
            var drawing = CreateImageDrawing(mainPart, graph, $"client-{suffix}-{index}", ref imageId, maxWidthInches: GraphImageTargetWidthInches, maxHeightInches: 2.2);
            var paragraph = new Paragraph(new Run(drawing))
            {
                ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Center })
            };
            cell.Append(paragraph);
        }
        else
        {
            cell.Append(CreateParagraph("Нет данных", fontSizePt: 10, italic: true, colorHex: "666666", justification: JustificationValues.Center));
        }

        return cell;
    }

    private static TableCell CreateClientEyeHeaderCell(string label, EyeData eye)
    {
        var props = new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
        props.Append(new TableCellMargin
        {
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            TopMargin = new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "120", Type = TableWidthUnitValues.Dxa }
        });

        var cell = new TableCell(props);
        var quality = FormatQualityCompact(eye.QualityIndex);
        var text = quality != null ? $"{label} {quality}" : label;
        cell.Append(CreateParagraph(text, fontSizePt: 11, bold: true, justification: JustificationValues.Center, spacingAfter: TwipsFromPoints(4)));
        return cell;
    }

    private static TableCell CreateClientEyeSummaryCell(ErgTest test, EyeData eye)
    {
        var props = new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top });
        props.Append(new TableCellMargin
        {
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            TopMargin = new TopMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "20", Type = TableWidthUnitValues.Dxa }
        });

        var cell = new TableCell(props);

        if (eye.IsFlat)
        {
            cell.Append(CreateParagraph("FLAT", fontSizePt: 26, bold: true, justification: JustificationValues.Center, spacingAfter: TwipsFromPoints(4)));
            return cell;
        }

        if (!EyeHasUsableMeasurements(eye))
        {
            cell.Append(CreateParagraph("Нет данных", fontSizePt: 10, italic: true, colorHex: "666666", justification: JustificationValues.Center));
            return cell;
        }

        bool hasContent = false;
        foreach (var (waveLabel, kind) in GetClientWaveOrder(test))
        {
            var display = BuildWaveDisplay(test, eye, kind);
            if (IsWaveDisplayEmpty(display))
                continue;

            AppendClientWaveParagraphs(cell, waveLabel, display);
            hasContent = true;
        }

        if (!hasContent)
        {
            cell.Append(CreateParagraph("Нет данных", fontSizePt: 10, italic: true, colorHex: "666666", justification: JustificationValues.Center));
        }

        return cell;
    }

    private static TableCell CreateClientHeaderCell(string text, int gridSpan)
    {
        var props = new TableCellProperties(
            new GridSpan { Val = gridSpan },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
            new Shading { Fill = "CCCCCC", Val = ShadingPatternValues.Clear, Color = "000000" }
        );
        props.Append(new TableCellMargin
        {
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            TopMargin = new TopMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "60", Type = TableWidthUnitValues.Dxa }
        });

        var cell = new TableCell(props);
        cell.Append(CreateParagraph(text, fontSizePt: 12, bold: true, justification: JustificationValues.Center));
        return cell;
    }

    private static Table CreateClientInfoTable(ErgPatient patient, CommonInfo? deviceInfo)
    {
        var table = new Table(
            new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Nil },
                    new LeftBorder { Val = BorderValues.Nil },
                    new BottomBorder { Val = BorderValues.Nil },
                    new RightBorder { Val = BorderValues.Nil },
                    new InsideHorizontalBorder { Val = BorderValues.Nil },
                    new InsideVerticalBorder { Val = BorderValues.Nil }
                ),
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableLook { Val = "04A0", FirstRow = true, LastRow = false, NoHorizontalBand = false, NoVerticalBand = false }
            ),
            new TableGrid(new GridColumn { Width = "5000" })
        );

        table.Append(new TableRow(CreateInfoCell($"ID пациента: {patient.PatientId} ({FormatAnimal(patient.Animal)})", JustificationValues.Left)));
        table.Append(new TableRow(CreateInfoCell($"Дата и время исследования: {FormatClientDateTime(patient.TestDateTime)}", JustificationValues.Left)));

        table.Append(new TableRow(CreateInfoCell($"Оборудование: {GetClientDeviceName(deviceInfo)}", JustificationValues.Left)));

        var software = GetClientSoftwareVersion(deviceInfo);
        if (!string.IsNullOrEmpty(software))
        {
            table.Append(new TableRow(CreateInfoCell($"Версия ПО: {software}", JustificationValues.Left)));
        }

        return table;
    }

    private static void EnsureClientHeaderRepeat(MainDocumentPart mainPart, Body body, Table infoTable)
    {
        if (mainPart == null || body == null || infoTable == null)
            return;

        var defaultHeaderPart = mainPart.AddNewPart<HeaderPart>();
        var defaultHeader = new Header();
        defaultHeader.Append(infoTable.CloneNode(true));
        defaultHeaderPart.Header = defaultHeader;

        var firstHeaderPart = mainPart.AddNewPart<HeaderPart>();
        var firstHeader = new Header(new Paragraph(new Run(new Text(string.Empty))));
        firstHeaderPart.Header = firstHeader;

        var sectionProps = body.Elements<SectionProperties>().LastOrDefault();
        if (sectionProps == null)
        {
            sectionProps = new SectionProperties();
            body.Append(sectionProps);
        }

        sectionProps.RemoveAllChildren<HeaderReference>();
        sectionProps.RemoveAllChildren<TitlePage>();
        sectionProps.Append(new TitlePage());
        sectionProps.Append(new HeaderReference { Type = HeaderFooterValues.First, Id = mainPart.GetIdOfPart(firstHeaderPart) });
        sectionProps.Append(new HeaderReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(defaultHeaderPart) });
    }

    private static Table CreateClientTestTable(MainDocumentPart mainPart, ErgTest test, int index, ref uint imageId)
    {
        var table = new Table(
            new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Nil },
                    new LeftBorder { Val = BorderValues.Nil },
                    new BottomBorder { Val = BorderValues.Nil },
                    new RightBorder { Val = BorderValues.Nil },
                    new InsideHorizontalBorder { Val = BorderValues.Nil },
                    new InsideVerticalBorder { Val = BorderValues.Nil }
                ),
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableLook { Val = "04A0", FirstRow = true, LastRow = false, NoHorizontalBand = false, NoVerticalBand = false }
            ),
            new TableGrid(new GridColumn { Width = "2500" }, new GridColumn { Width = "2500" })
        );

        var headerRow = new TableRow();
        headerRow.Append(CreateClientHeaderCell(FormatClientTestTitle(index, test), gridSpan: 2));
        table.Append(headerRow);

        var eyeHeaderRow = new TableRow();
        eyeHeaderRow.Append(CreateClientEyeHeaderCell("Правый глаз", test.RightEye));
        eyeHeaderRow.Append(CreateClientEyeHeaderCell("Левый глаз", test.LeftEye));
        table.Append(eyeHeaderRow);

        var summaryRow = new TableRow();
        summaryRow.Append(CreateClientEyeSummaryCell(test, test.RightEye));
        summaryRow.Append(CreateClientEyeSummaryCell(test, test.LeftEye));
        table.Append(summaryRow);

        var graphRow = new TableRow();
        graphRow.Append(CreateClientEyeGraphCell(mainPart, test, test.RightEye, index, "right", ref imageId));
        graphRow.Append(CreateClientEyeGraphCell(mainPart, test, test.LeftEye, index, "left", ref imageId));
        table.Append(graphRow);

        return table;
    }

    private static TableCell CreateClientWaveFlatCell(int gridSpan)
    {
        var props = new TableCellProperties(
            new GridSpan { Val = gridSpan },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );
        props.Append(new TableCellMargin
        {
            TopMargin = new TopMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa }
        });

        var cell = new TableCell(props);
        cell.Append(CreateParagraph("FLAT", fontSizePt: 26, bold: true, justification: JustificationValues.Center));
        return cell;
    }

    private static TableCell CreateClientWaveLabelCell(string label)
    {
        var props = new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
        props.Append(new TableCellMargin
        {
            TopMargin = new TopMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
            LeftMargin = new LeftMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "20", Type = TableWidthUnitValues.Dxa }
        });

        var cell = new TableCell(props);
        var text = string.IsNullOrWhiteSpace(label) ? string.Empty : label;
        cell.Append(CreateParagraph(text, fontSizePt: 11, bold: false, justification: JustificationValues.Left));
        return cell;
    }

    private static TableCell CreateClientWaveValueCell(string value, string norm, bool expand = false)
    {
        var cellProps = new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );
        cellProps.Append(new TableCellMargin
        {
            TopMargin = new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            LeftMargin = new LeftMargin { Width = expand ? "100" : "60", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = expand ? "100" : "60", Type = TableWidthUnitValues.Dxa }
        });

        var cell = new TableCell { TableCellProperties = cellProps };

        cell.Append(CreateMeasurementParagraph(value));

        var formatted = FormatNormForClient(norm);
        if (formatted != null)
        {
            cell.Append(CreateParagraph(formatted, fontSizePt: 9, colorHex: "666666", justification: JustificationValues.Center));
        }

        return cell;
    }

    private static Table CreateClientWaveValuesTable(string label, WaveDisplay display)
    {
        bool hasLabel = !string.IsNullOrWhiteSpace(label);
        var table = new Table(
            new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "4800" },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Nil },
                    new LeftBorder { Val = BorderValues.Nil },
                    new BottomBorder { Val = BorderValues.Nil },
                    new RightBorder { Val = BorderValues.Nil },
                    new InsideHorizontalBorder { Val = BorderValues.Nil },
                    new InsideVerticalBorder { Val = BorderValues.Nil }
                )
            ),
            hasLabel
                ? new TableGrid(new GridColumn { Width = "1200" }, new GridColumn { Width = "1800" }, new GridColumn { Width = "1800" })
                : new TableGrid(new GridColumn { Width = "2400" }, new GridColumn { Width = "2400" })
        );

        var row = new TableRow();
        if (hasLabel)
        {
            row.Append(CreateClientWaveLabelCell(label));

            if (display.IsFlat)
            {
                row.Append(CreateClientWaveFlatCell(gridSpan: 2));
            }
            else
            {
                row.Append(CreateClientWaveValueCell(display.MsValue, display.MsNorm));
                row.Append(CreateClientWaveValueCell(display.MkVValue, display.MkVNorm));
            }
        }
        else
        {
            if (display.IsFlat)
            {
                row.Append(CreateClientWaveFlatCell(gridSpan: 2));
            }
            else
            {
                row.Append(CreateClientWaveValueCell(display.MsValue, display.MsNorm, expand: true));
                row.Append(CreateClientWaveValueCell(display.MkVValue, display.MkVNorm, expand: true));
            }
        }

        table.Append(row);

        return table;
    }

    private static Table CreateDescriptionTable(string description)
    {
        var table = new Table(
            new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 8 },
                    new LeftBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 8 },
                    new BottomBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 8 },
                    new RightBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 8 },
                    new InsideHorizontalBorder { Val = BorderValues.Nil },
                    new InsideVerticalBorder { Val = BorderValues.Nil }
                ),
                new TableCellMarginDefault(
                    new TopMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                    new TableCellLeftMargin { Type = TableWidthValues.Dxa, Width = 80 },
                    new BottomMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                    new TableCellRightMargin { Type = TableWidthValues.Dxa, Width = 80 }
                )
            )
        );

        var cellProps = new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto });
        var cell = new TableCell(cellProps);
        cell.Append(CreateParagraph("Автоматическое заключение", fontSizePt: 11, bold: true));
        cell.Append(CreateParagraph(description, fontSizePt: 10));
        table.Append(new TableRow(cell));
        return table;
    }

    private static TableCell CreateGraphHeaderCell(string text)
    {
        var props = new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
            new Shading { Val = ShadingPatternValues.Clear, Fill = "DDDDDD", Color = "000000" }
        );
        var margin = new TableCellMargin
        {
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            TopMargin = new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa }
        };
        props.Append(margin);

        var cell = new TableCell(props);
        cell.Append(CreateParagraph(text, fontSizePt: 11, bold: true, justification: JustificationValues.Center));
        return cell;
    }

    private static TableCell CreateGraphImageCell(MainDocumentPart mainPart, GraphImage? image, string name, ref uint imageId)
    {
        var props = new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
        var margin = new TableCellMargin
        {
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            TopMargin = new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa }
        };
        props.Append(margin);

        var cell = new TableCell(props);

        if (image != null)
        {
            var drawing = CreateImageDrawing(mainPart, image, name, ref imageId, maxWidthInches: GraphImageTargetWidthInches, maxHeightInches: GraphImageTargetHeightInches);
            var paragraph = new Paragraph(new Run(drawing))
            {
                ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Center })
            };
            cell.Append(paragraph);
        }
        else
        {
            cell.Append(CreateParagraph("Нет данных", fontSizePt: 10, italic: true, colorHex: "666666", justification: JustificationValues.Center));
        }

        return cell;
    }

    private static Table CreateGraphTable(MainDocumentPart mainPart, GraphImage? rightGraph, GraphImage? leftGraph, ref uint imageId)
    {
        var table = new Table(
            new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableLook { Val = "04A0", FirstRow = true, LastRow = false, NoHorizontalBand = false, NoVerticalBand = false }
            ),
            new TableGrid(new GridColumn { Width = "2500" }, new GridColumn { Width = "2500" })
        );

        var headerRow = new TableRow();
        headerRow.Append(CreateGraphHeaderCell("Правый глаз"));
        headerRow.Append(CreateGraphHeaderCell("Левый глаз"));
        table.Append(headerRow);

        if (rightGraph == null && leftGraph == null)
        {
            var cellProps = new TableCellProperties(
                new GridSpan { Val = 2 },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
            );
            cellProps.Append(new TableCellMargin
            {
                TopMargin = new TopMargin { Width = "120", Type = TableWidthUnitValues.Dxa },
                BottomMargin = new BottomMargin { Width = "120", Type = TableWidthUnitValues.Dxa },
                LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa }
            });

            var emptyCell = new TableCell(cellProps);
            emptyCell.Append(CreateParagraph("Нет данных", fontSizePt: 10, italic: true, colorHex: "666666", justification: JustificationValues.Center));

            var row = new TableRow();
            row.Append(emptyCell);
            table.Append(row);
        }
        else
        {
            var imageRow = new TableRow();
            imageRow.Append(CreateGraphImageCell(mainPart, rightGraph, "right-eye", ref imageId));
            imageRow.Append(CreateGraphImageCell(mainPart, leftGraph, "left-eye", ref imageId));
            table.Append(imageRow);
        }

        return table;
    }

    private static Table CreateHeaderTable(ErgPatient patient, CommonInfo? deviceInfo)
    {
        var table = new Table(
            new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableLook { Val = "04A0", FirstRow = true, LastRow = false, NoHorizontalBand = false, NoVerticalBand = false }
            ),
            new TableGrid(new GridColumn { Width = "5000" }, new GridColumn { Width = "5000" })
        );

        table.Append(new TableRow(
            CreateInfoCell($"ID пациента: {patient.PatientId}", JustificationValues.Left),
            CreateInfoCell($"Животное: {FormatAnimal(patient.Animal)}", JustificationValues.Right)
        ));

        var deviceText = deviceInfo != null && (!string.IsNullOrWhiteSpace(deviceInfo.DeviceName) || !string.IsNullOrWhiteSpace(deviceInfo.SoftwareRev))
            ? $"Прибор: {deviceInfo.DeviceName}, ПО: {deviceInfo.SoftwareRev}"
            : string.Empty;

        table.Append(new TableRow(
            CreateInfoCell($"Дата/время исследования: {patient.TestDateTime}", JustificationValues.Left),
            CreateInfoCell(deviceText, JustificationValues.Right)
        ));

        table.Append(new TableRow(
            CreateInfoCell($"Количество тестов: {patient.Tests.Count} (в блоке указано: {patient.TotalNumTests})", JustificationValues.Left, gridSpan: 2)
        ));

        return table;
    }

    private static Drawing CreateImageDrawing(MainDocumentPart mainPart, GraphImage image, string name, ref uint imageId, double maxWidthInches, double maxHeightInches)
    {
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var stream = new MemoryStream(image.Data))
        {
            imagePart.FeedData(stream);
        }

        var relationshipId = mainPart.GetIdOfPart(imagePart);
        long cx = PixelsToEmus(image.Width, GraphRenderDpi);
        long cy = PixelsToEmus(image.Height, GraphRenderDpi);
        long maxCx = InchesToEmus(maxWidthInches);
        long maxCy = InchesToEmus(maxHeightInches);

        double scale = 1.0;
        if (cx > maxCx)
            scale = Math.Min(scale, (double)maxCx / cx);
        if (cy > maxCy)
            scale = Math.Min(scale, (double)maxCy / cy);

        if (scale < 1.0)
        {
            cx = (long)(cx * scale);
            cy = (long)(cy * scale);
        }

        var docId = imageId++;
        var picId = imageId++;

        return new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = cx, Cy = cy },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = docId, Name = $"{name}-{docId}" },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = picId, Name = $"{name}-{picId}" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(new A.Offset { X = 0L, Y = 0L }, new A.Extents { Cx = cx, Cy = cy }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })
                        )
                    )
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            )
            { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U }
        );
    }

    private static TableCell CreateInfoCell(string text, JustificationValues justification, int gridSpan = 1)
    {
        var props = new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
        var margin = new TableCellMargin
        {
            TopMargin = new TopMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
            LeftMargin = new LeftMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "60", Type = TableWidthUnitValues.Dxa }
        };
        props.Append(margin);
        if (gridSpan > 1)
            props.Append(new GridSpan { Val = gridSpan });

        var cell = new TableCell(props);
        cell.Append(CreateParagraph(text ?? string.Empty, fontSizePt: 11, justification: justification, tightenLineSpacing: true));
        return cell;
    }

    private static TableCell CreateMeasurementBodyCell(string text)
    {
        var props = new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
        var margin = new TableCellMargin
        {
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            TopMargin = new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa }
        };
        props.Append(margin);

        var cell = new TableCell(props);
        cell.Append(CreateParagraph(text, fontSizePt: 11));
        return cell;
    }

    private static TableCell CreateMeasurementHeaderCell(string text)
    {
        var props = new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
            new Shading { Val = ShadingPatternValues.Clear, Fill = "DDDDDD", Color = "000000" }
        );
        var margin = new TableCellMargin
        {
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            TopMargin = new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa }
        };
        props.Append(margin);

        var cell = new TableCell(props);
        cell.Append(CreateParagraph(text, fontSizePt: 11, bold: true, justification: JustificationValues.Center));
        return cell;
    }

    private static Paragraph CreateMeasurementParagraph(string value)
    {
        var paragraph = new Paragraph();
        paragraph.Append(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));

        if (IsMissingMeasurementString(value))
        {
            paragraph.Append(CreateMeasurementRun(MissingMeasurementText, 26, bold: true));
            return paragraph;
        }

        var (number, unit) = SplitValueAndUnit(value);

        paragraph.Append(CreateMeasurementRun(number, 26, bold: true));

        if (!string.IsNullOrEmpty(unit))
        {
            const int unitOffset = 8;
            paragraph.Append(CreateMeasurementRun(" ", 12, preserveSpace: true, position: unitOffset));
            paragraph.Append(CreateMeasurementRun(unit, 12, position: unitOffset));
        }

        return paragraph;
    }

    private static Run CreateMeasurementRun(string text, double fontSize, bool bold = false, bool preserveSpace = false, int? position = null)
    {
        var runProperties = new RunProperties(new FontSize { Val = PointsToHalfPointString(fontSize) });
        if (bold)
        {
            runProperties.Append(new Bold());
        }

        if (position.HasValue)
        {
            runProperties.Append(new Position { Val = position.Value.ToString() });
        }

        var run = new Run(runProperties);
        var textElement = new Text(text ?? string.Empty);
        if (preserveSpace)
        {
            textElement.Space = SpaceProcessingModeValues.Preserve;
        }

        run.Append(textElement);
        return run;
    }

    private static Table CreateMeasurementTable(ErgTest test)
    {
        var table = new Table(
            new TableProperties(
                new TableStyle { Val = "TableGrid" },
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableLook { Val = "04A0", FirstRow = true, LastRow = false, NoHorizontalBand = false, NoVerticalBand = false }
            ),
            new TableGrid(new GridColumn { Width = "2200" }, new GridColumn { Width = "1600" }, new GridColumn { Width = "1600" })
        );

        var headerRow = new TableRow();
        headerRow.Append(CreateMeasurementHeaderCell("Параметр"));
        headerRow.Append(CreateMeasurementHeaderCell("Правый глаз"));
        headerRow.Append(CreateMeasurementHeaderCell("Левый глаз"));
        table.Append(headerRow);

        foreach (var row in GetEyeTableRows(test))
        {
            var bodyRow = new TableRow();
            bodyRow.Append(CreateMeasurementBodyCell(row.Caption));
            bodyRow.Append(CreateMeasurementBodyCell(row.Right));
            bodyRow.Append(CreateMeasurementBodyCell(row.Left));
            table.Append(bodyRow);
        }

        return table;
    }

    private static Paragraph CreateParagraph(string text, double fontSizePt = 11, bool bold = false, JustificationValues? justification = null, int spacingBefore = 0, int spacingAfter = 0, bool italic = false, string? colorHex = null, bool tightenLineSpacing = false)
    {
        var paragraph = new Paragraph();
        var paragraphProps = new ParagraphProperties();
        if (justification.HasValue)
            paragraphProps.Append(new Justification { Val = justification.Value });
        var spacing = new SpacingBetweenLines();
        var hasSpacing = false;

        if (spacingBefore > 0)
        {
            spacing.Before = spacingBefore.ToString(CultureInfo.InvariantCulture);
            hasSpacing = true;
        }
        else if (tightenLineSpacing)
        {
            spacing.Before = "0";
            hasSpacing = true;
        }

        if (spacingAfter > 0)
        {
            spacing.After = spacingAfter.ToString(CultureInfo.InvariantCulture);
            hasSpacing = true;
        }
        else if (tightenLineSpacing)
        {
            spacing.After = "0";
            hasSpacing = true;
        }

        if (tightenLineSpacing)
        {
            spacing.LineRule = LineSpacingRuleValues.Auto;
            spacing.Line = "240";
            hasSpacing = true;
        }

        if (hasSpacing)
            paragraphProps.Append(spacing);
        paragraph.ParagraphProperties = paragraphProps;

        var run = new Run();
        var runProps = new RunProperties();
        if (bold)
            runProps.Append(new Bold());
        if (italic)
            runProps.Append(new Italic());
        if (!string.IsNullOrEmpty(colorHex))
            runProps.Append(new WordColor { Val = colorHex });
        runProps.Append(new FontSize { Val = ((int)Math.Round(fontSizePt * 2)).ToString(CultureInfo.InvariantCulture) });
        runProps.Append(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", EastAsia = "Calibri", ComplexScript = "Calibri" });
        run.RunProperties = runProps;

        AppendTextWithBreaks(run, text ?? string.Empty);
        paragraph.Append(run);
        return paragraph;
    }

    private static string[] DescribeGraphStyles(ErgTest test)
        => test.GraphStyles?
            .Where(s => s.Index < 6)
            .Select(s => $"{s.Index + 1}: RGB({s.Red},{s.Green},{s.Blue}){(s.Dotted ? ", пунктир" : string.Empty)}")
            .ToArray() ?? Array.Empty<string>();

    private static double DetermineAxisStep(double min, double max, int valueStep, int lineStep)
    {
        var range = max - min;
        if (range <= 0)
            return 0;

        double step = 0;

        if (valueStep > 0)
        {
            step = valueStep;
            if (lineStep > 0)
                step *= lineStep;
        }

        if (step <= 0)
        {
            step = CalculateNiceStep(range / 12.0);
        }
        else
        {
            step = CalculateNiceStep(step);
        }

        if (step <= 0)
            return 0;

        const double minTickCount = 6.0;
        const double maxTickCount = 16.0;

        while (range / step < minTickCount)
        {
            step /= 2.0;
            if (step <= 0)
                break;
        }

        while (range / step > maxTickCount)
        {
            step *= 2.0;
        }

        return step;
    }

    private static int DetermineValueCount(EyeData eye)
    {
        int count = eye.ValueCount ?? 0;
        count = Math.Max(count, eye.AWaveMs?.Length ?? 0);
        count = Math.Max(count, eye.AWaveMkV?.Length ?? 0);
        count = Math.Max(count, eye.BWaveMs?.Length ?? 0);
        count = Math.Max(count, eye.BWaveMkV?.Length ?? 0);
        return count;
    }

    private static void EnsureDefaultStyles(MainDocumentPart mainPart)
    {
        if (mainPart.StyleDefinitionsPart == null)
        {
            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            var styles = new Styles(
                new DocDefaults(
                    new RunPropertiesDefault(
                        new RunPropertiesBaseStyle(
                            new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", EastAsia = "Calibri", ComplexScript = "Calibri" },
                            new FontSize { Val = "22" })),
                    new ParagraphPropertiesDefault(
                        new ParagraphPropertiesBaseStyle(
                            new SpacingBetweenLines
                            {
                                Before = "0",
                                After = "0",
                                LineRule = LineSpacingRuleValues.Auto,
                                Line = "240"
                            })))
            );
            stylesPart.Styles = styles;
        }

        if (mainPart.NumberingDefinitionsPart == null)
        {
            var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
            numberingPart.Numbering = new Numbering();
        }
    }

    private static void EnsureDocumentPropertiesParts(WordprocessingDocument document, string? title)
    {
        if (document == null)
            return;

        var corePart = document.CoreFilePropertiesPart ?? document.AddCoreFilePropertiesPart();
        WriteCoreProperties(corePart, title);

        var appPart = document.ExtendedFilePropertiesPart ?? document.AddExtendedFilePropertiesPart();
        WriteExtendedProperties(appPart);
    }

    private static void EnsureDocumentRelationships(ZipArchive archive)
    {
        var relsEntry = archive.GetEntry("word/_rels/document.xml.rels");
        if (relsEntry == null)
            return;

        var ns = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
        XDocument document;
        using (var stream = relsEntry.Open())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: false))
        {
            document = XDocument.Load(reader);
        }

        var root = document.Root ?? new XElement(ns + "Relationships");
        if (document.Root == null)
            document.Add(root);

        var entries = new HashSet<string>(archive.Entries.Select(e => e.FullName), StringComparer.OrdinalIgnoreCase);
        var relationships = root.Elements(ns + "Relationship").ToList();
        bool changed = false;

        foreach (var relationship in relationships)
        {
            var target = (string?)relationship.Attribute("Target");
            if (string.IsNullOrWhiteSpace(target))
                continue;

            var resolved = ResolveDocumentRelationshipTarget(target);
            if (string.IsNullOrEmpty(resolved))
                continue;

            if (!entries.Contains(resolved))
            {
                relationship.Remove();
                changed = true;
            }
        }

        if (!changed)
            return;

        relsEntry.Delete();
        var newEntry = archive.CreateEntry("word/_rels/document.xml.rels", CompressionLevel.Optimal);
        using var newStream = newEntry.Open();
        using var writer = new StreamWriter(newStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        document.Save(writer);
    }

    private static void EnsurePackageRelationships(ZipArchive archive)
    {
        var relsEntry = archive.GetEntry("_rels/.rels");
        if (relsEntry == null)
            return;

        var ns = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
        XDocument document;
        using (var stream = relsEntry.Open())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: false))
        {
            document = XDocument.Load(reader);
        }

        var root = document.Root ?? new XElement(ns + "Relationships");
        if (document.Root == null)
            document.Add(root);

        var entries = new HashSet<string>(archive.Entries.Select(e => e.FullName), StringComparer.OrdinalIgnoreCase);
        var relationships = root.Elements(ns + "Relationship").ToList();
        bool changed = false;

        foreach (var relationship in relationships.ToList())
        {
            var target = (string?)relationship.Attribute("Target");
            if (string.IsNullOrWhiteSpace(target))
                continue;

            var resolved = NormalizeRelationshipTarget(target);
            if (string.IsNullOrEmpty(resolved))
                continue;

            if (!entries.Contains(resolved))
            {
                relationship.Remove();
                changed = true;
            }
        }

        relationships = root.Elements(ns + "Relationship").ToList();
        var existingIds = new HashSet<string>(relationships
            .Select(r => (string?)r.Attribute("Id") ?? string.Empty), StringComparer.OrdinalIgnoreCase);

        int idCounter = Math.Max(1, relationships.Count + 1);
        string NextId()
        {
            string candidate;
            do
            {
                candidate = $"rId{idCounter++}";
            } while (!existingIds.Add(candidate));

            return candidate;
        }

        bool EnsureRelation(string partName, string relationshipType)
        {
            var normalizedTarget = NormalizePartName(partName).TrimStart('/');
            if (!entries.Contains(normalizedTarget))
                return false;

            if (relationships.Any(r =>
                string.Equals((string?)r.Attribute("Type"), relationshipType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(NormalizeRelationshipTarget((string?)r.Attribute("Target")), normalizedTarget, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var id = NextId();
            root.Add(new XElement(ns + "Relationship",
                new XAttribute("Id", id),
                new XAttribute("Type", relationshipType),
                new XAttribute("Target", normalizedTarget)));
            relationships = root.Elements(ns + "Relationship").ToList();
            changed = true;
            return true;
        }

        EnsureRelation("/word/document.xml", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument");
        EnsureRelation("/docProps/core.xml", "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties");
        EnsureRelation("/docProps/app.xml", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties");

        if (!changed)
            return;

        relsEntry.Delete();
        var newEntry = archive.CreateEntry("_rels/.rels", CompressionLevel.Optimal);
        using var newStream = newEntry.Open();
        using var writer = new StreamWriter(newStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        document.Save(writer);
    }

    private static bool EyeHasUsableMeasurements(EyeData eye)
    {
        if (eye.IsFlat)
            return true;

        if (eye.QualityIndex.HasValue && eye.QualityIndex.Value == 0)
            return false;

        if (eye.ValueCount.HasValue && eye.ValueCount.Value == 0)
            return false;

        if (eye.GraphCount == 0)
            return false;

        return HasEyeMeasurementValues(eye);
    }

    private static string FormatAnimal(AnimalKind animal)
        => animal switch
        {
            AnimalKind.Cat => "Кошка",
            AnimalKind.Dog => "Собака",
            AnimalKind.Rabbit => "Кролик",
            AnimalKind.Horse => "Лошадь",
            AnimalKind.Other => "Прочие",
            _ => animal.ToString()
        };

    private static string FormatAxisValue(double value)
    {
        if (Math.Abs(value) < 1e-9)
            value = 0;

        if (Math.Abs(value) >= 1000)
            return value.ToString("0", CultureInfo.InvariantCulture);

        if (Math.Abs(value) >= 100)
            return value.ToString("0.#", CultureInfo.InvariantCulture);

        if (Math.Abs(value) >= 1)
            return value.ToString("0.##", CultureInfo.InvariantCulture);

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatClientDateTime(string value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return "—";

        var ruCulture = CultureInfo.GetCultureInfo("ru-RU");
        var cultures = new[] { ruCulture, CultureInfo.InvariantCulture };
        var formats = new[]
        {
            "dd.MM.yyyy HH:mm",
            "dd.MM.yyyy H:mm",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy H:mm",
            "dd-MM-yyyy HH:mm",
            "dd-MM-yyyy H:mm",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd H:mm",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.FFF",
            "yyyy.MM.dd HH:mm"
        };

        foreach (var culture in cultures)
        {
            if (DateTime.TryParse(trimmed, culture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var parsed))
                return parsed.ToString("dd/MM/yyyy HH:mm", ruCulture);
        }

        foreach (var culture in cultures)
        {
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(trimmed, format, culture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var parsedExact))
                    return parsedExact.ToString("dd/MM/yyyy HH:mm", ruCulture);
            }
        }

        return trimmed;
    }

    private static string FormatClientTestName(string testName)
    {
        if (string.IsNullOrWhiteSpace(testName))
            return "—";

        var trimmedEnd = testName.TrimEnd();
        return string.IsNullOrEmpty(trimmedEnd) ? "—" : trimmedEnd;
    }

    private static string FormatClientTestTitle(int index, ErgTest test)
    {
        var name = FormatClientTestName(test.TestName);
        return $"Тест № {index}:  {name}";
    }

    private static string FormatDeviceInfo(CommonInfo? deviceInfo)
    {
        if (deviceInfo == null)
            return "—";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(deviceInfo.DeviceName))
            parts.Add(deviceInfo.DeviceName);
        if (!string.IsNullOrWhiteSpace(deviceInfo.SoftwareRev))
            parts.Add($"ПО: {deviceInfo.SoftwareRev}");

        return parts.Count > 0 ? string.Join(", ", parts) : "—";
    }

    private static string FormatMarker(EyeData eye, byte? marker)
    {
        int valueCount = DetermineValueCount(eye);
        if (valueCount <= 0)
            return MissingMeasurementText;
        if (!marker.HasValue || marker.Value == byte.MaxValue)
            return MissingMeasurementText;
        return $"{marker} мс";
    }

    private static string FormatWaveMs(ushort? value)
    {
        if (!value.HasValue)
            return MissingMeasurementText;

        if (value.Value == byte.MaxValue || value.Value == ushort.MaxValue)
            return MissingMeasurementText;

        return $"{value.Value} мс";
    }

    private static string FormatWaveMkV(uint? value)
    {
        if (!value.HasValue)
            return MissingMeasurementText;

        if (value.Value == ushort.MaxValue)
            return MissingMeasurementText;

        return $"{value.Value} мкВ";
    }

    private static string FormatPrimaryMeasurement(double? value, string unit)
    {
        if (!value.HasValue)
            return MissingMeasurementText;

        double rounded = Math.Round(value.Value);
        if (Math.Abs(rounded - byte.MaxValue) < 0.5 || Math.Abs(rounded - ushort.MaxValue) < 0.5)
            return MissingMeasurementText;

        return $"{rounded:0} {unit}";
    }

    private static string? FormatMeasurement(EyeData eye, int index)
    {
        int valueCount = DetermineValueCount(eye);
        if (index >= valueCount)
            return null;

        var aMsArray = eye.AWaveMs ?? Array.Empty<ushort?>();
        var aMkVArray = eye.AWaveMkV ?? Array.Empty<uint?>();
        var bMsArray = eye.BWaveMs ?? Array.Empty<ushort?>();
        var bMkVArray = eye.BWaveMkV ?? Array.Empty<uint?>();

        var aMs = index < aMsArray.Length ? aMsArray[index] : null;
        var aMkV = index < aMkVArray.Length ? aMkVArray[index] : null;
        var bMs = index < bMsArray.Length ? bMsArray[index] : null;
        var bMkV = index < bMkVArray.Length ? bMkVArray[index] : null;

        bool hasA = aMs.HasValue || aMkV.HasValue;
        bool hasB = bMs.HasValue || bMkV.HasValue;

        if (!hasA && !hasB)
            return null;

        return $"a: {FormatWaveMs(aMs)}, {FormatWaveMkV(aMkV)}\n" +
               $"b: {FormatWaveMs(bMs)}, {FormatWaveMkV(bMkV)}";
    }

    private static string? FormatNormForClient(string value)
    {
        if (IsMissingMeasurementString(value))
            return null;

        return $"[{value}]";
    }

    private static string? FormatQualityCompact(byte? quality)
    {
        if (!quality.HasValue)
            return null;

        int value = Math.Clamp((int)quality.Value, 0, 3);
        return new string('★', value) + new string('☆', 3 - value);
    }

    private static string FormatRange(byte? min, byte? max)
    {
        if (!min.HasValue && !max.HasValue) return "—";
        if (!min.HasValue) return $"≤ {max}";
        if (!max.HasValue) return $"≥ {min}";
        return $"{min}..{max}";
    }

    private static string FormatRange(uint? min, uint? max)
    {
        if (!min.HasValue && !max.HasValue) return "—";
        if (!min.HasValue) return $"≤ {max}";
        if (!max.HasValue) return $"≥ {min}";
        return $"{min}..{max}";
    }

    private static string GetApplicationVersion()
    {
        var version = typeof(ErgReportBuilder).Assembly.GetName().Version;
        return version?.ToString() ?? "—";
    }

    private static string GetClientDeviceName(CommonInfo? info)
    {
        if (info == null)
            return DefaultClientDeviceName;

        if (!string.IsNullOrWhiteSpace(info.DeviceName))
            return info.DeviceName;

        if (!string.IsNullOrWhiteSpace(info.ReportName))
            return info.ReportName;

        return DefaultClientDeviceName;
    }

    private static string? GetClientSoftwareVersion(CommonInfo? info)
        => string.IsNullOrWhiteSpace(info?.SoftwareRev) ? null : info!.SoftwareRev;

    private static IEnumerable<(string Label, WaveKind Kind)> GetClientWaveOrder(ErgTest test)
    {
        if (test.AWaveExists)
        {
            yield return ("a-волна", WaveKind.A);
            yield return ("b-волна", WaveKind.B);
        }
        else
        {
            yield return (string.Empty, WaveKind.B);
        }
    }

    private static IEnumerable<TableRowData> GetEyeTableRows(ErgTest test)
    {
        yield return new TableRowData("FLAT", BoolText(test.RightEye.IsFlat), BoolText(test.LeftEye.IsFlat));
        yield return new TableRowData("QI", Quality(test.RightEye.QualityIndex), Quality(test.LeftEye.QualityIndex));
        yield return new TableRowData("Маркер a", FormatMarker(test.RightEye, test.RightEye.AWaveMarker), FormatMarker(test.LeftEye, test.LeftEye.AWaveMarker));
        yield return new TableRowData("Маркер b", FormatMarker(test.RightEye, test.RightEye.BWaveMarker), FormatMarker(test.LeftEye, test.LeftEye.BWaveMarker));

        var maxValues = Math.Max(DetermineValueCount(test.RightEye), DetermineValueCount(test.LeftEye));
        for (int i = 0; i < maxValues; i++)
        {
            var right = FormatMeasurement(test.RightEye, i);
            var left = FormatMeasurement(test.LeftEye, i);
            if (string.IsNullOrEmpty(right) && string.IsNullOrEmpty(left))
                continue;

            yield return new TableRowData($"Замер #{i + 1}", right ?? "—", left ?? "—");
        }
    }

    private static double? GetFirstValue(ushort?[]? values)
    {
        if (values == null)
            return null;

        foreach (var value in values)
        {
            if (!value.HasValue)
                continue;

            if (value.Value == byte.MaxValue || value.Value == ushort.MaxValue)
                continue;

            return value.Value;
        }

        return null;
    }

    private static double? GetFirstValue(uint?[]? values)
    {
        if (values == null)
            return null;

        foreach (var value in values)
        {
            if (!value.HasValue)
                continue;

            if (value.Value == ushort.MaxValue)
                continue;

            return value.Value;
        }

        return null;
    }

    private static IEnumerable<string> GetImageExtensions(IReadOnlyCollection<ZipArchiveEntry> entries)
    {
        return entries
            .Where(entry => entry.FullName.StartsWith("word/media/", StringComparison.OrdinalIgnoreCase))
            .Select(entry => Path.GetExtension(entry.FullName))
            .Where(ext => !string.IsNullOrWhiteSpace(ext))
            .Select(ext => ext!.TrimStart('.').ToLowerInvariant())
            .Distinct();
    }

    private static SKTypeface GetTypeface()
    {
        _skTypeface ??= SKTypeface.FromFamilyName("Arial")
                       ?? SKTypeface.FromFamilyName("DejaVu Sans")
                       ?? SKTypeface.Default;
        return _skTypeface;
    }

    private static IEnumerable<(string PartName, string ContentType)> GetWellKnownOverrides()
    {
        yield return ("/docProps/core.xml", "application/vnd.openxmlformats-package.core-properties+xml");
        yield return ("/docProps/app.xml", "application/vnd.openxmlformats-officedocument.extended-properties+xml");
        yield return ("/word/styles.xml", "application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml");
        yield return ("/word/settings.xml", "application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml");
        yield return ("/word/numbering.xml", "application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml");
        yield return ("/word/fontTable.xml", "application/vnd.openxmlformats-officedocument.wordprocessingml.fontTable+xml");
        yield return ("/word/webSettings.xml", "application/vnd.openxmlformats-officedocument.wordprocessingml.webSettings+xml");
        yield return ("/word/theme/theme1.xml", "application/vnd.openxmlformats-officedocument.theme+xml");
    }

    private static bool HasEyeMeasurementValues(EyeData eye)
    {
        return DetermineValueCount(eye) > 0;
    }

    private static long InchesToEmus(double inches) => (long)(inches * 914400);

    private static bool IsWaveDisplayEmpty(WaveDisplay display)
    {
        if (display.IsFlat)
            return false;

        return display.MsValue == "—"
            && display.MkVValue == "—"
            && FormatNormForClient(display.MsNorm) == null
            && FormatNormForClient(display.MkVNorm) == null;
    }

    private static bool IsWithinAxis(double value, double min, double max)
        => !double.IsNaN(value) && !double.IsInfinity(value) && value >= min - 1e-6 && value <= max + 1e-6;

    private static float MillimetersToPixels(float millimeters)
        => millimeters / 25.4f * GraphRenderDpi;

    private static string NormalizePartName(string? partName)
    {
        if (string.IsNullOrWhiteSpace(partName))
            return string.Empty;

        var trimmed = partName.Trim().Replace('\\', '/');
        return trimmed.StartsWith("/") ? trimmed : "/" + trimmed.TrimStart('/');
    }

    private static string NormalizeRelationshipTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return string.Empty;

        var trimmed = target.Replace('\\', '/').Trim();
        if (trimmed.StartsWith("../"))
            trimmed = trimmed.Substring(3);
        return trimmed.TrimStart('/');
    }

    private static void NormalizeWordContentTypes(string docxPath)
    {
        if (string.IsNullOrWhiteSpace(docxPath) || !File.Exists(docxPath))
            return;

        try
        {
            using var archive = ZipFile.Open(docxPath, ZipArchiveMode.Update);
            var manifestEntry = archive.GetEntry("[Content_Types].xml");
            if (manifestEntry == null)
                return;

            var entries = archive.Entries.ToList();
            var newManifest = BuildContentTypesManifest(entries);

            manifestEntry.Delete();
            var newEntry = archive.CreateEntry("[Content_Types].xml", CompressionLevel.Optimal);
            using (var newStream = newEntry.Open())
            using (var writer = new StreamWriter(newStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                newManifest.Save(writer);
            }

            EnsurePackageRelationships(archive);
            EnsureDocumentRelationships(archive);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Не удалось обновить типы содержимого DOCX-файла '{docxPath}'.", ex);
        }
    }

    private static bool PackagePartExists(IReadOnlyCollection<ZipArchiveEntry> entries, string partName)
    {
        var normalized = NormalizePartName(partName).TrimStart('/');
        return entries.Any(entry => string.Equals(entry.FullName, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static long PixelsToEmus(int pixels) => PixelsToEmus(pixels, 96f);

    private static long PixelsToEmus(int pixels, float dpi)
        => (long)Math.Round(pixels / dpi * 914400);

    private static string PointsToHalfPointString(double points)
        => Math.Round(points * 2).ToString(CultureInfo.InvariantCulture);

    private static float PointsToPixels(float points)
        => points / 72f * GraphRenderDpi;

    private static string[] PrepareClinicHeaderLines(string[] lines)
    {
        const int headerLineCount = 4;
        var result = new string[headerLineCount];
        Array.Fill(result, "\u00A0");

        if (lines == null || lines.Length == 0)
            return result;

        for (int i = 0; i < headerLineCount && i < lines.Length; i++)
        {
            var line = lines[i];
            if (!string.IsNullOrWhiteSpace(line))
                result[i] = line!;
        }

        return result;
    }

    private static string Quality(byte? quality)
    {
        if (!quality.HasValue)
            return "—";

        int value = Math.Clamp((int)quality.Value, 0, 3);
        return new string('★', value) + new string('☆', 3 - value);
    }

    private static string ResolveDocumentRelationshipTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return string.Empty;

        var normalized = target.Replace('\\', '/').Trim();
        if (normalized.StartsWith("../", StringComparison.Ordinal))
        {
            return "word/" + normalized.Substring(3);
        }

        if (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            return normalized.TrimStart('/');
        }

        return "word/" + normalized.TrimStart('/');
    }

    private static SKPaint SkTextPaint(float points, SKColor color, bool bold = false)
        => new SKPaint
        {
            Color = color,
            TextSize = PointsToPixels(points),
            IsAntialias = true,
            Typeface = GetTypeface(),
            FakeBoldText = bold
        };

    private static bool IsMissingMeasurementString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();
        return string.Equals(trimmed, "—", StringComparison.Ordinal)
            || string.Equals(trimmed, MissingMeasurementText, StringComparison.Ordinal);
    }

    private static (string Value, string Unit) SplitValueAndUnit(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (MissingMeasurementText, string.Empty);

        var trimmed = text.Trim();
        if (string.Equals(trimmed, MissingMeasurementText, StringComparison.Ordinal))
            return (MissingMeasurementText, string.Empty);

        var lastSpace = trimmed.LastIndexOf(' ');
        if (lastSpace <= 0 || lastSpace >= trimmed.Length - 1)
            return (trimmed, string.Empty);

        var value = trimmed[..lastSpace];
        var unit = trimmed[(lastSpace + 1)..];
        return (value, unit);
    }

    private static float GetBaselineOffset(DrawingFont font, Graphics graphics)
    {
        var family = font.FontFamily;
        var style = font.Style;
        var lineSpacing = family.GetLineSpacing(style);
        if (lineSpacing == 0)
            return font.GetHeight(graphics);

        var ascent = family.GetCellAscent(style);
        return font.GetHeight(graphics) * ascent / lineSpacing;
    }

    private static bool TryGetImageContentType(string extension, out string contentType)
    {
        contentType = extension.ToLowerInvariant() switch
        {
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "bmp" => "image/bmp",
            "gif" => "image/gif",
            "tif" or "tiff" => "image/tiff",
            "wmf" => "image/x-wmf",
            "emf" => "image/x-emf",
            _ => string.Empty
        };

        return !string.IsNullOrEmpty(contentType);
    }

    private static bool TryPrepareGraphData(ErgTest test, EyeData eye, out GraphRenderContext context)
    {
        context = default!;

        var graphs = eye.GraphsNormalized;
        if (graphs == null || graphs.Length == 0)
            return false;

        int curves = Math.Clamp(eye.GraphCount, 0, graphs.Length);
        if (curves <= 0)
            return false;

        bool hasSamples = false;
        bool hasValues = false;
        int maxSampleCount = 0;
        double sampleMin = double.PositiveInfinity;
        double sampleMax = double.NegativeInfinity;
        for (int i = 0; i < curves; i++)
        {
            var samples = graphs[i];
            if (samples is { Length: > 1 })
            {
                hasSamples = true;
                if (samples.Length > maxSampleCount)
                    maxSampleCount = samples.Length;
            }

            if (samples == null || samples.Length == 0)
                continue;

            for (int j = 0; j < samples.Length; j++)
            {
                var value = samples[j];
                if (double.IsNaN(value) || double.IsInfinity(value))
                    continue;

                hasValues = true;
                if (value < sampleMin)
                    sampleMin = value;
                if (value > sampleMax)
                    sampleMax = value;
            }
        }

        if (!hasSamples || !hasValues)
            return false;

        double xMin = test.GraphXScaleMin;
        double xMax = test.GraphXScaleMax;
        double scaleXMin = xMin;
        double scaleXMax = xMax;
        if (xMax <= xMin)
            xMax = xMin + 1;

        double yMin = test.GraphYScaleMin;
        double yMax = test.GraphYScaleMax;
        if (yMax <= yMin)
            yMax = yMin + 1;

        double flashOffset = test.GraphFlashPosition;

        double graphDt = test.GraphDt;
        int pointCount = Math.Max(test.GraphNumPoints, maxSampleCount);
        if (graphDt > 0 && pointCount > 1)
        {
            double totalDuration = (pointCount - 1) * graphDt;
            double dataMin = -flashOffset;
            double dataMax = totalDuration - flashOffset;
            if (dataMin < xMin)
                xMin = dataMin;
            if (dataMax > xMax)
                xMax = dataMax;
        }

        if (xMax <= xMin)
            xMax = xMin + 1;

        var markers = BuildMarkers(eye, xMin, xMax);
        var waveLevels = BuildWaveLevels(eye, graphs, curves, graphDt, flashOffset);

        context = new GraphRenderContext(
            graphs,
            curves,
            test.GraphNumPoints,
            xMin,
            xMax,
            yMin,
            yMax,
            sampleMin,
            sampleMax,
            flashOffset,
            markers,
            waveLevels,
            scaleXMin,
            scaleXMax);
        return true;
    }

    private static GraphImage? TryRenderGraphImage(ErgTest test, EyeData eye)
    {
        if (!RenderingSupport.GraphRenderingSupported)
            return null;

        if (!TryPrepareGraphData(test, eye, out var context))
            return null;

        return RenderingSupport.UseLegacyGraphRendering
            ? TryRenderGraphImageWithGdi(test, context)
            : TryRenderGraphImageWithSkia(test, context);
    }

    private static GraphImage? TryRenderGraphImageWithGdi(ErgTest test, GraphRenderContext context)
    {
        const int width = GraphImagePixelWidth;
        const int height = GraphImagePixelHeight;

        try
        {
            var opt = GraphOptions;

            using var bmp = new Bitmap(width, height);
            bmp.SetResolution(GraphRenderDpi, GraphRenderDpi);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            var sf = StringFormat.GenericTypographic;
            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

            g.Clear(System.Drawing.Color.White);

            // поля и зазоры
            float marginLeft = opt.MarginLeft;
            float marginRight = opt.MarginRight;
            float marginTop = opt.MarginTop;
            float marginBottom = opt.MarginBottom;
            float axisGapHorizontal = opt.AxisGapHorizontal; // слева под цифры Y + µV
            float axisGapVertical = opt.AxisGapVertical;     // снизу под цифры X + ms

            // длины рисок (мм → px)
            float majorTickLen = MillimetersToPixels(opt.MajorTickLenMm);
            float minorTickLen = MillimetersToPixels(opt.MinorTickLenMm);

            // резерв сверху под маркеры
            float markerTopReserve = 0f;

            var chartRect = new RectangleF(
                marginLeft,
                marginTop + markerTopReserve,
                width - marginLeft - marginRight,
                height - (marginTop + markerTopReserve) - marginBottom);

            double xMin = context.XMin, xMax = context.XMax;
            double yMin = context.YMin, yMax = context.YMax;
            const double eps = 1e-6;

            float X(double v) => (float)(chartRect.Left + (v - xMin) / (xMax - xMin) * chartRect.Width);
            float Y(double v) => (float)(chartRect.Bottom - (v - yMin) / (yMax - yMin) * chartRect.Height);

            var xTicks = BuildAxisTickSet(xMin, xMax, 0, test.GraphXValueStep, test.GraphXLineStep);
            var yTicks = BuildAxisTickSet(yMin, yMax, 0, test.GraphYValueStep, test.GraphYLineStep);

            float xAxisY = chartRect.Bottom + axisGapVertical; // ниже области графика
            float yAxisX = chartRect.Left - axisGapHorizontal; // левее области графика

            // сетка
            if (opt.GridThicknessPx > 0f)
            {
                using var grid = new Pen(System.Drawing.Color.FromArgb(230, 230, 230), opt.GridThicknessPx);
                foreach (var v in xTicks.GridLines)
                {
                    var px = X(v);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1) continue;
                    g.DrawLine(grid, px, chartRect.Top, px, chartRect.Bottom);
                }
                foreach (var v in yTicks.GridLines)
                {
                    if (Math.Abs(v) < eps) continue;
                    var py = Y(v);
                    if (py <= chartRect.Top + 1 || py >= chartRect.Bottom - 1) continue;
                    g.DrawLine(grid, chartRect.Left, py, chartRect.Right, py);
                }
            }

            // оси
            using (var axis = new Pen(System.Drawing.Color.Black, opt.AxisThicknessPx))
            {
                g.DrawLine(axis, chartRect.Left, xAxisY, chartRect.Right, xAxisY);
                g.DrawLine(axis, yAxisX, chartRect.Top, yAxisX, chartRect.Bottom);
            }

            // риски
            using (var tick = new Pen(System.Drawing.Color.Black, opt.TickThicknessPx))
            {
                foreach (var t in xTicks.Ticks)
                {
                    var px = X(t.Position);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1) continue;
                    g.DrawLine(tick, px, xAxisY, px, xAxisY + (t.IsMajor ? majorTickLen : minorTickLen)); // вниз (наружу)
                }
                foreach (var t in yTicks.Ticks)
                {
                    var py = Y(t.Position);
                    if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1) continue;
                    g.DrawLine(tick, yAxisX, py, yAxisX - (t.IsMajor ? majorTickLen : minorTickLen), py); // влево (наружу)
                }
            }

            var dottedPattern = BuildDottedPattern(opt);

            // вертикальные маркеры (a/b)
            if (context.Markers.Length > 0 && opt.ExtremumThicknessPx > 0f)
            {
                using var markerPen = new Pen(System.Drawing.Color.Black, Math.Max(1f, opt.ExtremumThicknessPx))
                {
                    DashStyle = DashStyle.Custom,
                    DashCap = DashCap.Round,
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                markerPen.DashPattern = (float[])dottedPattern.Clone();

                foreach (var m in context.Markers)
                {
                    var px = X(m.PositionMs);
                    if (float.IsNaN(px) || float.IsInfinity(px)) continue;
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1) continue;

                    g.DrawLine(markerPen, px, chartRect.Top, px, chartRect.Bottom);
                }
            }

            // горизонтальные маркеры (уровни a/b)
            if (context.WaveLevels.Length > 0 && opt.HorizontalMarkerThicknessPx > 0f)
            {
                using var levelPen = new Pen(System.Drawing.Color.Black, Math.Max(1f, opt.HorizontalMarkerThicknessPx))
                {
                    DashStyle = DashStyle.Custom,
                    DashCap = DashCap.Round,
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                levelPen.DashPattern = (float[])dottedPattern.Clone();

                foreach (var level in context.WaveLevels)
                {
                    if (!IsWithinAxis(level.Value, yMin, yMax)) continue;
                    var py = Y(level.Value);
                    if (py <= chartRect.Top + 1 || py >= chartRect.Bottom - 1) continue;

                    g.DrawLine(levelPen, chartRect.Left, py, chartRect.Right, py);
                }
            }

            // кривые
            var saved = g.Save();
            g.SetClip(chartRect);

            double dt = test.GraphDt;
            bool hasDt = dt > 0;
            var styles = test.GraphStyles ?? Array.Empty<GraphStyle>();
            double flashOffset = context.FlashOffset;

            for (int gi = 0; gi < context.Curves; gi++)
            {
                var s = context.Graphs[gi];
                if (s == null || s.Length == 0) continue;

                int n = context.DeclaredPointCount > 1 ? Math.Min(context.DeclaredPointCount, s.Length) : s.Length;
                if (n < 2) continue;

                var pts = new List<PointF>(n);
                for (int i = 0; i < n; i++)
                {
                    double xv;
                    if (hasDt)
                    {
                        xv = i * dt - flashOffset;
                        if (xv < xMin) continue;
                        if (xv > xMax) break;
                    }
                    else
                    {
                        xv = (n == 1) ? xMin : xMin + (xMax - xMin) * i / (n - 1);
                    }

                    var px = X(xv);
                    var py = Y(s[i]);
                    if (float.IsNaN(px) || float.IsNaN(py) || float.IsInfinity(px) || float.IsInfinity(py)) continue;
                    pts.Add(new PointF(px, py));
                }

                if (pts.Count < 2) continue;

                var st = gi < styles.Length ? styles[gi] : null;
                var col = st != null ? System.Drawing.Color.FromArgb(st.Red, st.Green, st.Blue)
                                     : System.Drawing.Color.FromArgb(56, 109, 179);

                using var pen = new Pen(col, opt.CurveThicknessPx) { LineJoin = LineJoin.Round };
                if (st?.Dotted == true)
                {
                    pen.DashStyle = DashStyle.Custom;
                    pen.DashCap = DashCap.Round;
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.DashPattern = (float[])dottedPattern.Clone();
                }
                g.DrawLines(pen, pts.ToArray());
            }

            g.Restore(saved);

            // ===== подписи осей =====
            float labelPx = PointsToPixels(opt.LabelFontPt);
            float unitPx = PointsToPixels(opt.UnitsFontPt);
            using var tickFont = new System.Drawing.Font("Arial", labelPx, FontStyle.Regular, GraphicsUnit.Pixel);
            using var unitFont = new System.Drawing.Font("Arial", unitPx, FontStyle.Regular, GraphicsUnit.Pixel);

            const float minLabelGapY = 4f;
            float minLabelGapX = opt.MinLabelGapXPx;

            // ----- X: цифры -------------------------------------------------------
            float digitsH = tickFont.GetHeight(g);
            float unitsH = unitFont.GetHeight(g);

            float xDigitsTop = xAxisY + majorTickLen + opt.XDigitsOffsetPx;
            float xUnitsTop = xDigitsTop + digitsH + opt.XUnitsGapPx;

            // следим, чтобы две строки поместились
            float maxUnitsTop = height - 6f - unitsH;
            if (xUnitsTop > maxUnitsTop)
            {
                float shiftUp = xUnitsTop - maxUnitsTop;
                xDigitsTop -= shiftUp;
                xUnitsTop = maxUnitsTop;
            }

            float lastRight = float.NegativeInfinity;
            var drawnPx = new List<float>();

            bool TryDrawLabel(double coordinate, double displayValue, bool force = false)
            {
                if (double.IsNaN(coordinate) || double.IsNaN(displayValue))
                    return false;
                if (double.IsInfinity(coordinate) || double.IsInfinity(displayValue))
                    return false;

                if (displayValue < context.ScaleXMin - eps || displayValue > context.ScaleXMax + eps)
                    return false;
                if (coordinate < xMin - eps || coordinate > xMax + eps)
                    return false;

                var px = X(coordinate);
                if (float.IsNaN(px) || float.IsInfinity(px))
                    return false;

                if (drawnPx.Any(p => Math.Abs(p - px) < 0.5f))
                    return false;

                var txt = FormatAxisValue(displayValue);
                var sz = g.MeasureString(txt, tickFont, int.MaxValue, sf);
                float left = px - sz.Width / 2f;
                float right = px + sz.Width / 2f;
                if (!force && left <= lastRight + minLabelGapX)
                    return false;

                g.DrawString(txt, tickFont, Brushes.Black, left, xDigitsTop, sf);
                lastRight = Math.Max(lastRight, right);
                drawnPx.Add(px);
                return true;
            }

            TryDrawLabel(context.ScaleXMin, context.ScaleXMin, force: true);

            bool zeroWithinScale = context.ScaleXMin - eps <= 0 && context.ScaleXMax + eps >= 0;
            if (zeroWithinScale && 0 >= xMin - eps && 0 <= xMax + eps)
            {
                TryDrawLabel(0, 0);
            }

            // остальные major без налезаний
            var majorVisibleX = xTicks.MajorTicks
                .Select(t => new { T = t, Px = X(t.Position) })
                .Where(v => v.Px >= chartRect.Left - 1 && v.Px <= chartRect.Right + 1)
                .OrderBy(v => v.Px)
                .ToList();

            foreach (var v in majorVisibleX)
            {
                if (Math.Abs(v.T.DisplayValue) <= eps)
                    continue;

                TryDrawLabel(v.T.Position, v.T.DisplayValue);
            }

            TryDrawLabel(context.ScaleXMax, context.ScaleXMax, force: true);

            // X: единицы ("ms")
            var msSz = g.MeasureString("ms", unitFont, int.MaxValue, sf);
            g.DrawString("ms", unitFont, Brushes.Black,
                chartRect.Left + (chartRect.Width - msSz.Width) / 2f, xUnitsTop, sf);

            // ----- Y: цифры + единицы --------------------------------------------
            var visMajorY = yTicks.MajorTicks
                .Select(t => new { T = t, Py = Y(t.Position) })
                .Where(v => v.Py >= chartRect.Top - 1 && v.Py <= chartRect.Bottom + 1)
                .OrderBy(v => v.Py)
                .ToList();

            float lastYBottom = float.NegativeInfinity;
            float leftmostYTextX = float.PositiveInfinity;
            float textH = tickFont.GetHeight(g);

            foreach (var v in visMajorY)
            {
                var txt = FormatAxisValue(v.T.DisplayValue);
                var sz = g.MeasureString(txt, tickFont, int.MaxValue, sf);
                float top = v.Py - sz.Height / 2f;

                if (top <= lastYBottom + minLabelGapY && Math.Abs(v.T.Position) > eps) continue;

                float textX = yAxisX - majorTickLen - opt.YDigitsLeftPadPx - sz.Width;
                g.DrawString(txt, tickFont, Brushes.Black, textX, top, sf);
                leftmostYTextX = Math.Min(leftmostYTextX, textX);
                lastYBottom = top + sz.Height;
            }

            // гарантируем ноль
            if (0 >= yMin - eps && 0 <= yMax + eps)
            {
                var py = Y(0);
                var t0 = FormatAxisValue(0);
                var s0 = g.MeasureString(t0, tickFont, int.MaxValue, sf);
                float top = py - s0.Height / 2f;
                float textX = yAxisX - majorTickLen - opt.YDigitsLeftPadPx - s0.Width;
                g.DrawString(t0, tickFont, Brushes.Black, textX, top, sf);
                leftmostYTextX = Math.Min(leftmostYTextX, textX);
            }

            // µV — левее самой левой цифры
            var uvSz = g.MeasureString("µV", unitFont, int.MaxValue, sf);
            float centerY = chartRect.Top + chartRect.Height / 2f;

            float unitsCenterX;
            if (float.IsPositiveInfinity(leftmostYTextX))
                unitsCenterX = yAxisX - majorTickLen - opt.YUnitsFallbackFromAxisPx;
            else
                unitsCenterX = (leftmostYTextX - opt.YUnitsGapFromNumbersPx) - uvSz.Height / 2f;

            unitsCenterX = Math.Max(uvSz.Height / 2f + 4f, unitsCenterX);

            g.TranslateTransform(unitsCenterX, centerY);
            g.RotateTransform(-90f);
            g.DrawString("µV", unitFont, Brushes.Black, -uvSz.Width / 2f, -uvSz.Height / 2f, sf);
            g.ResetTransform();

            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return new GraphImage(ms.ToArray(), width, height);
        }
        catch (Exception ex) when (ex is ExternalException or ArgumentException or PlatformNotSupportedException)
        {
            RenderingSupport.DisableGraphRendering($"Построение графиков отключено: {ex.Message}");
            return null;
        }
    }
    private static GraphImage? TryRenderGraphImageWithSkia(ErgTest test, GraphRenderContext context)
    {
        try
        {
            var opt = GraphOptions;

            const int width = GraphImagePixelWidth;
            const int height = GraphImagePixelHeight;

            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            if (surface == null)
            {
                RenderingSupport.DisableGraphRendering("Не удалось инициализировать движок SkiaSharp для построения графиков.");
                return null;
            }

            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // поля и зазоры
            float marginLeft = opt.MarginLeft;
            float marginRight = opt.MarginRight;
            float marginTop = opt.MarginTop;
            float marginBottom = opt.MarginBottom;
            float axisGapHorizontal = opt.AxisGapHorizontal;
            float axisGapVertical = opt.AxisGapVertical;

            // риски (мм → px)
            float majorTickLength = MillimetersToPixels(opt.MajorTickLenMm);
            float minorTickLength = MillimetersToPixels(opt.MinorTickLenMm);

            float markerTopReserve = 0f;

            var chartRect = new SKRect(marginLeft, marginTop + markerTopReserve, width - marginRight, height - marginBottom);

            double xMin = context.XMin;
            double xMax = context.XMax;
            double yMin = context.YMin;
            double yMax = context.YMax;
            const double eps = 1e-6;

            float X(double value) => (float)(chartRect.Left + (value - xMin) / (xMax - xMin) * chartRect.Width);
            float Y(double value) => (float)(chartRect.Bottom - (value - yMin) / (yMax - yMin) * chartRect.Height);

            var xAxisTicks = BuildAxisTickSet(xMin, xMax, 0, test.GraphXValueStep, test.GraphXLineStep);
            var yAxisTicks = BuildAxisTickSet(yMin, yMax, 0, test.GraphYValueStep, test.GraphYLineStep);

            float xAxisY = chartRect.Bottom + axisGapVertical;
            float yAxisX = chartRect.Left - axisGapHorizontal;

            // сетка
            if (opt.GridThicknessPx > 0f)
            {
                using var gridPaint = new SKPaint { Color = new SKColor(230, 230, 230), StrokeWidth = opt.GridThicknessPx, IsAntialias = true };
                foreach (var line in xAxisTicks.GridLines)
                {
                    var px = X(line);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1) continue;
                    canvas.DrawLine(px, chartRect.Top, px, chartRect.Bottom, gridPaint);
                }
                foreach (var line in yAxisTicks.GridLines)
                {
                    if (Math.Abs(line) < eps) continue;
                    var py = Y(line);
                    if (py <= chartRect.Top + 1 || py >= chartRect.Bottom - 1) continue;
                    canvas.DrawLine(chartRect.Left, py, chartRect.Right, py, gridPaint);
                }
            }

            // оси
            using (var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = opt.AxisThicknessPx, IsAntialias = true })
            {
                canvas.DrawLine(chartRect.Left, xAxisY, chartRect.Right, xAxisY, axisPaint);
                canvas.DrawLine(yAxisX, chartRect.Top, yAxisX, chartRect.Bottom, axisPaint);
            }

            // риски
            using (var tickPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = opt.TickThicknessPx, IsAntialias = true })
            {
                foreach (var t in xAxisTicks.Ticks)
                {
                    var px = X(t.Position);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1) continue;
                    var len = t.IsMajor ? majorTickLength : minorTickLength;
                    canvas.DrawLine(px, xAxisY, px, xAxisY + len, tickPaint);
                }
                foreach (var t in yAxisTicks.Ticks)
                {
                    var py = Y(t.Position);
                    if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1) continue;
                    var len = t.IsMajor ? majorTickLength : minorTickLength;
                    canvas.DrawLine(yAxisX, py, yAxisX - len, py, tickPaint);
                }
            }

            // вертикальные маркеры (a/b)
            if (context.Markers.Length > 0 && opt.ExtremumThicknessPx > 0f)
            {
                using var markerPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    StrokeWidth = Math.Max(1f, opt.ExtremumThicknessPx),
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round,
                    PathEffect = CreateDottedPathEffect(opt)
                };

                foreach (var m in context.Markers)
                {
                    var px = X(m.PositionMs);
                    if (double.IsNaN(px) || double.IsInfinity(px)) continue;
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1) continue;

                    canvas.DrawLine(px, chartRect.Top, px, chartRect.Bottom, markerPaint);
                }
            }

            // горизонтальные маркеры (уровни a/b)
            if (context.WaveLevels.Length > 0 && opt.HorizontalMarkerThicknessPx > 0f)
            {
                using var levelPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    StrokeWidth = Math.Max(1f, opt.HorizontalMarkerThicknessPx),
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round,
                    PathEffect = CreateDottedPathEffect(opt)
                };

                foreach (var level in context.WaveLevels)
                {
                    if (!IsWithinAxis(level.Value, yMin, yMax)) continue;
                    var py = Y(level.Value);
                    if (py <= chartRect.Top + 1 || py >= chartRect.Bottom - 1) continue;

                    canvas.DrawLine(chartRect.Left, py, chartRect.Right, py, levelPaint);
                }
            }

            // кривые
            canvas.Save();
            canvas.ClipRect(chartRect, SKClipOperation.Intersect, antialias: true);

            double dt = test.GraphDt;
            bool hasDt = dt > 0;
            var styles = test.GraphStyles ?? Array.Empty<GraphStyle>();
            double flashOffset = context.FlashOffset;

            for (int gi = 0; gi < context.Curves; gi++)
            {
                var s = context.Graphs[gi];
                if (s == null || s.Length == 0) continue;

                int n = context.DeclaredPointCount > 1 ? Math.Min(context.DeclaredPointCount, s.Length) : s.Length;
                if (n < 2) continue;

                using var path = new SKPath();
                bool started = false;
                for (int i = 0; i < n; i++)
                {
                    double xv;
                    if (hasDt)
                    {
                        xv = i * dt - flashOffset;
                        if (xv < xMin) continue;
                        if (xv > xMax) break;
                    }
                    else
                    {
                        xv = (n == 1) ? xMin : xMin + (xMax - xMin) * i / (n - 1);
                    }

                    var px = X(xv);
                    var py = Y(s[i]);
                    if (double.IsNaN(px) || double.IsNaN(py) || double.IsInfinity(px) || double.IsInfinity(py)) continue;

                    if (!started) { path.MoveTo(px, py); started = true; }
                    else path.LineTo(px, py);
                }
                if (!started) continue;

                var st = gi < styles.Length ? styles[gi] : null;
                var col = st != null ? new SKColor(st.Red, st.Green, st.Blue) : new SKColor(56, 109, 179);

                using var linePaint = new SKPaint
                {
                    Color = col,
                    StrokeWidth = opt.CurveThicknessPx,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeJoin = SKStrokeJoin.Round,
                    StrokeCap = SKStrokeCap.Round
                };
                if (st?.Dotted == true)
                    linePaint.PathEffect = CreateDottedPathEffect(opt);

                canvas.DrawPath(path, linePaint);
            }

            canvas.Restore();

            // ===== подписи осей =====
            float leftmostYTextX = float.PositiveInfinity;
            float xDigitsTop = xAxisY + majorTickLength + opt.XDigitsOffsetPx;
            float xUnitsBaseline = 0f;

            using var unitPaint = SkTextPaint(opt.UnitsFontPt, SKColors.Black);
            var unitFm = unitPaint.FontMetrics;
            float unitsHeight = unitFm.Descent - unitFm.Ascent;

            using (var labelPaint = SkTextPaint(opt.LabelFontPt, SKColors.Black))
            {
                var fm = labelPaint.FontMetrics;
                float textHeight = fm.Descent - fm.Ascent;
                const float minLabelGapY = 4f;

                float xUnitsTop = xDigitsTop + textHeight + opt.XUnitsGapPx;
                float maxUnitsTop = height - 6f - unitsHeight;
                if (xUnitsTop > maxUnitsTop)
                {
                    float shiftUp = xUnitsTop - maxUnitsTop;
                    xDigitsTop -= shiftUp;
                    xUnitsTop = maxUnitsTop;
                }

                float xDigitsBaseline = xDigitsTop - fm.Ascent;
                xUnitsBaseline = xUnitsTop - unitFm.Ascent;
                float minGapX = opt.MinLabelGapXPx;

                // ----- X: цифры -----
                float lastRight = float.NegativeInfinity;
                var drawnPx = new List<float>();

                bool TryDrawLabel(double coordinate, double displayValue, bool force = false)
                {
                    if (double.IsNaN(coordinate) || double.IsNaN(displayValue))
                        return false;
                    if (double.IsInfinity(coordinate) || double.IsInfinity(displayValue))
                        return false;

                    if (displayValue < context.ScaleXMin - eps || displayValue > context.ScaleXMax + eps)
                        return false;
                    if (coordinate < xMin - eps || coordinate > xMax + eps)
                        return false;

                    var px = X(coordinate);
                    if (float.IsNaN(px) || float.IsInfinity(px))
                        return false;

                    if (drawnPx.Any(p => Math.Abs(p - px) < 0.5f))
                        return false;

                    var txt = FormatAxisValue(displayValue);
                    float w = labelPaint.MeasureText(txt);
                    float left = px - w / 2f;
                    float right = px + w / 2f;
                    if (!force && left <= lastRight + minGapX)
                        return false;

                    canvas.DrawText(txt, px - w / 2f, xDigitsBaseline, labelPaint);
                    lastRight = Math.Max(lastRight, right);
                    drawnPx.Add(px);
                    return true;
                }

                TryDrawLabel(context.ScaleXMin, context.ScaleXMin, force: true);

                bool zeroWithinScale = context.ScaleXMin - eps <= 0 && context.ScaleXMax + eps >= 0;
                if (zeroWithinScale && 0 >= xMin - eps && 0 <= xMax + eps)
                {
                    TryDrawLabel(0, 0);
                }

                var majorVisibleX = xAxisTicks.MajorTicks
                    .Select(t => new { T = t, Px = X(t.Position) })
                    .Where(v => v.Px >= chartRect.Left - 1 && v.Px <= chartRect.Right + 1)
                    .OrderBy(v => v.Px)
                    .ToList();

                foreach (var v in majorVisibleX)
                {
                    if (Math.Abs(v.T.DisplayValue) <= eps)
                        continue;

                    TryDrawLabel(v.T.Position, v.T.DisplayValue);
                }

                TryDrawLabel(context.ScaleXMax, context.ScaleXMax, force: true);

                // ----- Y: цифры -----
                var visMajorY = yAxisTicks.MajorTicks
                    .Select(t => new { T = t, Py = Y(t.Position) })
                    .Where(v => v.Py >= chartRect.Top - 1 && v.Py <= chartRect.Bottom + 1)
                    .OrderBy(v => v.Py)
                    .ToList();

                float lastYBottom = float.NegativeInfinity;
                bool zeroDrawn = false;

                foreach (var v in visMajorY)
                {
                    var txt = FormatAxisValue(v.T.DisplayValue);
                    float w = labelPaint.MeasureText(txt);
                    float top = v.Py - textHeight / 2f;

                    if (top <= lastYBottom + minLabelGapY && Math.Abs(v.T.Position) > eps) continue;

                    float textX = yAxisX - majorTickLength - opt.YDigitsLeftPadPx - w;
                    float baseline = top - fm.Ascent;
                    canvas.DrawText(txt, textX, baseline, labelPaint);
                    leftmostYTextX = Math.Min(leftmostYTextX, textX);
                    lastYBottom = top + textHeight;

                    if (Math.Abs(v.T.Position) <= eps) zeroDrawn = true;
                }

                if (!zeroDrawn && 0 >= yMin - eps && 0 <= yMax + eps)
                {
                    var py = Y(0);
                    var txt = FormatAxisValue(0);
                    float w = labelPaint.MeasureText(txt);
                    float top = py - textHeight / 2f;
                    float textX = yAxisX - majorTickLength - opt.YDigitsLeftPadPx - w;
                    float baseline = top - fm.Ascent;
                    canvas.DrawText(txt, textX, baseline, labelPaint);
                    leftmostYTextX = Math.Min(leftmostYTextX, textX);
                }
            }

            // единицы измерения
            var xLabel = "ms";
            float xLabelWidth = unitPaint.MeasureText(xLabel);
            float midX = (chartRect.Left + chartRect.Right) / 2f;
            canvas.DrawText(xLabel, midX - xLabelWidth / 2f, xUnitsBaseline, unitPaint);

            float uvWidth = unitPaint.MeasureText("µV");
            float uvHeight = unitsHeight;
            float unitsX = float.IsPositiveInfinity(leftmostYTextX)
                ? yAxisX - majorTickLength - opt.YUnitsFallbackFromAxisPx
                : (leftmostYTextX - opt.YUnitsGapFromNumbersPx) - uvHeight / 2f;
            unitsX = Math.Max(uvHeight / 2f + 4f, unitsX);

            float centerY = (chartRect.Top + chartRect.Bottom) / 2f;

            canvas.Save();
            canvas.Translate(unitsX, centerY);
            canvas.RotateDegrees(-90);
            float uvBaseline = -uvHeight / 2f - unitFm.Ascent;
            canvas.DrawText("µV", -uvWidth / 2f, uvBaseline, unitPaint);
            canvas.Restore();

            using var snapshot = surface.Snapshot();
            using var data = snapshot.Encode(SKEncodedImageFormat.Png, 90);
            if (data == null) return null;
            return new GraphImage(data.ToArray(), width, height);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or TypeInitializationException or NotSupportedException)
        {
            RenderingSupport.DisableGraphRendering($"Построение графиков отключено: {ex.Message}");
            return null;
        }
    }

    private static int TwipsFromCentimeters(double centimeters)
        => (int)Math.Round(centimeters / 2.54 * 1440);

    private static int TwipsFromPoints(double points) => (int)Math.Round(points * 20);

    private static void WriteCoreProperties(CoreFilePropertiesPart part, string? title)
    {
        if (part == null)
            return;

        var document = BuildCorePropertiesDocument(title);
        using var stream = part.GetStream(FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        document.Save(writer);
    }

    private static void WriteExtendedProperties(ExtendedFilePropertiesPart part)
    {
        if (part == null)
            return;

        var document = BuildExtendedPropertiesDocument();
        using var stream = part.GetStream(FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        document.Save(writer);
    }

    public static void BuildPatientReport(
        ErgPatient patient,
        string pdfPath,
        CommonInfo? deviceInfo = null,
        string? clinicName = null,
        string? rawFilePath = null,
        ReportTemplate template = ReportTemplate.Classic)
    {
        if (patient == null) throw new ArgumentNullException(nameof(patient));
        if (string.IsNullOrWhiteSpace(pdfPath)) throw new ArgumentNullException(nameof(pdfPath));

        var directory = Path.GetDirectoryName(pdfPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (RenderingSupport.UseLegacyPdfGeneration)
        {
            BuildPatientReportLegacyPdf(patient, pdfPath, deviceInfo, clinicName, rawFilePath, template);
            return;
        }

        switch (template)
        {
            case ReportTemplate.Classic:
                BuildPatientReportQuestPdfClassic(patient, pdfPath, deviceInfo, clinicName, rawFilePath);
                break;
            case ReportTemplate.Client:
                BuildPatientReportQuestPdfClient(patient, pdfPath, deviceInfo, clinicName, rawFilePath);
                break;
            default:
                BuildPatientReportQuestPdfClassic(patient, pdfPath, deviceInfo, clinicName, rawFilePath);
                break;
        }
    }

    public static void BuildPatientWordReport(
        ErgPatient patient,
        string docxPath,
        CommonInfo? deviceInfo = null,
        string? clinicName = null,
        string? rawFilePath = null,
        ReportTemplate template = ReportTemplate.Classic)
    {
        if (patient == null) throw new ArgumentNullException(nameof(patient));
        if (string.IsNullOrWhiteSpace(docxPath)) throw new ArgumentNullException(nameof(docxPath));

        var directory = Path.GetDirectoryName(docxPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        switch (template)
        {
            case ReportTemplate.Classic:
                BuildPatientWordReportClassic(patient, docxPath, deviceInfo, clinicName, rawFilePath);
                break;
            case ReportTemplate.Client:
                BuildPatientWordReportClient(patient, docxPath, deviceInfo, clinicName, rawFilePath);
                break;
            default:
                BuildPatientWordReportClassic(patient, docxPath, deviceInfo, clinicName, rawFilePath);
                break;
        }
    }


    // Утилита для превью (PNG байты), чтобы UI мог быстро получить картинку
    public static byte[]? RenderGraphPng(ErgTest test, EyeData eye)
    {
        var img = TryRenderGraphImage(test, eye); // приватный метод того же класса
        return img?.Data;
    }

    public static GraphRenderOptions GraphOptions { get; } = new GraphRenderOptions();

    private sealed class LegacyPdfRenderer : IDisposable
    {
        private const int Dpi = 200;
        private const int PageHeight = (int)(11.69f * Dpi);
        private const int PageWidth = (int)(8.27f * Dpi);

        private Bitmap? _bitmap;

        private readonly DrawingFont _clinicFont = new("Arial", 10f, FontStyle.Regular, GraphicsUnit.Point);

        private readonly string[] _clinicHeaderLines;
        private readonly SolidBrush _descriptionBackgroundBrush = new(DrawingColor.FromArgb(245, 245, 245));
        private readonly DrawingFont _descriptionFont = new("Arial", 10f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly DrawingFont _descriptionTitleFont = new("Arial", 12f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly string _descriptionTitleText;
        private readonly CommonInfo? _deviceInfo;
        private readonly DrawingFont _eyeLabelFont = new("Arial", 11f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly DrawingFont _waveLabelFont = new("Arial", 11f, FontStyle.Regular, GraphicsUnit.Point);

        private readonly StringFormat _formatCenter = new(StringFormatFlags.LineLimit)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.Word
        };

        private readonly StringFormat _formatLeft = new(StringFormatFlags.LineLimit)
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.Word
        };

        private readonly StringFormat _formatLeftCenter = new(StringFormatFlags.LineLimit)
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.Word
        };

        private readonly StringFormat _formatRight = new(StringFormatFlags.LineLimit)
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.Word
        };
        private readonly float _graphGap = 0.12f * Dpi;
        private Graphics? _graphics;
        private readonly DrawingFont _infoLabelFont = new("Arial", 11f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly DrawingFont _infoSmallFont = new("Arial", 9f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly DrawingFont _infoValueFont = new("Arial", 11f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly float _marginBottom;

        private readonly float _marginLeft;
        private readonly float _marginRight;
        private readonly float _marginTop;

        private readonly SolidBrush _mutedBrush = new(DrawingColor.FromArgb(100, 100, 100));
        private readonly DrawingFont _normFont = new("Arial", 9f, FontStyle.Regular, GraphicsUnit.Point);

        private readonly List<byte[]> _pages = new();

        private readonly ErgPatient _patient;
        private readonly string _pdfPath;
        private readonly DrawingFont _placeholderFont = new("Arial", 10f, FontStyle.Italic, GraphicsUnit.Point);
        private readonly string _reportTitle;
        private readonly DrawingFont _reportTitleFont = new("Arial", 14f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly string? _reportVersion;
        private readonly float _spacingLarge = 0.18f * Dpi;
        private readonly float _spacingMedium = 0.11f * Dpi;
        private readonly float _spacingSmall = 0.07f * Dpi;
        private readonly float _summarySpacingMedium = 0.08f * Dpi;
        private readonly float _summarySpacingSmall = 0.05f * Dpi;
        private readonly ReportTemplate _template;
        private readonly SolidBrush _testHeaderBackgroundBrush = new(DrawingColor.FromArgb(0xCC, 0xCC, 0xCC));
        private readonly DrawingFont _testTitleFont = new("Arial", 12f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly DrawingFont _unitFont = new("Arial", 12f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly bool _useDescriptionBackground;
        private readonly DrawingFont _valueFont = new("Arial", 24f, FontStyle.Bold, GraphicsUnit.Point);
        private float _y;

        public LegacyPdfRenderer(ErgPatient patient, string pdfPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath, ReportTemplate template)
        {
            _patient = patient;
            _pdfPath = pdfPath;
            _deviceInfo = deviceInfo;
            _template = template;

            _clinicHeaderLines = PrepareClinicHeaderLines(BuildClinicHeaderLines(clinicName));
            _reportTitle = !string.IsNullOrWhiteSpace(deviceInfo?.ReportName)
                ? deviceInfo!.ReportName!
                : "Отчет по результатам ЭРГ-исследования сетчатки";

            var version = GetApplicationVersion();
            _reportVersion = string.IsNullOrWhiteSpace(version) || version == "—" ? null : version;

            var marginPoints = MillimetersToPoints(20d);
            var margin = PointsToPixels(marginPoints);
            _marginLeft = margin;
            _marginRight = margin;
            _marginTop = margin;
            _marginBottom = margin;

            _descriptionTitleText = template == ReportTemplate.Client
                ? "Заключение:"
                : "Автоматическое заключение";
            _useDescriptionBackground = template != ReportTemplate.Client;
        }

        private static string BuildEyeLabel(string label, EyeData eye)
        {
            var quality = FormatQualityCompact(eye.QualityIndex);
            return quality != null ? $"{label} {quality}" : label;
        }

        private static float CalculateGraphHeight(GraphImage graph, float targetWidth)
        {
            if (graph.Width <= 0 || graph.Height <= 0)
                return targetWidth * 0.6f;

            return graph.Height / (float)graph.Width * targetWidth;
        }

        private float CalculateNormHeight(string? msText, string? mkvText, float halfWidth)
        {
            if (string.IsNullOrWhiteSpace(msText) && string.IsNullOrWhiteSpace(mkvText))
                return 0f;

            float height = 0f;
            if (!string.IsNullOrWhiteSpace(msText))
                height = Math.Max(height, MeasureText(msText!, _normFont, halfWidth, _formatCenter));
            if (!string.IsNullOrWhiteSpace(mkvText))
                height = Math.Max(height, MeasureText(mkvText!, _normFont, halfWidth, _formatCenter));
            return height;
        }

        private void DrawCenteredValue(float left, float width, ref float cursor, string text)
        {
            if (_graphics == null)
                return;

            var height = _valueFont.GetHeight(_graphics);
            var rect = new RectangleF(left, cursor, width, height);
            _graphics.DrawString(text, _valueFont, Brushes.Black, rect, _formatCenter);
            cursor += height;
        }

        private void DrawDescription()
        {
            if (_graphics == null || string.IsNullOrWhiteSpace(_patient.Description))
                return;

            var descriptionText = NormalizeDescription(_patient.Description);

            if (_template == ReportTemplate.Client)
            {
                var titleHeight = MeasureText(_descriptionTitleText, _descriptionTitleFont, ContentWidth, _formatLeft);
                var textHeight = MeasureText(descriptionText, _descriptionFont, ContentWidth, _formatLeft);
                var blockHeight = titleHeight + textHeight + _summarySpacingSmall * 2f;

                EnsureSpace(blockHeight + _spacingMedium);

                var titleRect = new RectangleF(_marginLeft, _y, ContentWidth, titleHeight);
                _graphics.DrawString(_descriptionTitleText, _descriptionTitleFont, Brushes.Black, titleRect, _formatLeft);

                var textRect = new RectangleF(
                    _marginLeft,
                    titleRect.Bottom + _summarySpacingSmall,
                    ContentWidth,
                    textHeight);
                _graphics.DrawString(descriptionText, _descriptionFont, Brushes.Black, textRect, _formatLeft);

                _y += blockHeight + _spacingMedium;
                return;
            }

            var innerWidth = ContentWidth - _summarySpacingSmall * 2f;
            var titleHeightLegacy = MeasureText(_descriptionTitleText, _descriptionTitleFont, innerWidth, _formatLeft);
            var textHeightLegacy = MeasureText(descriptionText, _descriptionFont, innerWidth, _formatLeft);
            var blockHeightLegacy = titleHeightLegacy + textHeightLegacy + _summarySpacingSmall * 3f;

            EnsureSpace(blockHeightLegacy + _spacingMedium);

            var outerRect = new RectangleF(_marginLeft, _y, ContentWidth, blockHeightLegacy);
            if (_useDescriptionBackground)
            {
                _graphics!.FillRectangle(_descriptionBackgroundBrush, outerRect);
            }

            var titleRectLegacy = new RectangleF(
                _marginLeft + _summarySpacingSmall,
                _y + _summarySpacingSmall,
                innerWidth,
                titleHeightLegacy);
            _graphics!.DrawString(_descriptionTitleText, _descriptionTitleFont, Brushes.Black, titleRectLegacy, _formatLeft);

            var textRectLegacy = new RectangleF(
                _marginLeft + _summarySpacingSmall,
                titleRectLegacy.Bottom + _summarySpacingSmall * 0.5f,
                innerWidth,
                textHeightLegacy);
            _graphics.DrawString(descriptionText, _descriptionFont, Brushes.Black, textRectLegacy, _formatLeft);

            _y += blockHeightLegacy + _spacingMedium;
        }

        private void DrawEyeSummary(RectangleF rect, string label, ErgTest test, EyeData eye)
        {
            if (_graphics == null)
                return;

            float cursor = rect.Top;
            var labelText = BuildEyeLabel(label, eye);
            var labelHeight = MeasureText(labelText, _eyeLabelFont, rect.Width, _formatCenter);
            var labelRect = new RectangleF(rect.Left, cursor, rect.Width, labelHeight);
            _graphics.DrawString(labelText, _eyeLabelFont, Brushes.Black, labelRect, _formatCenter);
            cursor += labelHeight + _summarySpacingSmall;

            if (eye.IsFlat)
            {
                DrawCenteredValue(rect.Left, rect.Width, ref cursor, "FLAT");
                cursor += _summarySpacingSmall;
                return;
            }

            if (!EyeHasUsableMeasurements(eye))
            {
                cursor += _summarySpacingSmall;
                return;
            }

            bool hasContent = false;
            float labelColumnWidth = GetEyeLabelColumnWidth(rect.Width);
            float valuesWidth = Math.Max(0f, rect.Width - labelColumnWidth);
            if (valuesWidth <= 0f)
            {
                valuesWidth = rect.Width;
                labelColumnWidth = 0f;
            }

            float spacing = _summarySpacingSmall * 0.5f;

            foreach (var (waveLabel, kind) in GetClientWaveOrder(test))
            {
                var display = BuildWaveDisplay(test, eye, kind);
                if (IsWaveDisplayEmpty(display))
                    continue;

                cursor += spacing;

                float measurementHeight = display.IsFlat
                    ? _valueFont.GetHeight(_graphics)
                    : Math.Max(_valueFont.GetHeight(_graphics), _unitFont.GetHeight(_graphics));

                if (!string.IsNullOrWhiteSpace(waveLabel) && labelColumnWidth > 0f)
                {
                    var waveRect = new RectangleF(rect.Left, cursor, labelColumnWidth, measurementHeight);
                    _graphics.DrawString(waveLabel, _waveLabelFont, Brushes.Black, waveRect, _formatLeftCenter);
                }

                var valueArea = new RectangleF(rect.Left + labelColumnWidth, cursor, valuesWidth, measurementHeight);

                if (display.IsFlat)
                {
                    _graphics.DrawString("FLAT", _valueFont, Brushes.Black, valueArea, _formatCenter);
                    cursor += measurementHeight + spacing;
                    hasContent = true;
                    continue;
                }

                var centers = DrawMeasurementPair(valueArea, display.MsValue, display.MkVValue);
                cursor += measurementHeight;

                var msText = FormatNormForClient(display.MsNorm);
                var mkvText = FormatNormForClient(display.MkVNorm);
                var normHeight = CalculateNormHeight(msText, mkvText, valuesWidth / 2f);
                if (normHeight > 0f)
                {
                    var normRect = new RectangleF(valueArea.Left, cursor, valuesWidth, normHeight);
                    DrawNormPair(normRect, msText, mkvText, centers.MsCenter, centers.MkVCenter);
                    cursor += normHeight;
                }

                cursor += spacing;
                hasContent = true;
            }

            if (!hasContent)
                cursor += _summarySpacingSmall;
        }

        private void DrawEyeSummaryRow(ErgTest test)
        {
            if (_graphics == null)
                return;

            var columnWidth = (ContentWidth - _graphGap) / 2f;
            var rightHeight = MeasureEyeSummaryHeight("Правый глаз", test, test.RightEye, columnWidth);
            var leftHeight = MeasureEyeSummaryHeight("Левый глаз", test, test.LeftEye, columnWidth);
            var height = Math.Max(rightHeight, leftHeight);

            if (height <= 0f)
                height = _valueFont.GetHeight(_graphics) + _summarySpacingSmall;

            EnsureSpace(height + _summarySpacingMedium);

            var top = _y;
            var rightRect = new RectangleF(_marginLeft, top, columnWidth, height);
            var leftRect = new RectangleF(_marginLeft + columnWidth + _graphGap, top, columnWidth, height);

            DrawEyeSummary(rightRect, "Правый глаз", test, test.RightEye);
            DrawEyeSummary(leftRect, "Левый глаз", test, test.LeftEye);

            _y = top + height + _summarySpacingMedium;
        }

        private void DrawGraphColumn(RectangleF rect, GraphImage? image, float graphHeight)
        {
            if (_graphics == null)
                return;

            var graphRect = new RectangleF(rect.Left, rect.Top, rect.Width, graphHeight);

            if (image != null)
            {
                using var stream = new MemoryStream(image.Data);
                using var bitmap = DrawingImage.FromStream(stream);
                var scale = Math.Min(graphRect.Width / image.Width, graphRect.Height / image.Height);
                var drawWidth = image.Width * scale;
                var drawHeight = image.Height * scale;
                var drawX = graphRect.Left + (graphRect.Width - drawWidth) / 2f;
                var drawY = graphRect.Top + (graphRect.Height - drawHeight) / 2f;
                _graphics.DrawImage(bitmap, drawX, drawY, drawWidth, drawHeight);
            }
            else
            {
                _graphics.DrawString("Нет данных", _placeholderFont, _mutedBrush, graphRect, _formatCenter);
            }
        }

        private void DrawGraphSection(ErgTest test, GraphImage? rightGraph, GraphImage? leftGraph)
        {
            if (_graphics == null)
                return;

            var columnWidth = (ContentWidth - _graphGap) / 2f;
            float graphHeight = 0f;
            if (rightGraph != null)
                graphHeight = Math.Max(graphHeight, CalculateGraphHeight(rightGraph, columnWidth));
            if (leftGraph != null)
                graphHeight = Math.Max(graphHeight, CalculateGraphHeight(leftGraph, columnWidth));
            if (graphHeight <= 0f)
                graphHeight = columnWidth * 0.55f;

            var totalHeight = graphHeight;

            EnsureSpace(totalHeight + _spacingSmall);

            var top = _y;

            if (rightGraph == null && leftGraph == null)
            {
                var rect = new RectangleF(_marginLeft, top, ContentWidth, totalHeight);
                _graphics.DrawString("Нет данных", _placeholderFont, _mutedBrush, rect, _formatCenter);
                _y = top + totalHeight + _spacingSmall;
                return;
            }

            var rightRect = new RectangleF(_marginLeft, top, columnWidth, totalHeight);
            var leftRect = new RectangleF(_marginLeft + columnWidth + _graphGap, top, columnWidth, totalHeight);

            DrawGraphColumn(rightRect, rightGraph, graphHeight);
            DrawGraphColumn(leftRect, leftGraph, graphHeight);

            _y = top + totalHeight + _spacingSmall;
        }

        private void DrawInfoBlock(bool allowPageBreak = true)
        {
            var lines = new List<(string Label, string Value)>
            {
                ("ID пациента:", $"{_patient.PatientId} ({FormatAnimal(_patient.Animal)})"),
                ("Дата и время исследования:", FormatClientDateTime(_patient.TestDateTime)),
                ("Оборудование:", GetClientDeviceName(_deviceInfo))
            };

            var software = GetClientSoftwareVersion(_deviceInfo);
            if (!string.IsNullOrWhiteSpace(software))
            {
                lines.Add(("Версия ПО:", software!));
            }

            foreach (var (label, value) in lines)
            {
                DrawInfoLine(label, string.IsNullOrWhiteSpace(value) ? "—" : value, allowPageBreak);
            }

            _y += _spacingSmall * 0.5f;
        }

        private void DrawInfoLine(string label, string value, bool allowPageBreak)
        {
            if (_graphics == null)
                return;

            var labelWidth = Math.Min(ContentWidth * 0.45f, _graphics.MeasureString(label, _infoLabelFont).Width + 4f);
            var lineHeight = Math.Max(_infoLabelFont.GetHeight(_graphics), _infoValueFont.GetHeight(_graphics));

            if (allowPageBreak)
                EnsureSpace(lineHeight + _summarySpacingSmall * 0.5f);

            var labelRect = new RectangleF(_marginLeft, _y, labelWidth, lineHeight);
            _graphics.DrawString(label, _infoLabelFont, Brushes.Black, labelRect, _formatLeft);

            var valueRect = new RectangleF(_marginLeft + labelWidth + 6f, _y, ContentWidth - labelWidth - 6f, lineHeight);
            _graphics.DrawString(value, _infoValueFont, Brushes.Black, valueRect, _formatLeft);

            _y += lineHeight + _summarySpacingSmall * 0.5f;
        }

        private (float MsCenter, float MkVCenter) DrawMeasurementPair(RectangleF rect, string msValue, string mkvValue)
        {
            var halfWidth = rect.Width / 2f;
            var leftRect = new RectangleF(rect.Left, rect.Top, halfWidth, rect.Height);
            var rightRect = new RectangleF(rect.Left + halfWidth, rect.Top, halfWidth, rect.Height);

            var msCenter = DrawMeasurementValue(leftRect, msValue);
            var mkvCenter = DrawMeasurementValue(rightRect, mkvValue);

            return (msCenter, mkvCenter);
        }

        private float DrawMeasurementValue(RectangleF rect, string text)
        {
            if (_graphics == null)
                return rect.Left + rect.Width / 2f;

            if (text == "FLAT")
            {
                _graphics.DrawString(text, _valueFont, Brushes.Black, rect, _formatCenter);
                return rect.Left + rect.Width / 2f;
            }

            var parts = SplitValueAndUnit(text);
            var valueSize = _graphics.MeasureString(parts.Value, _valueFont);
            var unitSize = string.IsNullOrEmpty(parts.Unit) ? SizeF.Empty : _graphics.MeasureString(parts.Unit, _unitFont);
            float unitGap = 0f;
            if (!string.IsNullOrEmpty(parts.Unit))
            {
                const float UnitGapFactor = 0.25f;
                var measuredSpace = _graphics.MeasureString(" ", _unitFont).Width;
                unitGap = Math.Max(0f, measuredSpace * UnitGapFactor);
            }

            var totalWidth = valueSize.Width + (string.IsNullOrEmpty(parts.Unit) ? 0f : unitGap + unitSize.Width);
            var startX = rect.Left + (rect.Width - totalWidth) / 2f;

            var valueRect = new RectangleF(startX, rect.Top + (rect.Height - _valueFont.GetHeight(_graphics)) / 2f, valueSize.Width, _valueFont.GetHeight(_graphics));
            _graphics.DrawString(parts.Value, _valueFont, Brushes.Black, valueRect, _formatLeft);

            float valueCenter = valueRect.Left + valueRect.Width / 2f;

            if (!string.IsNullOrEmpty(parts.Unit))
            {
                var unitHeight = _unitFont.GetHeight(_graphics);
                var valueBaselineOffset = GetBaselineOffset(_valueFont, _graphics);
                var unitBaselineOffset = GetBaselineOffset(_unitFont, _graphics);
                var unitTop = valueRect.Top + valueBaselineOffset - unitBaselineOffset;

                var unitRect = new RectangleF(
                    startX + valueSize.Width + unitGap,
                    unitTop,
                    unitSize.Width,
                    unitHeight);
                _graphics.DrawString(parts.Unit, _unitFont, Brushes.Black, unitRect, _formatLeft);
            }

            return valueCenter;
        }

        private void DrawNormPair(RectangleF rect, string? msText, string? mkvText, float msCenter, float mkvCenter)
        {
            if (_graphics == null)
                return;

            var halfWidth = rect.Width / 2f;
            var leftRect = new RectangleF(rect.Left, rect.Top, halfWidth, rect.Height);
            var rightRect = new RectangleF(rect.Left + halfWidth, rect.Top, halfWidth, rect.Height);

            if (!string.IsNullOrWhiteSpace(msText))
            {
                var size = _graphics.MeasureString(msText!, _normFont);
                var x = msCenter - size.Width / 2f;
                var min = leftRect.Left;
                var max = leftRect.Right - size.Width;
                if (x < min)
                    x = min;
                if (x > max)
                    x = max;
                var textRect = new RectangleF(x, rect.Top, size.Width, rect.Height);
                _graphics.DrawString(msText!, _normFont, _mutedBrush, textRect, _formatCenter);
            }
            if (!string.IsNullOrWhiteSpace(mkvText))
            {
                var size = _graphics.MeasureString(mkvText!, _normFont);
                var x = mkvCenter - size.Width / 2f;
                var min = rightRect.Left;
                var max = rightRect.Right - size.Width;
                if (x < min)
                    x = min;
                if (x > max)
                    x = max;
                var textRect = new RectangleF(x, rect.Top, size.Width, rect.Height);
                _graphics.DrawString(mkvText!, _normFont, _mutedBrush, textRect, _formatCenter);
            }
        }

        private void DrawParagraph(string text, DrawingFont font, Brush brush, float spacingBefore, float spacingAfter, StringFormat? format = null)
        {
            if (_graphics == null)
                return;

            if (string.IsNullOrWhiteSpace(text))
            {
                _y += spacingBefore + spacingAfter;
                return;
            }

            format ??= _formatLeft;
            var height = MeasureText(text, font, ContentWidth, format);
            EnsureSpace(spacingBefore + height + spacingAfter);
            _y += spacingBefore;
            var rect = new RectangleF(_marginLeft, _y, ContentWidth, height);
            _graphics.DrawString(text, font, brush, rect, format);
            _y += height + spacingAfter;
        }

        private void DrawPlaceholder(float left, float width, ref float cursor)
        {
            if (_graphics == null)
                return;

            var height = _placeholderFont.GetHeight(_graphics);
            var rect = new RectangleF(left, cursor, width, height);
            _graphics.DrawString("Нет данных", _placeholderFont, _mutedBrush, rect, _formatCenter);
            cursor += height;
        }

        private void DrawTestHeader(string title)
        {
            if (_graphics == null)
                return;

            float spacingBefore = _spacingLarge * 0.4f;
            float spacingAfter = Math.Max(_summarySpacingSmall, LayoutMillimetersToPixels(2d));

            if (string.IsNullOrWhiteSpace(title))
            {
                _y += spacingBefore + spacingAfter;
                return;
            }

            float padding = _summarySpacingSmall * 0.6f;
            float textWidth = ContentWidth - padding * 2f;
            if (textWidth <= 0f)
                textWidth = ContentWidth;

            float textHeight = MeasureText(title, _testTitleFont, textWidth, _formatCenter);
            float blockHeight = textHeight + padding * 2f;

            EnsureSpace(spacingBefore + blockHeight + spacingAfter);
            _y += spacingBefore;

            var rect = new RectangleF(_marginLeft, _y, ContentWidth, blockHeight);
            _graphics.FillRectangle(_testHeaderBackgroundBrush, rect);

            var textRect = new RectangleF(rect.Left, rect.Top, rect.Width, rect.Height);
            _graphics.DrawString(title, _testTitleFont, Brushes.Black, textRect, _formatCenter);

            _y += blockHeight + spacingAfter;
        }

        private void DrawTestSection(int index, ErgTest test)
        {
            var title = FormatClientTestTitle(index + 1, test);
            var rightGraph = TryRenderGraphImage(test, test.RightEye);
            var leftGraph = TryRenderGraphImage(test, test.LeftEye);
            var requiredHeight = MeasureTestSectionHeight(title, test, rightGraph, leftGraph);
            EnsureSpace(requiredHeight);
            DrawTestHeader(title);
            DrawEyeSummaryRow(test);
            DrawGraphSection(test, rightGraph, leftGraph);
        }

        private float MeasureTestSectionHeight(string title, ErgTest test, GraphImage? rightGraph, GraphImage? leftGraph)
        {
            if (_graphics == null)
                return 0f;

            float total = 0f;
            float spacingBefore = _spacingLarge * 0.4f;
            float spacingAfter = Math.Max(_summarySpacingSmall, LayoutMillimetersToPixels(2d));

            if (string.IsNullOrWhiteSpace(title))
            {
                total += spacingBefore + spacingAfter;
            }
            else
            {
                float padding = _summarySpacingSmall * 0.6f;
                float textWidth = ContentWidth - padding * 2f;
                if (textWidth <= 0f)
                    textWidth = ContentWidth;
                float textHeight = MeasureText(title, _testTitleFont, textWidth, _formatCenter);
                float blockHeight = textHeight + padding * 2f;
                total += spacingBefore + blockHeight + spacingAfter;
            }

            var columnWidth = (ContentWidth - _graphGap) / 2f;
            if (columnWidth <= 0f)
                columnWidth = ContentWidth / 2f;

            var rightHeight = MeasureEyeSummaryHeight("Правый глаз", test, test.RightEye, columnWidth);
            var leftHeight = MeasureEyeSummaryHeight("Левый глаз", test, test.LeftEye, columnWidth);
            float summaryHeight = Math.Max(rightHeight, leftHeight);
            if (summaryHeight <= 0f)
                summaryHeight = _valueFont.GetHeight(_graphics) + _summarySpacingSmall;
            total += summaryHeight + _summarySpacingMedium;

            float graphHeight = 0f;
            if (rightGraph != null)
                graphHeight = Math.Max(graphHeight, CalculateGraphHeight(rightGraph, columnWidth));
            if (leftGraph != null)
                graphHeight = Math.Max(graphHeight, CalculateGraphHeight(leftGraph, columnWidth));
            if (graphHeight <= 0f)
                graphHeight = columnWidth * 0.55f;

            total += graphHeight + _spacingSmall;

            return total;
        }

        private void DrawTitle()
        {
            var headerLineSpacing = PointsToPixels(HeaderLineSpacingPoints);
            foreach (var (line, index) in _clinicHeaderLines.Select((value, idx) => (value, idx)))
            {
                var text = string.IsNullOrWhiteSpace(line) ? "\u00A0" : line;
                var spacingAfter = index == _clinicHeaderLines.Length - 1 ? 0f : headerLineSpacing;
                DrawParagraph(text, _clinicFont, Brushes.Black, 0, spacingAfter, _formatCenter);
            }

            DrawParagraph(
                _reportTitle,
                _reportTitleFont,
                Brushes.Black,
                PointsToPixels(HeaderTitleSpacingPoints),
                _spacingLarge,
                _formatCenter);
        }

        private void EnsureSpace(float requiredHeight)
        {
            if (_graphics == null)
                return;

            var limit = PageHeight - _marginBottom;
            if (_y + requiredHeight <= limit)
                return;

            StartNewPage(includeHeader: false);
        }

        private void FinalizeCurrentPage()
        {
            if (_graphics == null || _bitmap == null)
                return;

            _graphics.Dispose();
            using var ms = new MemoryStream();
            _bitmap.Save(ms, DrawingImageFormat.Png);
            _pages.Add(ms.ToArray());
            _bitmap.Dispose();
            _bitmap = null;
            _graphics = null;
        }

        private float GetEyeLabelColumnWidth(float totalWidth)
        {
            var ratio = _template == ReportTemplate.Client ? 0.4f : 0.35f;
            var maxWidth = _template == ReportTemplate.Client ? 140f : 120f;
            var width = Math.Min(maxWidth, totalWidth * ratio);
            if (width < 1f)
                return 0f;
            return width;
        }

        private float MeasureEyeSummaryHeight(string label, ErgTest test, EyeData eye, float width)
        {
            if (_graphics == null)
                return 0f;

            float total = 0f;
            var labelText = BuildEyeLabel(label, eye);
            total += MeasureText(labelText, _eyeLabelFont, width, _formatCenter);
            total += _summarySpacingSmall;

            if (eye.IsFlat)
            {
                total += _valueFont.GetHeight(_graphics) + _summarySpacingSmall;
                return total;
            }

            if (!EyeHasUsableMeasurements(eye))
            {
                total += _summarySpacingSmall;
                return total;
            }

            bool hasContent = false;
            float labelColumnWidth = GetEyeLabelColumnWidth(width);
            float valuesWidth = Math.Max(0f, width - labelColumnWidth);
            if (valuesWidth <= 0f)
            {
                valuesWidth = width;
                labelColumnWidth = 0f;
            }

            foreach (var (_, kind) in GetClientWaveOrder(test))
            {
                var display = BuildWaveDisplay(test, eye, kind);
                if (IsWaveDisplayEmpty(display))
                    continue;

                float measurementHeight = _valueFont.GetHeight(_graphics);
                float normHeight = 0f;

                if (!display.IsFlat)
                {
                    measurementHeight = Math.Max(_valueFont.GetHeight(_graphics), _unitFont.GetHeight(_graphics));
                    var msText = FormatNormForClient(display.MsNorm);
                    var mkvText = FormatNormForClient(display.MkVNorm);
                    normHeight = CalculateNormHeight(msText, mkvText, valuesWidth / 2f);
                }

                total += _summarySpacingSmall * 0.5f;
                total += measurementHeight;
                if (normHeight > 0f)
                    total += normHeight;
                total += _summarySpacingSmall * 0.5f;
                hasContent = true;
            }

            if (!hasContent)
            {
                total += _summarySpacingSmall;
            }

            return total;
        }

        private float MeasureText(string text, DrawingFont font, float width, StringFormat? format = null)
        {
            if (_graphics == null || string.IsNullOrWhiteSpace(text))
                return 0f;

            format ??= _formatLeft;
            var size = _graphics.MeasureString(text, font, new SizeF(width, float.MaxValue), format);
            return size.Height;
        }

        private static string NormalizeDescription(string description)
        {
            return description
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .TrimEnd();
        }

        private static float PointsToPixels(double points) => (float)(points / 72d * Dpi);

        private static double MillimetersToPoints(double millimeters) => millimeters / 25.4d * 72d;

        private static float LayoutMillimetersToPixels(double millimeters) => (float)(millimeters / 25.4d * Dpi);

        private void SavePdf()
        {
            using var document = new PdfDocument();
            var totalPages = _pages.Count;

            var versionText = _reportVersion != null ? $"Версия отчета: {_reportVersion}" : null;
            var versionFont = new XFont("Arial", 8, XFontStyle.Regular);
            var pageFont = new XFont("Arial", 9, XFontStyle.Regular);
            var footerRectFormatLeft = new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center };
            var footerRectFormatRight = new XStringFormat { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Center };
            var footerHeight = MillimetersToPoints(20d);

            for (int i = 0; i < totalPages; i++)
            {
                var pageImage = _pages[i];
                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;

                using var gfx = XGraphics.FromPdfPage(page);
                using var image = XImage.FromStream(() => new MemoryStream(pageImage));
                gfx.DrawImage(image, 0, 0, page.Width, page.Height);

                var footerRect = new XRect(footerHeight, page.Height - footerHeight, page.Width - footerHeight * 2d, footerHeight);

                if (versionText != null)
                {
                    gfx.DrawString(versionText, versionFont, XBrushes.Gray, footerRect, footerRectFormatLeft);
                }

                var pageLabel = $"Стр. {i + 1} из {totalPages}";
                gfx.DrawString(pageLabel, pageFont, XBrushes.Gray, footerRect, footerRectFormatRight);
            }

            document.Save(_pdfPath);
        }

        private void StartNewPage(bool includeHeader)
        {
            FinalizeCurrentPage();

            _bitmap = new Bitmap(PageWidth, PageHeight);
            _bitmap.SetResolution(Dpi, Dpi);
            _graphics = Graphics.FromImage(_bitmap);
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            _graphics.Clear(DrawingColor.White);
            _y = _marginTop;

            if (includeHeader)
                DrawTitle();

            DrawInfoBlock(allowPageBreak: false);
        }

        private float ContentWidth => PageWidth - _marginLeft - _marginRight;

        public void Build()
        {
            StartNewPage(includeHeader: true);
            for (int i = 0; i < _patient.Tests.Count; i++)
            {
                DrawTestSection(i, _patient.Tests[i]);
            }

            DrawDescription();

            FinalizeCurrentPage();
            SavePdf();
        }

        public void Dispose()
        {
            FinalizeCurrentPage();
            _graphics?.Dispose();
            _bitmap?.Dispose();

            _clinicFont.Dispose();
            _reportTitleFont.Dispose();
            _infoLabelFont.Dispose();
            _infoValueFont.Dispose();
            _testTitleFont.Dispose();
            _descriptionTitleFont.Dispose();
            _descriptionFont.Dispose();
            _infoSmallFont.Dispose();
            _eyeLabelFont.Dispose();
            _waveLabelFont.Dispose();
            _valueFont.Dispose();
            _unitFont.Dispose();
            _normFont.Dispose();
            _placeholderFont.Dispose();
            _formatLeft.Dispose();
            _formatLeftCenter.Dispose();
            _formatRight.Dispose();
            _formatCenter.Dispose();
            _mutedBrush.Dispose();
            _descriptionBackgroundBrush.Dispose();
            _testHeaderBackgroundBrush.Dispose();
        }
    }
    private sealed class TestComponent : IComponent
    {
        private readonly int _index;
        private readonly ErgTest _test;

        public TestComponent(int index, ErgTest test)
        {
            _index = index;
            _test = test;
        }

        public void Compose(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).Column(column =>
            {
                column.Spacing(8);
                column.Item().Text($"Тест №{_index}: {_test.TestName}").FontSize(13).SemiBold();

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Точек: {_test.GraphNumPoints}, Δt: {_test.GraphDt} мс, дискрет/мкВ: {_test.GraphDiscrPerMkV}");
                    row.RelativeItem().AlignRight().Text($"Вспышка: {_test.GraphFlashPosition} мс");
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Диапазон X: {_test.GraphXScaleMin}…{_test.GraphXScaleMax} мс (шаг {_test.GraphXValueStep})");
                    row.RelativeItem().AlignRight().Text($"Диапазон Y: {_test.GraphYScaleMin}…{_test.GraphYScaleMax} мкВ (шаг {_test.GraphYValueStep})");
                });

                column.Item().Text($"a-волна: {(_test.AWaveExists ? "есть" : "нет")}, нормы ms: {FormatRange(_test.AWaveMsNormalMin, _test.AWaveMsNormalMax)}, мкВ: {FormatRange(_test.AWaveMkVNormalMin, _test.AWaveMkVNormalMax)}");
                if (_test.AWaveExists)
                {
                    column.Item().Text($"b-волна нормы ms: {FormatRange(_test.BWaveMsNormalMin, _test.BWaveMsNormalMax)}, мкВ: {FormatRange(_test.BWaveMkVNormalMin, _test.BWaveMkVNormalMax)}");
                }

                column.Item().Component(new EyeTableComponent(_test));

                column.Item().Component(new GraphComponent(_test));
            });
        }

    }

    private sealed class ClientInfoComponent : IComponent
    {
        private readonly CommonInfo? _deviceInfo;
        private readonly ErgPatient _patient;

        public ClientInfoComponent(ErgPatient patient, CommonInfo? deviceInfo)
        {
            _patient = patient;
            _deviceInfo = deviceInfo;
        }

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(2);
                column.Item().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(12));
                    text.Span("ID пациента: ").SemiBold();
                    text.Span($"{_patient.PatientId} ({FormatAnimal(_patient.Animal)})");
                });

                column.Item().Text(text =>
                {
                    text.Span("Дата и время исследования: ").SemiBold();
                    text.Span(FormatClientDateTime(_patient.TestDateTime));
                });

                column.Item().Text(text =>
                {
                    text.Span("Оборудование: ").SemiBold();
                    text.Span(GetClientDeviceName(_deviceInfo));
                });

                var software = GetClientSoftwareVersion(_deviceInfo);
                if (!string.IsNullOrEmpty(software))
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Версия ПО: ").SemiBold();
                        text.Span(software);
                    });
                }

            });
        }
    }

    private sealed class ClientTestComponent : IComponent
    {
        private readonly int _index;
        private readonly ErgTest _test;

        public ClientTestComponent(int index, ErgTest test)
        {
            _index = index;
            _test = test;
        }

        public void Compose(IContainer container)
        {
            container = container.ShowEntire();

            container.Column(column =>
            {
                column.Spacing(12);
                column.Item().Element(header =>
                {
                    header.Background(Colors.Grey.Lighten2)
                        .PaddingVertical(6)
                        .PaddingHorizontal(8)
                        .AlignCenter()
                        .Text(FormatClientTestTitle(_index, _test)).FontSize(12).SemiBold();
                });

                column.Item().Row(row =>
                {
                    row.Spacing(18);
                    row.RelativeItem().Component(new ClientEyeSummaryComponent("Правый глаз", _test, _test.RightEye));
                    row.RelativeItem().Component(new ClientEyeSummaryComponent("Левый глаз", _test, _test.LeftEye));
                });
                column.Item().Row(row =>
                {
                    row.Spacing(18);
                    row.RelativeItem().Component(new ClientGraphComponent(_test, _test.RightEye));
                    row.RelativeItem().Component(new ClientGraphComponent(_test, _test.LeftEye));
                });
            });
        }
    }

    private sealed class ClientEyeSummaryComponent : IComponent
    {
        private readonly EyeData _eye;
        private readonly string _label;
        private readonly ErgTest _test;

        public ClientEyeSummaryComponent(string label, ErgTest test, EyeData eye)
        {
            _label = label;
            _test = test;
            _eye = eye;
        }

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(6);
                column.Item().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(11));
                    text.Span(_label).SemiBold();
                    var quality = FormatQualityCompact(_eye.QualityIndex);
                    if (!string.IsNullOrWhiteSpace(quality))
                    {
                        text.Span(" ");
                        text.Span(quality).FontColor(Colors.Grey.Darken1);
                    }
                });

                if (_eye.IsFlat)
                {
                    column.Item().AlignCenter().Text("FLAT").FontSize(26).SemiBold();
                    return;
                }

                if (!EyeHasUsableMeasurements(_eye))
                {
                    column.Item().AlignCenter().Text("Нет данных").Italic().FontColor(Colors.Grey.Darken1);
                    return;
                }

                bool hasContent = false;

                foreach (var (label, kind) in GetClientWaveOrder(_test))
                {
                    var display = BuildWaveDisplay(_test, _eye, kind);
                    if (IsWaveDisplayEmpty(display))
                        continue;

                    column.Item().Component(new ClientWaveValuesComponent(label, display));
                    hasContent = true;
                }

                if (!hasContent)
                {
                    column.Item().AlignCenter().Text("Нет данных").Italic().FontColor(Colors.Grey.Darken1);
                }
            });
        }

    }

    private sealed class ClientWaveValuesComponent : IComponent
    {
        private readonly WaveDisplay _display;
        private readonly string _label;

        public ClientWaveValuesComponent(string label, WaveDisplay display)
        {
            _label = label;
            _display = display;
        }

        private static void AppendMeasurement(ColumnDescriptor column, string value)
        {
            column.Item().AlignCenter().Row(row =>
            {
                row.Spacing(2);

                if (IsMissingMeasurementString(value))
                {
                    row.AutoItem().AlignBottom().Text(MissingMeasurementText).FontSize(26).SemiBold();
                    return;
                }

                var parts = SplitValueAndUnit(value);
                row.AutoItem().AlignBottom().Text(parts.Value).FontSize(26).SemiBold();
                if (!string.IsNullOrEmpty(parts.Unit))
                {
                    row.AutoItem().AlignBottom().Text(parts.Unit).FontSize(12);
                }
            });
        }

        private static void AppendNorm(ColumnDescriptor column, string value)
        {
            var formatted = FormatNormForClient(value);
            if (formatted == null)
                return;

            column.Item().AlignCenter().Text(formatted).FontSize(9).FontColor(Colors.Grey.Darken1);
        }

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(4);

                column.Item().Row(row =>
                {
                    row.Spacing(12);

                    row.AutoItem().MinWidth(68).AlignMiddle().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(11));
                        text.Span(string.IsNullOrWhiteSpace(_label) ? " " : _label);
                    });

                    row.RelativeItem().Column(valuesColumn =>
                    {
                        valuesColumn.Spacing(4);

                        if (_display.IsFlat)
                        {
                            valuesColumn.Item().AlignCenter().Text("FLAT").FontSize(26).SemiBold();
                            return;
                        }

                        valuesColumn.Item().Row(valueRow =>
                        {
                            valueRow.Spacing(8);

                            valueRow.RelativeItem().Column(msColumn =>
                            {
                                msColumn.Spacing(2);
                                AppendMeasurement(msColumn, _display.MsValue);
                                AppendNorm(msColumn, _display.MsNorm);
                            });

                            valueRow.RelativeItem().Column(mkvColumn =>
                            {
                                mkvColumn.Spacing(2);
                                AppendMeasurement(mkvColumn, _display.MkVValue);
                                AppendNorm(mkvColumn, _display.MkVNorm);
                            });
                        });
                    });
                });
            });
        }
    }

    private sealed class ClientGraphComponent : IComponent
    {
        private readonly EyeData _eye;
        private readonly ErgTest _test;

        public ClientGraphComponent(ErgTest test, EyeData eye)
        {
            _test = test;
            _eye = eye;
        }

        public void Compose(IContainer container)
        {
            var graph = TryRenderGraphImage(_test, _eye);

            container.Column(column =>
            {
                column.Spacing(4);
                if (graph != null)
                {
                    column.Item().AlignCenter().Element(e => e.Image(graph.Data).FitWidth());
                }
                else
                {
                    column.Item()
                        .MinHeight(120)
                        .AlignCenter().AlignMiddle()
                        .Text("Нет данных").Italic().FontColor(Colors.Grey.Darken1);
                }
            });
        }
    }

    private sealed class ClientDescriptionComponent : IComponent
    {
        private readonly string _description;

        public ClientDescriptionComponent(string description) => _description = description;

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(4);
                column.Item().Text("Заключение:").FontSize(12).SemiBold();
                column.Item().Text(_description).FontSize(11);
            });
        }
    }

    private sealed class EyeTableComponent : IComponent
    {
        private readonly ErgTest _test;

        public EyeTableComponent(ErgTest test) => _test = test;

        private static void AddRow(TableDescriptor table, string caption, string right, string left)
        {
            table.Cell().Element(CellBody).Text(caption);
            table.Cell().Element(CellBody).Text(right).LineHeight(1.2f);
            table.Cell().Element(CellBody).Text(left).LineHeight(1.2f);
        }

        private static IContainer CellBody(IContainer container) => container.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);

        private static IContainer CellHeader(IContainer container) => container.Padding(4).Background(Colors.Grey.Lighten4).DefaultTextStyle(t => t.SemiBold());

        public void Compose(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(110);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellHeader).Text("Параметр");
                    header.Cell().Element(CellHeader).AlignCenter().Text("Правый глаз");
                    header.Cell().Element(CellHeader).AlignCenter().Text("Левый глаз");
                });

                foreach (var row in GetEyeTableRows(_test))
                {
                    AddRow(table, row.Caption, row.Right, row.Left);
                }
            });
        }
    }

    private sealed class GraphComponent : IComponent
    {
        private readonly ErgTest _test;

        public GraphComponent(ErgTest test) => _test = test;

        public void Compose(IContainer container)
        {
            var rightGraph = TryRenderGraphImage(_test, _test.RightEye);
            var leftGraph = TryRenderGraphImage(_test, _test.LeftEye);
            var styleDescriptions = DescribeGraphStyles(_test);

            container.Column(column =>
            {
                column.Spacing(6);
                column.Item().Text("Графические данные").SemiBold();
                column.Item().Text($"Правый глаз: {_test.RightEye.GraphCount} граф., левый глаз: {_test.LeftEye.GraphCount} граф.");

                if (styleDescriptions.Length > 0)
                {
                    column.Item().Text("Стили графиков: " + string.Join("; ", styleDescriptions)).FontSize(10);
                }

                if (rightGraph == null && leftGraph == null)
                {
                    column.Item().AlignCenter().Text("Нет данных").Italic().FontColor(Colors.Grey.Darken1);
                }
                else
                {
                    column.Item().Row(row =>
                    {
                        row.Spacing(12);

                        row.RelativeItem().Column(col =>
                        {
                            col.Spacing(4);
                            col.Item().Text("Правый глаз").SemiBold();
                            if (rightGraph != null)
                            {
                                col.Item().Image(rightGraph.Data).FitWidth();
                            }
                            else
                            {
                                col.Item().AlignCenter().AlignMiddle().Text("Нет данных").Italic().FontColor(Colors.Grey.Darken1);
                            }
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Spacing(4);
                            col.Item().Text("Левый глаз").SemiBold();
                            if (leftGraph != null)
                            {
                                col.Item().Image(leftGraph.Data).FitWidth();
                            }
                            else
                            {
                                col.Item().AlignCenter().AlignMiddle().Text("Нет данных").Italic().FontColor(Colors.Grey.Darken1);
                            }
                        });
                    });
                }

                var rightPreview = BuildGraphPreview(_test.RightEye.GraphsNormalized, _test.GraphNumPoints);
                if (!string.Equals(rightPreview, "нет данных", StringComparison.OrdinalIgnoreCase))
                {
                    column.Item().Text("Первые 10 точек (правый глаз, график 1): " + rightPreview).FontSize(10);
                }

                var leftPreview = BuildGraphPreview(_test.LeftEye.GraphsNormalized, _test.GraphNumPoints);
                if (!string.Equals(leftPreview, "нет данных", StringComparison.OrdinalIgnoreCase))
                {
                    column.Item().Text("Первые 10 точек (левый глаз, график 1): " + leftPreview).FontSize(10);
                }
            });
        }
    }

    private sealed record TableRowData(string Caption, string Right, string Left);

    private sealed record GraphImage(byte[] Data, int Width, int Height)
    {
    }

    private enum WaveKind
    {
        A,
        B
    }

    private sealed record WaveMeasurement(double? Ms, double? MkV);

    private sealed record WaveDisplay(bool IsFlat, string MsValue, string MkVValue, string MsNorm, string MkVNorm);

    private sealed record GraphRenderContext(
        double[][] Graphs,
        int Curves,
        int DeclaredPointCount,
        double XMin,
        double XMax,
        double YMin,
        double YMax,
        double SampleMin,
        double SampleMax,
        double FlashOffset,
        GraphMarker[] Markers,
        GraphWaveLevel[] WaveLevels,
        double ScaleXMin,
        double ScaleXMax);

    private enum GraphMarkerKind
    {
        AWave,
        BWave
    }

    private sealed record GraphMarker(GraphMarkerKind Kind, double PositionMs);

    private sealed record GraphWaveLevel(GraphMarkerKind Kind, double Value);

    private sealed record AxisTick(double Position, double DisplayValue, bool IsMajor, bool IsAnchor);

    private sealed record AxisTickSet(AxisTick[] Ticks, AxisTick[] MajorTicks, double[] GridLines);

    private static float[] BuildDottedPattern(GraphRenderOptions opt)
    {
        var dash = GraphRenderOptions.NormalizeDottedLength(opt.DottedDashLengthPx, GraphRenderOptions.DefaultDottedDashLengthPx);
        var gap = GraphRenderOptions.NormalizeDottedLength(opt.DottedGapLengthPx, GraphRenderOptions.DefaultDottedGapLengthPx);
        return new[] { dash, gap };
    }

    private static SKPathEffect? CreateDottedPathEffect(GraphRenderOptions opt)
    {
        var pattern = BuildDottedPattern(opt);
        return SKPathEffect.CreateDash(pattern, 0);
    }

    public sealed class GraphRenderOptions
    {
        internal const float MinDottedSegmentLengthPx = 1f;
        internal const float DefaultDottedDashLengthPx = 4f;
        internal const float DefaultDottedGapLengthPx = 3f;

        // Внешние зазоры под оси/подписи
        public float AxisGapHorizontal { get; set; } = 20f; // слева под цифры Y и "µV"
        public float AxisGapVertical { get; set; } = 8f;  // снизу под цифры X и "ms"

        // Толщины линий (px)
        public float AxisThicknessPx { get; set; } = 4f;
        public float CurveThicknessPx { get; set; } = 8f;
        public float ExtremumThicknessPx { get; set; } = 1.2f;
        public float HorizontalMarkerThicknessPx { get; set; } = 1.2f;

        // Параметры пунктира (px)
        public float DottedDashLengthPx { get; set; } = DefaultDottedDashLengthPx;
        public float DottedGapLengthPx { get; set; } = DefaultDottedGapLengthPx;

        public static float NormalizeDottedLength(float value, float fallback)
        {
            if (!float.IsFinite(value) || value <= 0f)
                return fallback;
            return Math.Max(MinDottedSegmentLengthPx, value);
        }

        // Эти два не были в JSON — оставляю прежние значения
        public float GridThicknessPx { get; set; } = 1.0f;

        // Шрифты (pt)
        public float LabelFontPt { get; set; } = 7f;   // цифры на осях

        // Риски (мм)
        public float MajorTickLenMm { get; set; } = 3f;
        public float MarginBottom { get; set; } = 120f;
        // Поля страницы
        public float MarginLeft { get; set; } = 165f;
        public float MarginRight { get; set; } = 20f;
        public float MarginTop { get; set; } = 5f;
        public float MinLabelGapXPx { get; set; } = 5f;  // анти-коллизия X
        public float MinLabelGapYPx { get; set; } = 4f;  // анти-коллизия Y
        public float MinorTickLenMm { get; set; } = 1.5f;
        public float TickThicknessPx { get; set; } = 3f;
        public float UnitsFontPt { get; set; } = 6.5f; // "ms", "µV"

        // Ручки позиционирования подписей
        public float XDigitsOffsetPx { get; set; } = 5f;  // ниже рисок X
        public float XUnitsGapPx { get; set; } = 5f;  // от цифр X до "ms"
        public float YDigitsLeftPadPx { get; set; } = 16f; // от оси до цифр Y
        public float YUnitsFallbackFromAxisPx { get; set; } = 70f; // если цифр нет — от оси
        public float YUnitsGapFromNumbersPx { get; set; } = 22f; // "µV" левее самой левой цифры
    }
}
