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

    static ErgReportBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
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

                    if (!string.IsNullOrWhiteSpace(patient.Description))
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(desc =>
                        {
                            desc.Item().Text("Автоматическое заключение").SemiBold();
                            desc.Item().Text(patient.Description).FontSize(10).WrapAnywhere();
                        });
                    }

                    for (int i = 0; i < patient.Tests.Count; i++)
                    {
                        var test = patient.Tests[i];
                        column.Item().Component(new TestComponent(i + 1, test));
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
                page.Margin(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(11));

                var clinicHeaderLines = BuildClinicHeaderLines(clinicName);
                if (clinicHeaderLines.All(string.IsNullOrWhiteSpace))
                    clinicHeaderLines[0] = "Шапка [название организации]";

                page.Header().Column(column =>
                {
                    column.Spacing(1);

                    foreach (var line in clinicHeaderLines)
                    {
                        column.Item().MinHeight(12).AlignCenter().AlignMiddle().Text(text =>
                        {
                            text.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(10));
                            text.Span(string.IsNullOrWhiteSpace(line) ? " " : line);
                        });
                    }

                    // Отступ между шапкой и заголовком отчета оставлен минимальным, чтобы блоки располагались плотнее.
                    column.Item().PaddingTop(2).AlignCenter().Text(reportTitle).FontSize(14).SemiBold();
                });

                page.Content().Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Component(new ClientInfoComponent(patient, deviceInfo));

                    for (int i = 0; i < patient.Tests.Count; i++)
                    {
                        column.Item().Component(new ClientTestComponent(i + 1, patient.Tests[i]));
                    }

                    if (!string.IsNullOrWhiteSpace(patient.Description))
                    {
                        column.Item().Component(new ClientDescriptionComponent(patient.Description));
                    }

                });

                page.Footer().Column(footer =>
                {
                    footer.Spacing(2);
                    if (!string.IsNullOrWhiteSpace(reportVersion) && reportVersion != "—")
                    {
                        footer.Item().AlignCenter().Text(t =>
                        {
                            t.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                            t.Span($"Версия отчета: {reportVersion}");
                        });
                    }

                    footer.Item().AlignCenter().Text(txt =>
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

    private static void BuildPatientReportLegacyPdf(ErgPatient patient, string pdfPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath, ReportTemplate template)
    {
        _ = template;
        using var renderer = new LegacyPdfRenderer(patient, pdfPath, deviceInfo, clinicName, rawFilePath);
        renderer.Build();
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

    private static void BuildPatientWordReportClassic(ErgPatient patient, string docxPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath)
    {
        var headerTitle = string.IsNullOrWhiteSpace(clinicName)
            ? "Отчет по результатам ЭРГ-исследования сетчатки"
            : clinicName;

        using (var document = WordprocessingDocument.Create(docxPath, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new WordDocument(new Body());
            EnsureWordDocumentInfrastructure(mainPart);
            var body = mainPart.Document.Body ?? throw new InvalidOperationException("Не удалось создать тело документа Word.");

            body.Append(CreateParagraph(headerTitle, fontSizePt: 16, bold: true, justification: JustificationValues.Center, spacingAfter: TwipsFromPoints(12)));
            body.Append(CreateHeaderTable(patient, deviceInfo));

            if (!string.IsNullOrWhiteSpace(patient.Description))
            {
                body.Append(CreateDescriptionTable(patient.Description));
            }

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

            ApplyPageMargins(body, leftCm: 1.0, rightCm: 1.0, topCm: 1.0, bottomCm: 1.0);

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
            EnsureWordDocumentInfrastructure(mainPart);
            var body = mainPart.Document.Body ?? throw new InvalidOperationException("Не удалось создать тело документа Word.");

            var clinicHeaderLines = BuildClinicHeaderLines(clinicName);
            if (clinicHeaderLines.All(string.IsNullOrWhiteSpace))
                clinicHeaderLines[0] = "Шапка [название организации]";

        foreach (var line in clinicHeaderLines)
        {
            var text = string.IsNullOrWhiteSpace(line) ? string.Empty : line;
            body.Append(CreateParagraph(text, fontSizePt: 10, bold: true, justification: JustificationValues.Center, spacingAfter: TwipsFromPoints(1), tightenLineSpacing: true));
        }

        var reportTitle = !string.IsNullOrWhiteSpace(deviceInfo?.ReportName)
            ? deviceInfo!.ReportName!
            : "Отчет по результатам ЭРГ-исследования сетчатки";
        // Отдельно фиксируем минимальный отступ, чтобы заголовок отчета визуально примыкал к шапке.
        body.Append(CreateParagraph(reportTitle, fontSizePt: 14, bold: true, justification: JustificationValues.Center, spacingAfter: TwipsFromPoints(4)));
            body.Append(CreateClientInfoTable(patient, deviceInfo));

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

            ApplyPageMargins(body, leftCm: 1.0, rightCm: 1.0, topCm: 1.0, bottomCm: 1.0);

            EnsureDocumentPropertiesParts(document, reportTitle);

            mainPart.Document.Save();
        }

        NormalizeWordContentTypes(docxPath);
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

    private static IEnumerable<string> GetImageExtensions(IReadOnlyCollection<ZipArchiveEntry> entries)
    {
        return entries
            .Where(entry => entry.FullName.StartsWith("word/media/", StringComparison.OrdinalIgnoreCase))
            .Select(entry => Path.GetExtension(entry.FullName))
            .Where(ext => !string.IsNullOrWhiteSpace(ext))
            .Select(ext => ext!.TrimStart('.').ToLowerInvariant())
            .Distinct();
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

    private static bool PackagePartExists(IReadOnlyCollection<ZipArchiveEntry> entries, string partName)
    {
        var normalized = NormalizePartName(partName).TrimStart('/');
        return entries.Any(entry => string.Equals(entry.FullName, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePartName(string? partName)
    {
        if (string.IsNullOrWhiteSpace(partName))
            return string.Empty;

        var trimmed = partName.Trim().Replace('\\', '/');
        return trimmed.StartsWith("/") ? trimmed : "/" + trimmed.TrimStart('/');
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

    private static string NormalizeRelationshipTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return string.Empty;

        var trimmed = target.Replace('\\', '/').Trim();
        if (trimmed.StartsWith("../"))
            trimmed = trimmed.Substring(3);
        return trimmed.TrimStart('/');
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

    private sealed class LegacyPdfRenderer : IDisposable
    {
        private const int Dpi = 200;
        private const int PageWidth = (int)(8.27f * Dpi);
        private const int PageHeight = (int)(11.69f * Dpi);

        private readonly ErgPatient _patient;
        private readonly string _pdfPath;
        private readonly CommonInfo? _deviceInfo;

        private readonly string[] _clinicHeaderLines;
        private readonly string _reportTitle;
        private readonly string? _reportVersion;

        private readonly List<byte[]> _pages = new();
        private Bitmap? _bitmap;
        private Graphics? _graphics;
        private float _y;

        private readonly float _marginLeft = 0.8f * Dpi;
        private readonly float _marginRight = 0.8f * Dpi;
        private readonly float _marginTop = 0.8f * Dpi;
        private readonly float _marginBottom = 0.9f * Dpi;
        private readonly float _spacingSmall = 0.07f * Dpi;
        private readonly float _spacingMedium = 0.11f * Dpi;
        private readonly float _spacingLarge = 0.18f * Dpi;
        private readonly float _graphGap = 0.12f * Dpi;
        private readonly float _summarySpacingSmall = 0.05f * Dpi;
        private readonly float _summarySpacingMedium = 0.08f * Dpi;

        private float ContentWidth => PageWidth - _marginLeft - _marginRight;

        private readonly DrawingFont _clinicFont = new("Arial", 10f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly DrawingFont _reportTitleFont = new("Arial", 14f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly DrawingFont _infoLabelFont = new("Arial", 11f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly DrawingFont _infoValueFont = new("Arial", 11f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly DrawingFont _testTitleFont = new("Arial", 12f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly DrawingFont _descriptionTitleFont = new("Arial", 12f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly DrawingFont _descriptionFont = new("Arial", 10f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly DrawingFont _infoSmallFont = new("Arial", 9f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly DrawingFont _eyeLabelFont = new("Arial", 11f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly DrawingFont _valueFont = new("Arial", 26f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly DrawingFont _unitFont = new("Arial", 12f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly DrawingFont _normFont = new("Arial", 10f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly DrawingFont _placeholderFont = new("Arial", 10f, FontStyle.Italic, GraphicsUnit.Point);

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

        private readonly StringFormat _formatCenter = new(StringFormatFlags.LineLimit)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.Word
        };

        private readonly SolidBrush _mutedBrush = new(DrawingColor.FromArgb(100, 100, 100));
        private readonly SolidBrush _descriptionBackgroundBrush = new(DrawingColor.FromArgb(245, 245, 245));
        private readonly SolidBrush _testHeaderBackgroundBrush = new(DrawingColor.FromArgb(0xEE, 0xEE, 0xEE));

        public LegacyPdfRenderer(ErgPatient patient, string pdfPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath)
        {
            _patient = patient;
            _pdfPath = pdfPath;
            _deviceInfo = deviceInfo;

            _clinicHeaderLines = BuildClinicHeaderLines(clinicName);
            if (_clinicHeaderLines.All(string.IsNullOrWhiteSpace))
                _clinicHeaderLines[0] = "Шапка [название организации]";
            _reportTitle = !string.IsNullOrWhiteSpace(deviceInfo?.ReportName)
                ? deviceInfo!.ReportName!
                : "Отчет по результатам ЭРГ-исследования сетчатки";

            var version = GetApplicationVersion();
            _reportVersion = string.IsNullOrWhiteSpace(version) || version == "—" ? null : version;
        }

        public void Build()
        {
            StartNewPage();
            DrawTitle();
            DrawInfoBlock();
            DrawDescription();
            for (int i = 0; i < _patient.Tests.Count; i++)
            {
                DrawTestSection(i, _patient.Tests[i]);
            }

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

        private void StartNewPage()
        {
            FinalizeCurrentPage();

            _bitmap = new Bitmap(PageWidth, PageHeight);
            _bitmap.SetResolution(Dpi, Dpi);
            _graphics = Graphics.FromImage(_bitmap);
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            _graphics.Clear(DrawingColor.White);
            _y = _marginTop;
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

        private void EnsureSpace(float requiredHeight)
        {
            if (_graphics == null)
                return;

            var limit = PageHeight - _marginBottom;
            if (_y + requiredHeight <= limit)
                return;

            StartNewPage();
        }

        private float MeasureText(string text, DrawingFont font, float width, StringFormat? format = null)
        {
            if (_graphics == null || string.IsNullOrWhiteSpace(text))
                return 0f;

            format ??= _formatLeft;
            var size = _graphics.MeasureString(text, font, new SizeF(width, float.MaxValue), format);
            return size.Height;
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

        private void DrawTitle()
        {
            foreach (var line in _clinicHeaderLines)
            {
                var text = string.IsNullOrWhiteSpace(line) ? "\u00A0" : line;
                DrawParagraph(text, _clinicFont, Brushes.Black, 0, _summarySpacingSmall * 0.25f, _formatCenter);
            }

            DrawParagraph(_reportTitle, _reportTitleFont, Brushes.Black, _summarySpacingSmall * 0.5f, _spacingLarge, _formatCenter);
        }

        private void DrawInfoBlock()
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
                DrawInfoLine(label, string.IsNullOrWhiteSpace(value) ? "—" : value);
            }

            _y += _spacingSmall * 0.5f;
        }

        private void DrawInfoLine(string label, string value)
        {
            if (_graphics == null)
                return;

            var labelWidth = Math.Min(ContentWidth * 0.45f, _graphics.MeasureString(label, _infoLabelFont).Width + 4f);
            var lineHeight = Math.Max(_infoLabelFont.GetHeight(_graphics), _infoValueFont.GetHeight(_graphics));

            EnsureSpace(lineHeight + _summarySpacingSmall * 0.5f);

            var labelRect = new RectangleF(_marginLeft, _y, labelWidth, lineHeight);
            _graphics.DrawString(label, _infoLabelFont, Brushes.Black, labelRect, _formatLeft);

            var valueRect = new RectangleF(_marginLeft + labelWidth + 6f, _y, ContentWidth - labelWidth - 6f, lineHeight);
            _graphics.DrawString(value, _infoValueFont, Brushes.Black, valueRect, _formatLeft);

            _y += lineHeight + _summarySpacingSmall * 0.5f;
        }

        private void DrawDescription()
        {
            if (_graphics == null || string.IsNullOrWhiteSpace(_patient.Description))
                return;

            var innerWidth = ContentWidth - _summarySpacingSmall * 2f;
            var titleHeight = MeasureText("Автоматическое заключение", _descriptionTitleFont, innerWidth, _formatLeft);
            var textHeight = MeasureText(_patient.Description, _descriptionFont, innerWidth, _formatLeft);
            var blockHeight = titleHeight + textHeight + _summarySpacingSmall * 3f;

            EnsureSpace(blockHeight + _spacingMedium);

            var outerRect = new RectangleF(_marginLeft, _y, ContentWidth, blockHeight);
            _graphics!.FillRectangle(_descriptionBackgroundBrush, outerRect);

            var titleRect = new RectangleF(
                _marginLeft + _summarySpacingSmall,
                _y + _summarySpacingSmall,
                innerWidth,
                titleHeight);
            _graphics.DrawString("Автоматическое заключение", _descriptionTitleFont, Brushes.Black, titleRect, _formatLeft);

            var textRect = new RectangleF(
                _marginLeft + _summarySpacingSmall,
                titleRect.Bottom + _summarySpacingSmall * 0.5f,
                innerWidth,
                textHeight);
            _graphics.DrawString(_patient.Description, _descriptionFont, Brushes.Black, textRect, _formatLeft);

            _y += blockHeight + _spacingMedium;
        }

        private void DrawTestSection(int index, ErgTest test)
        {
            var title = FormatClientTestTitle(index + 1, test);
            DrawTestHeader(title);
            DrawEyeSummaryRow(test);
            DrawGraphSection(test);
        }

        private void DrawTestHeader(string title)
        {
            if (_graphics == null)
                return;

            float spacingBefore = _spacingLarge * 0.4f;
            float spacingAfter = _summarySpacingSmall;

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
                total += _placeholderFont.GetHeight(_graphics) + _summarySpacingSmall;
                return total;
            }

            bool hasContent = false;
            float labelColumnWidth = Math.Min(100f, width * 0.35f);
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
                total += _placeholderFont.GetHeight(_graphics) + _summarySpacingSmall;
            }

            return total;
        }

        private static string BuildEyeLabel(string label, EyeData eye)
        {
            var quality = FormatQualityCompact(eye.QualityIndex);
            return quality != null ? $"{label} {quality}" : label;
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
                DrawPlaceholder(rect.Left, rect.Width, ref cursor);
                cursor += _summarySpacingSmall;
                return;
            }

            bool hasContent = false;
            float labelColumnWidth = Math.Min(100f, rect.Width * 0.35f);
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
                    _graphics.DrawString(waveLabel, _eyeLabelFont, Brushes.Black, waveRect, _formatLeftCenter);
                }

                var valueArea = new RectangleF(rect.Left + labelColumnWidth, cursor, valuesWidth, measurementHeight);

                if (display.IsFlat)
                {
                    _graphics.DrawString("FLAT", _valueFont, Brushes.Black, valueArea, _formatCenter);
                    cursor += measurementHeight + spacing;
                    hasContent = true;
                    continue;
                }

                DrawMeasurementPair(valueArea, display.MsValue, display.MkVValue);
                cursor += measurementHeight;

                var msText = FormatNormForClient(display.MsNorm);
                var mkvText = FormatNormForClient(display.MkVNorm);
                var normHeight = CalculateNormHeight(msText, mkvText, valuesWidth / 2f);
                if (normHeight > 0f)
                {
                    var normRect = new RectangleF(valueArea.Left, cursor, valuesWidth, normHeight);
                    DrawNormPair(normRect, msText, mkvText);
                    cursor += normHeight;
                }

                cursor += spacing;
                hasContent = true;
            }

            if (!hasContent)
            {
                DrawPlaceholder(rect.Left, rect.Width, ref cursor);
                cursor += _summarySpacingSmall;
            }
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

        private void DrawCenteredValue(float left, float width, ref float cursor, string text)
        {
            if (_graphics == null)
                return;

            var height = _valueFont.GetHeight(_graphics);
            var rect = new RectangleF(left, cursor, width, height);
            _graphics.DrawString(text, _valueFont, Brushes.Black, rect, _formatCenter);
            cursor += height;
        }

        private void DrawMeasurementPair(RectangleF rect, string msValue, string mkvValue)
        {
            var halfWidth = rect.Width / 2f;
            var leftRect = new RectangleF(rect.Left, rect.Top, halfWidth, rect.Height);
            var rightRect = new RectangleF(rect.Left + halfWidth, rect.Top, halfWidth, rect.Height);

            DrawMeasurementValue(leftRect, msValue);
            DrawMeasurementValue(rightRect, mkvValue);
        }

        private void DrawMeasurementValue(RectangleF rect, string text)
        {
            if (_graphics == null)
                return;

            if (text == "FLAT")
            {
                _graphics.DrawString(text, _valueFont, Brushes.Black, rect, _formatCenter);
                return;
            }

            var parts = SplitValueAndUnit(text);
            var valueSize = _graphics.MeasureString(parts.Value, _valueFont);
            var unitSize = string.IsNullOrEmpty(parts.Unit) ? SizeF.Empty : _graphics.MeasureString(parts.Unit, _unitFont);
            var spaceWidth = string.IsNullOrEmpty(parts.Unit) ? 0f : _graphics.MeasureString(" ", _unitFont).Width * 0.5f;

            var totalWidth = valueSize.Width + (string.IsNullOrEmpty(parts.Unit) ? 0f : spaceWidth + unitSize.Width);
            var startX = rect.Left + (rect.Width - totalWidth) / 2f;

            var valueRect = new RectangleF(startX, rect.Top + (rect.Height - _valueFont.GetHeight(_graphics)) / 2f, valueSize.Width, _valueFont.GetHeight(_graphics));
            _graphics.DrawString(parts.Value, _valueFont, Brushes.Black, valueRect, _formatLeft);

            if (!string.IsNullOrEmpty(parts.Unit))
            {
                var unitRect = new RectangleF(startX + valueSize.Width + spaceWidth, rect.Top + rect.Height - _unitFont.GetHeight(_graphics), unitSize.Width, _unitFont.GetHeight(_graphics));
                _graphics.DrawString(parts.Unit, _unitFont, Brushes.Black, unitRect, _formatLeft);
            }
        }

        private void DrawNormPair(RectangleF rect, string? msText, string? mkvText)
        {
            if (_graphics == null)
                return;

            var halfWidth = rect.Width / 2f;
            var leftRect = new RectangleF(rect.Left, rect.Top, halfWidth, rect.Height);
            var rightRect = new RectangleF(rect.Left + halfWidth, rect.Top, halfWidth, rect.Height);

            if (!string.IsNullOrWhiteSpace(msText))
                _graphics.DrawString(msText!, _normFont, _mutedBrush, leftRect, _formatCenter);
            if (!string.IsNullOrWhiteSpace(mkvText))
                _graphics.DrawString(mkvText!, _normFont, _mutedBrush, rightRect, _formatCenter);
        }

        private void DrawGraphSection(ErgTest test)
        {
            if (_graphics == null)
                return;

            var rightGraph = TryRenderGraphImage(test, test.RightEye);
            var leftGraph = TryRenderGraphImage(test, test.LeftEye);

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
            var rightRect = new RectangleF(_marginLeft, top, columnWidth, totalHeight);
            var leftRect = new RectangleF(_marginLeft + columnWidth + _graphGap, top, columnWidth, totalHeight);

            DrawGraphColumn(rightRect, rightGraph, graphHeight);
            DrawGraphColumn(leftRect, leftGraph, graphHeight);

            _y = top + totalHeight + _spacingSmall;
        }

        private static float CalculateGraphHeight(GraphImage graph, float targetWidth)
        {
            if (graph.Width <= 0 || graph.Height <= 0)
                return targetWidth * 0.6f;

            return graph.Height / (float)graph.Width * targetWidth;
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
                using var dashedPen = new Pen(DrawingColor.FromArgb(200, 200, 200)) { DashPattern = new[] { 4f, 4f } };
                _graphics.DrawRectangle(dashedPen, graphRect.X, graphRect.Y, graphRect.Width, graphRect.Height);
                _graphics.DrawString("Нет данных", _placeholderFont, _mutedBrush, graphRect, _formatCenter);
            }
        }

        private void SavePdf()
        {
            using var document = new PdfDocument();
            var totalPages = _pages.Count;

            var versionText = _reportVersion != null ? $"Версия отчета: {_reportVersion}" : null;
            var versionFont = new XFont("Arial", 8, XFontStyle.Regular);
            var pageFont = new XFont("Arial", 9, XFontStyle.Regular);

            for (int i = 0; i < totalPages; i++)
            {
                var pageImage = _pages[i];
                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;

                using var gfx = XGraphics.FromPdfPage(page);
                using var image = XImage.FromStream(() => new MemoryStream(pageImage));
                gfx.DrawImage(image, 0, 0, page.Width, page.Height);

                if (versionText != null)
                {
                    gfx.DrawString(versionText, versionFont, XBrushes.Gray, new XPoint(page.Width / 2, page.Height - 32), XStringFormats.Center);
                }

                var pageLabel = $"Стр. {i + 1} из {totalPages}";
                gfx.DrawString(pageLabel, pageFont, XBrushes.Gray, new XPoint(page.Width / 2, page.Height - 18), XStringFormats.Center);
            }

            document.Save(_pdfPath);
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
        private readonly ErgPatient _patient;
        private readonly CommonInfo? _deviceInfo;

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
            container.Column(column =>
            {
                column.Spacing(10);
                column.Item().Background(Colors.Grey.Lighten3)
                    .PaddingVertical(6)
                    .PaddingHorizontal(8)
                    .AlignCenter()
                    .Text(FormatClientTestTitle(_index, _test)).FontSize(12).SemiBold();

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
        private readonly string _label;
        private readonly ErgTest _test;
        private readonly EyeData _eye;

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
        private readonly string _label;
        private readonly WaveDisplay _display;

        public ClientWaveValuesComponent(string label, WaveDisplay display)
        {
            _label = label;
            _display = display;
        }

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(4);

                column.Item().Row(row =>
                {
                    row.Spacing(12);

                    var hasLabel = !string.IsNullOrWhiteSpace(_label);
                    if (hasLabel)
                    {
                        row.AutoItem().MinWidth(68).AlignMiddle().Text(text =>
                        {
                            text.DefaultTextStyle(style => style.FontSize(11).SemiBold());
                            text.Span(_label);
                        });
                    }

                    var valuesContainer = hasLabel
                        ? row.RelativeItem()
                        : row.RelativeItem().AlignCenter();

                    valuesContainer.Column(valuesColumn =>
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

        private static void AppendNorm(ColumnDescriptor column, string value)
        {
            var formatted = FormatNormForClient(value);
            if (formatted == null)
                return;

            column.Item().AlignCenter().Text(formatted).FontSize(10).FontColor(Colors.Grey.Darken1);
        }

        private static void AppendMeasurement(ColumnDescriptor column, string value)
        {
            column.Item().AlignCenter().Row(row =>
            {
                row.Spacing(2);

                if (value == "—")
                {
                    row.AutoItem().AlignBottom().Text(value).FontSize(26).SemiBold();
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
    }

    private sealed class ClientGraphComponent : IComponent
    {
        private readonly ErgTest _test;
        private readonly EyeData _eye;

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
                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(12)
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
                column.Item().Text("Описание:").FontSize(12).SemiBold();
                column.Item().Text(_description).FontSize(11);
            });
        }
    }

    private sealed class EyeTableComponent : IComponent
    {
        private readonly ErgTest _test;

        public EyeTableComponent(ErgTest test) => _test = test;

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

        private static IContainer CellHeader(IContainer container) => container.Padding(4).Background(Colors.Grey.Lighten4).DefaultTextStyle(t => t.SemiBold());

        private static void AddRow(TableDescriptor table, string caption, string right, string left)
        {
            table.Cell().Element(CellBody).Text(caption);
            table.Cell().Element(CellBody).Text(right).LineHeight(1.2f);
            table.Cell().Element(CellBody).Text(left).LineHeight(1.2f);
        }

        private static IContainer CellBody(IContainer container) => container.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);
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
                    column.Item().Text("Графические данные недоступны.").Italic().FontColor(Colors.Grey.Darken1);
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
                                col.Item().Text("Нет данных").Italic().FontColor(Colors.Grey.Darken1);
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
                                col.Item().Text("Нет данных").Italic().FontColor(Colors.Grey.Darken1);
                            }
                        });
                    });
                }

                column.Item().Text("Первые 10 точек (правый глаз, график 1): " + BuildGraphPreview(_test.RightEye.GraphsNormalized, _test.GraphNumPoints)).FontSize(10);
                column.Item().Text("Первые 10 точек (левый глаз, график 1): " + BuildGraphPreview(_test.LeftEye.GraphsNormalized, _test.GraphNumPoints)).FontSize(10);
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
        GraphMarker[] Markers);

    private readonly struct AxisTick
    {
        public AxisTick(double value, bool isMajor)
        {
            Value = value;
            IsMajor = isMajor;
        }

        public double Value { get; }
        public bool IsMajor { get; }
    }

    private sealed record AxisScale(double MajorStep, int MinorSubdivisions);

    private enum GraphMarkerKind
    {
        AWave,
        BWave
    }

    private sealed record GraphMarker(GraphMarkerKind Kind, double PositionMs);

    private static System.Drawing.Color GetMarkerColor(GraphMarker marker)
        => marker.Kind == GraphMarkerKind.AWave
            ? System.Drawing.Color.FromArgb(0, 102, 204)
            : System.Drawing.Color.FromArgb(0, 150, 0);

    private static string GetMarkerLabel(GraphMarker marker)
        => marker.Kind == GraphMarkerKind.AWave ? "a" : "b";

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

    private static string BoolText(bool value) => value ? "Да" : "Нет";

    private static int DetermineValueCount(EyeData eye)
    {
        if (eye.ValueCount.HasValue)
            return eye.ValueCount.Value;

        int count = 0;
        count = Math.Max(count, eye.AWaveMs?.Length ?? 0);
        count = Math.Max(count, eye.AWaveMkV?.Length ?? 0);
        count = Math.Max(count, eye.BWaveMs?.Length ?? 0);
        count = Math.Max(count, eye.BWaveMkV?.Length ?? 0);
        return count;
    }

    private static string Quality(byte? quality)
    {
        if (!quality.HasValue)
            return "—";

        int value = Math.Clamp((int)quality.Value, 0, 3);
        return new string('★', value) + new string('☆', 3 - value);
    }

    private static string? FormatQualityCompact(byte? quality)
    {
        if (!quality.HasValue)
            return null;

        int value = Math.Clamp((int)quality.Value, 0, 3);
        return new string('★', value) + new string('☆', 3 - value);
    }

    private static string FormatMarker(EyeData eye, byte? marker)
    {
        int valueCount = eye.ValueCount ?? DetermineValueCount(eye);
        if (valueCount <= 0)
            return "—";
        if (!marker.HasValue || marker.Value == 0)
            return "—";
        return $"{marker} мс";
    }

    private static string? FormatMeasurement(EyeData eye, int index)
    {
        int valueCount = eye.ValueCount ?? DetermineValueCount(eye);
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

        static string FormatMs(ushort? value) => value.HasValue ? $"{value} мс" : "—";
        static string FormatMkV(uint? value) => value.HasValue ? $"{value} мкВ" : "—";

        return $"a: {FormatMs(aMs)}, {FormatMkV(aMkV)}\n" +
               $"b: {FormatMs(bMs)}, {FormatMkV(bMkV)}";
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

    private static bool IsWaveDisplayEmpty(WaveDisplay display)
    {
        if (display.IsFlat)
            return false;

        return display.MsValue == "—"
            && display.MkVValue == "—"
            && FormatNormForClient(display.MsNorm) == null
            && FormatNormForClient(display.MkVNorm) == null;
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

    private static bool HasEyeMeasurementValues(EyeData eye)
    {
        return GetFirstValue(eye.AWaveMs).HasValue
            || GetFirstValue(eye.AWaveMkV).HasValue
            || GetFirstValue(eye.BWaveMs).HasValue
            || GetFirstValue(eye.BWaveMkV).HasValue;
    }

    private static string? FormatNormForClient(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "—")
            return null;

        return $"[{value}]";
    }

    private static (string Value, string Unit) SplitValueAndUnit(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ("—", string.Empty);

        var trimmed = text.Trim();
        var lastSpace = trimmed.LastIndexOf(' ');
        if (lastSpace <= 0 || lastSpace >= trimmed.Length - 1)
            return (trimmed, string.Empty);

        var value = trimmed[..lastSpace];
        var unit = trimmed[(lastSpace + 1)..];
        return (value, unit);
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

    private static string[] BuildClinicHeaderLines(string? clinicName)
    {
        var lines = new string[4];
        if (string.IsNullOrWhiteSpace(clinicName))
            return lines;

        var normalized = clinicName.Replace("\r\n", "\n").Replace('\r', '\n');
        var parts = normalized.Split('\n', StringSplitOptions.None);
        for (int i = 0; i < lines.Length && i < parts.Length; i++)
        {
            lines[i] = parts[i].Trim();
        }

        return lines;
    }

    private static string FormatClientTestTitle(int index, ErgTest test)
    {
        var name = FormatClientTestName(test.TestName);
        return $"Тест № {index}: {name}";
    }

    private static string FormatClientTestName(string testName)
    {
        if (string.IsNullOrWhiteSpace(testName))
            return "—";

        return testName.Trim();
    }

    private static string[] DescribeGraphStyles(ErgTest test)
        => test.GraphStyles?
            .Where(s => s.Index < 6)
            .Select(s => $"{s.Index + 1}: RGB({s.Red},{s.Green},{s.Blue}){(s.Dotted ? ", пунктир" : string.Empty)}")
            .ToArray() ?? Array.Empty<string>();

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
        for (int i = 0; i < curves; i++)
        {
            var samples = graphs[i];
            if (samples is { Length: > 1 })
            {
                hasSamples = true;
                break;
            }
        }

        if (!hasSamples)
            return false;

        double xMin = test.GraphXScaleMin;
        double xMax = test.GraphXScaleMax;
        if (xMax <= xMin)
            xMax = xMin + 1;

        double yMin = test.GraphYScaleMin;
        double yMax = test.GraphYScaleMax;
        if (yMax <= yMin)
            yMax = yMin + 1;

        var markers = BuildMarkers(eye, xMin, xMax);

        context = new GraphRenderContext(graphs, curves, test.GraphNumPoints, xMin, xMax, yMin, yMax, markers);
        return true;
    }

    private static GraphMarker[] BuildMarkers(EyeData eye, double xMin, double xMax)
    {
        static bool TryCreateMarker(byte? value, double xMin, double xMax, GraphMarkerKind kind, out GraphMarker marker)
        {
            marker = default!;
            if (!value.HasValue || value.Value == 0)
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

    private static GraphImage? TryRenderGraphImageWithSkia(ErgTest test, GraphRenderContext context)
    {
        try
        {
            const int width = 900;
            const int height = 540;
            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            if (surface == null)
            {
                RenderingSupport.DisableGraphRendering("Не удалось инициализировать движок SkiaSharp для построения графиков.");
                return null;
            }

            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            const float marginLeft = 80f;
            const float marginRight = 30f;
            const float marginTop = 24f;
            const float marginBottom = 80f;
            var chartRect = new SKRect(marginLeft, marginTop, width - marginRight, height - marginBottom);

            double xMin = context.XMin;
            double xMax = context.XMax;
            double yMin = context.YMin;
            double yMax = context.YMax;

            float TransformX(double value) => (float)(chartRect.Left + (value - xMin) / (xMax - xMin) * chartRect.Width);
            float TransformY(double value) => (float)(chartRect.Bottom - (value - yMin) / (yMax - yMin) * chartRect.Height);

            var xScale = DetermineAxisScale(xMin, xMax, test.GraphXValueStep, test.GraphXLineStep);
            var yScale = DetermineAxisScale(yMin, yMax, test.GraphYValueStep, test.GraphYLineStep);
            var xTicks = BuildAxisTicks(xMin, xMax, xScale);
            var yTicks = BuildAxisTicks(yMin, yMax, yScale);

            using var majorGridPaint = new SKPaint { Color = new SKColor(210, 210, 210), StrokeWidth = 1.2f, IsAntialias = true };
            using var minorGridPaint = new SKPaint { Color = new SKColor(235, 235, 235), StrokeWidth = 0.8f, IsAntialias = true };

            foreach (var tick in xTicks)
            {
                var px = TransformX(tick.Value);
                if (px <= chartRect.Left + 1 || px >= chartRect.Right - 1)
                    continue;

                var paint = tick.IsMajor ? majorGridPaint : minorGridPaint;
                canvas.DrawLine(px, chartRect.Top, px, chartRect.Bottom, paint);
            }

            foreach (var tick in yTicks)
            {
                if (IsApproximatelyZero(tick.Value))
                    continue;

                var py = TransformY(tick.Value);
                if (py <= chartRect.Top + 1 || py >= chartRect.Bottom - 1)
                    continue;

                var paint = tick.IsMajor ? majorGridPaint : minorGridPaint;
                canvas.DrawLine(chartRect.Left, py, chartRect.Right, py, paint);
            }

            using (var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.8f, IsAntialias = true })
            {
                canvas.DrawLine(chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom, axisPaint);
                canvas.DrawLine(chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom, axisPaint);
            }

            const float majorTickInside = 6f;
            const float majorTickOutside = 8f;
            const float minorTickInside = 3f;
            const float minorTickOutside = 5f;

            using var majorTickPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.3f, IsAntialias = true };
            using var minorTickPaint = new SKPaint { Color = new SKColor(70, 70, 70), StrokeWidth = 1f, IsAntialias = true };

            foreach (var tick in xTicks)
            {
                var px = TransformX(tick.Value);
                if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                    continue;

                var inside = tick.IsMajor ? majorTickInside : minorTickInside;
                var outside = tick.IsMajor ? majorTickOutside : minorTickOutside;
                var paint = tick.IsMajor ? majorTickPaint : minorTickPaint;
                canvas.DrawLine(px, chartRect.Bottom - inside, px, chartRect.Bottom + outside, paint);
            }

            foreach (var tick in yTicks)
            {
                var py = TransformY(tick.Value);
                if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                    continue;

                var inside = tick.IsMajor ? majorTickInside : minorTickInside;
                var outside = tick.IsMajor ? majorTickOutside : minorTickOutside;
                var paint = tick.IsMajor ? majorTickPaint : minorTickPaint;
                canvas.DrawLine(chartRect.Left - outside, py, chartRect.Left + inside, py, paint);
            }

            using (var zeroPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.2f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0) })
            {
                if (xMin < 0 && xMax > 0)
                {
                    var zeroX = TransformX(0);
                    canvas.DrawLine(zeroX, chartRect.Top, zeroX, chartRect.Bottom, zeroPaint);
                }
            }

            if (test.GraphFlashPosition >= xMin && test.GraphFlashPosition <= xMax)
            {
                using var flashPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.5f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0) };
                var flashX = TransformX(test.GraphFlashPosition);
                canvas.DrawLine(flashX, chartRect.Top, flashX, chartRect.Bottom, flashPaint);
            }

            var graphStyles = test.GraphStyles ?? Array.Empty<GraphStyle>();

            if (context.Markers.Length > 0)
            {
                foreach (var marker in context.Markers)
                {
                    var px = TransformX(marker.PositionMs);
                    if (double.IsNaN(px) || double.IsInfinity(px))
                        continue;
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;

                    var markerColor = GetMarkerColor(marker);
                    var skColor = new SKColor(markerColor.R, markerColor.G, markerColor.B);

                    using var markerPaint = new SKPaint
                    {
                        Color = skColor,
                        StrokeWidth = 1.5f,
                        IsAntialias = true,
                        PathEffect = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0)
                    };
                    canvas.DrawLine(px, chartRect.Top, px, chartRect.Bottom, markerPaint);

                    using var labelPaint = new SKPaint
                    {
                        Color = skColor,
                        TextSize = 16f,
                        IsAntialias = true,
                        IsStroke = false,
                        FakeBoldText = true
                    };

                    var label = GetMarkerLabel(marker);
                    var textWidth = labelPaint.MeasureText(label);
                    float labelY = chartRect.Top - 6f;
                    if (labelY < 12f)
                        labelY = chartRect.Top + 16f;
                    canvas.DrawText(label, px - textWidth / 2f, labelY, labelPaint);
                }
            }

            canvas.Save();
            canvas.ClipRect(chartRect);

            double graphDt = test.GraphDt;
            bool hasGraphDt = graphDt > 0;

            for (int graphIndex = 0; graphIndex < context.Curves; graphIndex++)
            {
                var samples = context.Graphs[graphIndex];
                if (samples == null || samples.Length == 0)
                    continue;

                int count = context.DeclaredPointCount > 1 ? Math.Min(context.DeclaredPointCount, samples.Length) : samples.Length;
                if (count < 2)
                    continue;

                using var path = new SKPath();
                bool hasPoint = false;
                for (int point = 0; point < count; point++)
                {
                    double xValue;
                    if (hasGraphDt)
                    {
                        xValue = point * graphDt;
                        if (xValue < xMin)
                            continue;
                        if (xValue > xMax)
                            break;
                    }
                    else if (count == 1)
                    {
                        xValue = xMin;
                    }
                    else
                    {
                        xValue = xMin + (xMax - xMin) * point / (count - 1);
                    }

                    double yValue = samples[point];

                    var px = TransformX(xValue);
                    var py = TransformY(yValue);
                    if (double.IsNaN(px) || double.IsNaN(py) || double.IsInfinity(px) || double.IsInfinity(py))
                        continue;

                    if (!hasPoint)
                    {
                        path.MoveTo(px, py);
                        hasPoint = true;
                    }
                    else
                    {
                        path.LineTo(px, py);
                    }
                }

                if (!hasPoint)
                    continue;

                var style = graphIndex < graphStyles.Length ? graphStyles[graphIndex] : null;
                var color = style != null ? new SKColor(style.Red, style.Green, style.Blue) : new SKColor(56, 109, 179);

                using var linePaint = new SKPaint { Color = color, StrokeWidth = 5f, IsAntialias = true, Style = SKPaintStyle.Stroke };
                if (style?.Dotted == true)
                {
                    linePaint.PathEffect = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0);
                }

                canvas.DrawPath(path, linePaint);
            }

            canvas.Restore();

            using (var labelPaint = new SKPaint { Color = SKColors.Black, TextSize = 14f, IsAntialias = true })
            {
                var metrics = labelPaint.FontMetrics;
                float textHeight = metrics.Descent - metrics.Ascent;

                foreach (var tick in xTicks)
                {
                    if (!tick.IsMajor)
                        continue;

                    var px = TransformX(tick.Value);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;
                    var text = FormatAxisValue(tick.Value);
                    var textWidth = labelPaint.MeasureText(text);
                    canvas.DrawText(text, px - textWidth / 2f, chartRect.Bottom + textHeight, labelPaint);
                }

                foreach (var tick in yTicks)
                {
                    if (!tick.IsMajor)
                        continue;

                    var py = TransformY(tick.Value);
                    if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                        continue;
                    var text = FormatAxisValue(tick.Value);
                    var textWidth = labelPaint.MeasureText(text);
                    canvas.DrawText(text, chartRect.Left - 10f - textWidth, py + textHeight / 3f, labelPaint);
                }
            }

            using (var titlePaint = new SKPaint { Color = SKColors.Black, TextSize = 16f, IsAntialias = true })
            {
                var xLabel = "ms";
                var xWidth = titlePaint.MeasureText(xLabel);
                var midX = (chartRect.Left + chartRect.Right) / 2f;
                canvas.DrawText(xLabel, midX - xWidth / 2f, height - 12, titlePaint);

                var yLabel = "µV";
                canvas.Save();
                canvas.Translate(20, (chartRect.Top + chartRect.Bottom) / 2f);
                canvas.RotateDegrees(-90);
                var yWidth = titlePaint.MeasureText(yLabel);
                canvas.DrawText(yLabel, -yWidth / 2f, 0, titlePaint);
                canvas.Restore();
            }

            using var snapshot = surface.Snapshot();
            using var data = snapshot.Encode(SKEncodedImageFormat.Png, 90);
            if (data == null)
                return null;

            return new GraphImage(data.ToArray(), width, height);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or TypeInitializationException or NotSupportedException)
        {
            RenderingSupport.DisableGraphRendering($"Построение графиков отключено: {ex.Message}");
            return null;
        }
    }

    private static GraphImage? TryRenderGraphImageWithGdi(ErgTest test, GraphRenderContext context)
    {
        const int width = 900;
        const int height = 540;

        try
        {
            using var bitmap = new Bitmap(width, height);
            bitmap.SetResolution(96, 96);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.Clear(System.Drawing.Color.White);

            const float marginLeft = 80f;
            const float marginRight = 30f;
            const float marginTop = 24f;
            const float marginBottom = 80f;
            var chartRect = new RectangleF(marginLeft, marginTop, width - marginLeft - marginRight, height - marginTop - marginBottom);

            double xMin = context.XMin;
            double xMax = context.XMax;
            double yMin = context.YMin;
            double yMax = context.YMax;

            float TransformX(double value) => (float)(chartRect.Left + (value - xMin) / (xMax - xMin) * chartRect.Width);
            float TransformY(double value) => (float)(chartRect.Bottom - (value - yMin) / (yMax - yMin) * chartRect.Height);

            const float majorTickInside = 6f;
            const float majorTickOutside = 8f;
            const float minorTickInside = 3f;
            const float minorTickOutside = 5f;

            var xScale = DetermineAxisScale(xMin, xMax, test.GraphXValueStep, test.GraphXLineStep);
            var yScale = DetermineAxisScale(yMin, yMax, test.GraphYValueStep, test.GraphYLineStep);
            var xTicks = BuildAxisTicks(xMin, xMax, xScale);
            var yTicks = BuildAxisTicks(yMin, yMax, yScale);

            using var majorGridPen = new Pen(System.Drawing.Color.FromArgb(210, 210, 210), 1.2f);
            using var minorGridPen = new Pen(System.Drawing.Color.FromArgb(235, 235, 235), 0.8f);

            foreach (var tick in xTicks)
            {
                var px = TransformX(tick.Value);
                if (px <= chartRect.Left + 1 || px >= chartRect.Right - 1)
                    continue;

                var pen = tick.IsMajor ? majorGridPen : minorGridPen;
                graphics.DrawLine(pen, px, chartRect.Top, px, chartRect.Bottom);
            }

            foreach (var tick in yTicks)
            {
                if (IsApproximatelyZero(tick.Value))
                    continue;

                var py = TransformY(tick.Value);
                if (py <= chartRect.Top + 1 || py >= chartRect.Bottom - 1)
                    continue;

                var pen = tick.IsMajor ? majorGridPen : minorGridPen;
                graphics.DrawLine(pen, chartRect.Left, py, chartRect.Right, py);
            }

            using (var axisPen = new Pen(System.Drawing.Color.Black, 1.8f))
            {
                graphics.DrawLine(axisPen, chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom);
                graphics.DrawLine(axisPen, chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom);
            }

            using var majorTickPen = new Pen(System.Drawing.Color.Black, 1.3f);
            using var minorTickPen = new Pen(System.Drawing.Color.FromArgb(70, 70, 70), 1f);

            foreach (var tick in xTicks)
            {
                var px = TransformX(tick.Value);
                if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                    continue;

                var inside = tick.IsMajor ? majorTickInside : minorTickInside;
                var outside = tick.IsMajor ? majorTickOutside : minorTickOutside;
                var pen = tick.IsMajor ? majorTickPen : minorTickPen;
                graphics.DrawLine(pen, px, chartRect.Bottom - inside, px, chartRect.Bottom + outside);
            }

            foreach (var tick in yTicks)
            {
                var py = TransformY(tick.Value);
                if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                    continue;

                var inside = tick.IsMajor ? majorTickInside : minorTickInside;
                var outside = tick.IsMajor ? majorTickOutside : minorTickOutside;
                var pen = tick.IsMajor ? majorTickPen : minorTickPen;
                graphics.DrawLine(pen, chartRect.Left - outside, py, chartRect.Left + inside, py);
            }

            using (var dashedPen = new Pen(System.Drawing.Color.Black, 1.2f) { DashPattern = new[] { 4f, 4f } })
            {
                if (xMin < 0 && xMax > 0)
                {
                    var zeroX = TransformX(0);
                    graphics.DrawLine(dashedPen, zeroX, chartRect.Top, zeroX, chartRect.Bottom);
                }
            }

            if (test.GraphFlashPosition >= xMin && test.GraphFlashPosition <= xMax)
            {
                using var flashPen = new Pen(System.Drawing.Color.Black, 1.5f) { DashPattern = new[] { 6f, 4f } };
                var flashX = TransformX(test.GraphFlashPosition);
                graphics.DrawLine(flashPen, flashX, chartRect.Top, flashX, chartRect.Bottom);
            }

            var graphStyles = test.GraphStyles ?? Array.Empty<GraphStyle>();

            if (context.Markers.Length > 0)
            {
                using var markerFont = new System.Drawing.Font("Arial", 10f, FontStyle.Bold, GraphicsUnit.Point);

                foreach (var marker in context.Markers)
                {
                    var px = TransformX(marker.PositionMs);
                    if (float.IsNaN(px) || float.IsInfinity(px))
                        continue;
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;

                    var markerColor = GetMarkerColor(marker);
                    using var markerPen = new Pen(markerColor, 1.5f) { DashPattern = new[] { 6f, 4f } };
                    graphics.DrawLine(markerPen, px, chartRect.Top, px, chartRect.Bottom);

                    var label = GetMarkerLabel(marker);
                    using var markerBrush = new SolidBrush(markerColor);
                    var size = graphics.MeasureString(label, markerFont);
                    float labelY = chartRect.Top - size.Height;
                    if (labelY < 2f)
                        labelY = chartRect.Top + 2f;
                    graphics.DrawString(label, markerFont, markerBrush, px - size.Width / 2f, labelY);
                }
            }

            var state = graphics.Save();
            graphics.SetClip(chartRect);

            double graphDt = test.GraphDt;
            bool hasGraphDt = graphDt > 0;

            for (int graphIndex = 0; graphIndex < context.Curves; graphIndex++)
            {
                var samples = context.Graphs[graphIndex];
                if (samples == null || samples.Length == 0)
                    continue;

                int count = context.DeclaredPointCount > 1 ? Math.Min(context.DeclaredPointCount, samples.Length) : samples.Length;
                if (count < 2)
                    continue;

                var points = new List<PointF>(count);
                for (int point = 0; point < count; point++)
                {
                    double xValue;
                    if (hasGraphDt)
                    {
                        xValue = point * graphDt;
                        if (xValue < xMin)
                            continue;
                        if (xValue > xMax)
                            break;
                    }
                    else if (count == 1)
                    {
                        xValue = xMin;
                    }
                    else
                    {
                        xValue = xMin + (xMax - xMin) * point / (count - 1);
                    }

                    double yValue = samples[point];

                    var px = TransformX(xValue);
                    var py = TransformY(yValue);
                    if (float.IsNaN(px) || float.IsNaN(py) || float.IsInfinity(px) || float.IsInfinity(py))
                        continue;

                    points.Add(new PointF(px, py));
                }

                if (points.Count < 2)
                    continue;

                var style = graphIndex < graphStyles.Length ? graphStyles[graphIndex] : null;
                var color = style != null ? System.Drawing.Color.FromArgb(style.Red, style.Green, style.Blue) : System.Drawing.Color.FromArgb(56, 109, 179);

                using var pen = new Pen(color, 5f) { LineJoin = LineJoin.Round };
                if (style?.Dotted == true)
                {
                    pen.DashPattern = new[] { 6f, 4f };
                }

                graphics.DrawLines(pen, points.ToArray());
            }

            graphics.Restore(state);

            using var tickFont = new System.Drawing.Font("Arial", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

            foreach (var tick in xTicks)
            {
                if (!tick.IsMajor)
                    continue;

                var px = TransformX(tick.Value);
                if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                    continue;
                var text = FormatAxisValue(tick.Value);
                var size = graphics.MeasureString(text, tickFont);
                graphics.DrawString(text, tickFont, Brushes.Black, px - size.Width / 2f, chartRect.Bottom + size.Height);
            }

            using var tickFormatLeft = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

            foreach (var tick in yTicks)
            {
                if (!tick.IsMajor)
                    continue;

                var py = TransformY(tick.Value);
                if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                    continue;
                var rect = new RectangleF(chartRect.Left - 16f, py - tickFont.GetHeight(graphics) / 2f, 40f, tickFont.GetHeight(graphics));
                graphics.DrawString(FormatAxisValue(tick.Value), tickFont, Brushes.Black, rect, tickFormatLeft);
            }

            using var axisTitleFont = new System.Drawing.Font("Arial", 11f, FontStyle.Regular, GraphicsUnit.Point);
            var xLabelSize = graphics.MeasureString("ms", axisTitleFont);
            graphics.DrawString("ms", axisTitleFont, Brushes.Black, chartRect.Left + (chartRect.Width - xLabelSize.Width) / 2f, height - xLabelSize.Height - 6f);

            graphics.TranslateTransform(20f, chartRect.Top + chartRect.Height / 2f);
            graphics.RotateTransform(-90f);
            var yLabelSize = graphics.MeasureString("µV", axisTitleFont);
            graphics.DrawString("µV", axisTitleFont, Brushes.Black, -yLabelSize.Width / 2f, -yLabelSize.Height / 2f);
            graphics.ResetTransform();

            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return new GraphImage(ms.ToArray(), width, height);
        }
        catch (Exception ex) when (ex is ExternalException or ArgumentException or PlatformNotSupportedException)
        {
            RenderingSupport.DisableGraphRendering($"Построение графиков отключено: {ex.Message}");
            return null;
        }
    }

    private static double DetermineAxisStep(double min, double max, int valueStep, int lineStep)
    {
        var range = max - min;
        if (range <= 0)
            return 0;

        if (valueStep > 0 && lineStep > 0)
            return valueStep * lineStep;

        if (valueStep > 0)
            return valueStep;

        var roughStep = range / 10.0;
        if (roughStep <= 0)
            return 0;

        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
        var normalized = roughStep / magnitude;
        double stepNormalized = normalized switch
        {
            < 1.5 => 1,
            < 3 => 2,
            < 7 => 5,
            _ => 10
        };

        return stepNormalized * magnitude;
    }

    private static AxisScale DetermineAxisScale(double min, double max, int valueStep, int lineStep)
    {
        var majorStep = DetermineAxisStep(min, max, valueStep, lineStep);
        if (majorStep <= 0)
            return new AxisScale(0, 0);

        var subdivisions = DetermineMinorSubdivisions(majorStep, valueStep, lineStep);
        var range = max - min;
        if (range > 0)
        {
            const double minMajorTicks = 4.0;
            const double maxMajorTicks = 12.0;

            var majorCount = range / majorStep;
            if (majorCount < minMajorTicks)
            {
                var adjustedStep = majorStep / 2.0;
                if (adjustedStep > 1e-6)
                {
                    majorStep = adjustedStep;
                    majorCount = range / majorStep;
                    subdivisions = Math.Max(DetermineMinorSubdivisions(majorStep, valueStep, lineStep), subdivisions > 0 ? subdivisions : 1);
                }
            }

            while (majorCount > maxMajorTicks && majorStep > 0)
            {
                majorStep *= 2;
                majorCount = range / majorStep;
                subdivisions = DetermineMinorSubdivisions(majorStep, valueStep, lineStep);
            }
        }

        return new AxisScale(majorStep, subdivisions);
    }

    private static int DetermineMinorSubdivisions(double majorStep, int valueStep, int lineStep)
    {
        if (majorStep <= 0)
            return 0;

        if (valueStep > 0 && lineStep > 0)
            return Math.Clamp(lineStep - 1, 0, 8);

        if (valueStep > 0 && majorStep > valueStep)
        {
            var subdivisions = (int)Math.Round(majorStep / valueStep) - 1;
            return Math.Clamp(subdivisions, 0, 8);
        }

        if (valueStep > 0 && Math.Abs(majorStep - valueStep) < 1e-6)
            return 0;

        return 3;
    }

    private static AxisTick[] BuildAxisTicks(double min, double max, AxisScale scale)
    {
        if (max <= min)
            return Array.Empty<AxisTick>();

        var ticks = new List<AxisTick>();
        var majorValues = new List<double>();
        var majorStep = scale.MajorStep;

        if (majorStep > 0)
        {
            var start = Math.Floor(min / majorStep) * majorStep;
            var end = Math.Ceiling(max / majorStep) * majorStep;
            for (double value = start; value <= end + 1e-6; value += majorStep)
            {
                if (value < min - 1e-6 || value > max + 1e-6)
                    continue;

                majorValues.Add(NormalizeTickValue(value));
            }

            majorValues.Add(NormalizeTickValue(min));
            majorValues.Add(NormalizeTickValue(max));

            majorValues = majorValues
                .Where(v => !double.IsNaN(v) && !double.IsInfinity(v) && v >= min - 1e-6 && v <= max + 1e-6)
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            if (majorValues.Count == 0)
            {
                majorValues.Add(NormalizeTickValue(min));
                majorValues.Add(NormalizeTickValue(max));
            }

            for (int i = 0; i < majorValues.Count; i++)
            {
                var current = majorValues[i];
                ticks.Add(new AxisTick(current, true));

                if (i >= majorValues.Count - 1 || scale.MinorSubdivisions <= 0)
                    continue;

                var next = majorValues[i + 1];
                var interval = next - current;
                if (interval <= 1e-9)
                    continue;

                var minorStep = interval / (scale.MinorSubdivisions + 1);
                for (int j = 1; j <= scale.MinorSubdivisions; j++)
                {
                    var minorValue = current + minorStep * j;
                    if (minorValue <= min + 1e-6 || minorValue >= max - 1e-6)
                        continue;

                    ticks.Add(new AxisTick(NormalizeTickValue(minorValue), false));
                }
            }
        }
        else
        {
            const int fallbackSegments = 8;
            for (int i = 0; i <= fallbackSegments; i++)
            {
                var value = min + (max - min) * i / fallbackSegments;
                ticks.Add(new AxisTick(NormalizeTickValue(value), true));
            }
        }

        return ticks
            .GroupBy(t => t.Value)
            .Select(group => new AxisTick(group.Key, group.Any(t => t.IsMajor)))
            .OrderBy(t => t.Value)
            .ToArray();
    }

    private static double NormalizeTickValue(double value) => Math.Round(value, 6);

    private static bool IsApproximatelyZero(double value) => Math.Abs(value) < 1e-6;

    private static string FormatAxisValue(double value)
    {
        if (Math.Abs(value) >= 1000)
            return value.ToString("0", CultureInfo.InvariantCulture);

        if (Math.Abs(value) >= 100)
            return value.ToString("0.#", CultureInfo.InvariantCulture);

        if (Math.Abs(value) >= 1)
            return value.ToString("0.##", CultureInfo.InvariantCulture);

        return value.ToString("0.###", CultureInfo.InvariantCulture);
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
                    new InsideHorizontalBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 0 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Color = "CCCCCC", Size = 0 }
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

    private static void AppendClientDescription(Body body, string description)
    {
        body.Append(CreateParagraph("Описание:", fontSizePt: 12, bold: true, spacingBefore: TwipsFromPoints(18), spacingAfter: TwipsFromPoints(4)));
        body.Append(CreateParagraph(description, fontSizePt: 11));
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

    private static TableCell CreateClientHeaderCell(string text, int gridSpan)
    {
        var props = new TableCellProperties(
            new GridSpan { Val = gridSpan },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
            new Shading { Fill = "EEEEEE", Val = ShadingPatternValues.Clear, Color = "000000" }
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

    private static TableCell CreateClientEyeHeaderCell(string label, EyeData eye)
    {
        var props = new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
        props.Append(new TableCellMargin
        {
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            TopMargin = new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "20", Type = TableWidthUnitValues.Dxa }
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
            var drawing = CreateImageDrawing(mainPart, graph, $"client-{suffix}-{index}", ref imageId, maxWidthInches: 3.6, maxHeightInches: 2.2);
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

    private static void AppendClientWaveParagraphs(TableCell cell, string label, WaveDisplay display)
    {
        cell.Append(CreateClientWaveValuesTable(label, display));
    }

    private static Table CreateClientWaveValuesTable(string label, WaveDisplay display)
    {
        bool hasLabel = !string.IsNullOrWhiteSpace(label);
        var table = new Table(
            new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "4800" },
                new TableJustification { Val = TableRowAlignmentValues.Center },
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
        }

        if (display.IsFlat)
        {
            row.Append(CreateClientWaveFlatCell(gridSpan: 2, fullWidth: !hasLabel));
        }
        else
        {
            row.Append(CreateClientWaveValueCell(display.MsValue, display.MsNorm, fullWidth: !hasLabel));
            row.Append(CreateClientWaveValueCell(display.MkVValue, display.MkVNorm, fullWidth: !hasLabel));
        }

        table.Append(row);

        return table;
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
        cell.Append(CreateParagraph(text, fontSizePt: 11, bold: true, justification: JustificationValues.Left));
        return cell;
    }

    private static TableCell CreateClientWaveFlatCell(int gridSpan, bool fullWidth)
    {
        var props = new TableCellProperties(
            new GridSpan { Val = gridSpan },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );
        props.Append(new TableCellMargin
        {
            TopMargin = new TopMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
            LeftMargin = new LeftMargin { Width = fullWidth ? "60" : "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = fullWidth ? "60" : "80", Type = TableWidthUnitValues.Dxa }
        });

        var cell = new TableCell(props);
        cell.Append(CreateParagraph("FLAT", fontSizePt: 26, bold: true, justification: JustificationValues.Center));
        return cell;
    }

    private static TableCell CreateClientWaveValueCell(string value, string norm, bool fullWidth)
    {
        var cellProps = new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );
        cellProps.Append(new TableCellMargin
        {
            TopMargin = new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
            LeftMargin = new LeftMargin { Width = fullWidth ? "40" : "60", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = fullWidth ? "40" : "60", Type = TableWidthUnitValues.Dxa }
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

    private static Paragraph CreateMeasurementParagraph(string value)
    {
        var paragraph = new Paragraph();
        paragraph.Append(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));

        if (string.IsNullOrWhiteSpace(value) || value == "—")
        {
            paragraph.Append(CreateMeasurementRun("—", 26, bold: true));
            return paragraph;
        }

        var (number, unit) = SplitValueAndUnit(value);

        paragraph.Append(CreateMeasurementRun(number, 26, bold: true));

        if (!string.IsNullOrEmpty(unit))
        {
            const int unitOffset = 4;
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

    private static void AppendClientNormParagraph(TableCell cell, string value)
    {
        var formatted = FormatNormForClient(value);
        if (formatted == null)
            return;

        cell.Append(CreateParagraph(formatted, fontSizePt: 9, colorHex: "666666", justification: JustificationValues.Center));
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

        var msText = measurement.Ms.HasValue ? $"{measurement.Ms.Value:0} мс" : "—";
        var mkvText = measurement.MkV.HasValue ? $"{measurement.MkV.Value:0} мкВ" : "—";

        return new WaveDisplay(eye.IsFlat, msText, mkvText, msNorm, mkvNorm);
    }

    private static double? GetFirstValue(ushort?[]? values)
    {
        if (values == null)
            return null;

        foreach (var value in values)
        {
            if (value.HasValue)
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
            if (value.HasValue)
                return value.Value;
        }

        return null;
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

    private static string GetApplicationVersion()
    {
        var version = typeof(ErgReportBuilder).Assembly.GetName().Version;
        return version?.ToString() ?? "—";
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

        body.Append(CreateParagraph("Первые 10 точек (правый глаз, график 1): " + BuildGraphPreview(test.RightEye.GraphsNormalized, test.GraphNumPoints), fontSizePt: 10));
        body.Append(CreateParagraph("Первые 10 точек (левый глаз, график 1): " + BuildGraphPreview(test.LeftEye.GraphsNormalized, test.GraphNumPoints), fontSizePt: 10));
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

        var imageRow = new TableRow();
        imageRow.Append(CreateGraphImageCell(mainPart, rightGraph, "right-eye", ref imageId));
        imageRow.Append(CreateGraphImageCell(mainPart, leftGraph, "left-eye", ref imageId));
        table.Append(imageRow);

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
            var drawing = CreateImageDrawing(mainPart, image, name, ref imageId, maxWidthInches: 3.6, maxHeightInches: 3.0);
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

    private static Drawing CreateImageDrawing(MainDocumentPart mainPart, GraphImage image, string name, ref uint imageId, double maxWidthInches, double maxHeightInches)
    {
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var stream = new MemoryStream(image.Data))
        {
            imagePart.FeedData(stream);
        }

        var relationshipId = mainPart.GetIdOfPart(imagePart);
        long cx = PixelsToEmus(image.Width);
        long cy = PixelsToEmus(image.Height);
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
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            )
            { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U }
        );
    }

    private static long PixelsToEmus(int pixels) => (long)(pixels / 96.0 * 914400);

    private static long InchesToEmus(double inches) => (long)(inches * 914400);

    private static void EnsureDocumentPropertiesParts(WordprocessingDocument document, string? title)
    {
        if (document == null)
            return;

        var corePart = document.CoreFilePropertiesPart ?? document.AddCoreFilePropertiesPart();
        WriteCoreProperties(corePart, title);

        var appPart = document.ExtendedFilePropertiesPart ?? document.AddExtendedFilePropertiesPart();
        WriteExtendedProperties(appPart);
    }

    private static void EnsureWordDocumentInfrastructure(MainDocumentPart mainPart)
    {
        if (mainPart == null)
            return;

        EnsureDefaultStyles(mainPart);
        EnsureFontTable(mainPart);
        EnsureDocumentSettings(mainPart);
    }

    private static void EnsureDefaultStyles(MainDocumentPart mainPart)
    {
        if (mainPart == null)
            return;

        var stylesPart = mainPart.StyleDefinitionsPart ?? mainPart.AddNewPart<StyleDefinitionsPart>();
        var styles = stylesPart.Styles ?? new Styles();
        stylesPart.Styles = styles;

        bool changed = false;

        if (styles.DocDefaults == null)
        {
            styles.DocDefaults = new DocDefaults(
                new RunPropertiesDefault(new RunProperties(
                    new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", EastAsia = "Calibri", ComplexScript = "Calibri" },
                    new FontSize { Val = "22" }
                )),
                new ParagraphPropertiesDefault(new ParagraphProperties())
            );
            changed = true;
        }

        if (!styles.Elements<Style>().Any(s => string.Equals(s.StyleId, "Normal", StringComparison.Ordinal)))
        {
            var normalStyle = new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = "Normal",
                Default = true
            };
            normalStyle.Append(new StyleName { Val = "Normal" });
            normalStyle.Append(new UIPriority { Val = 0 });
            normalStyle.Append(new PrimaryStyle());
            normalStyle.Append(new StyleRunProperties(
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", EastAsia = "Calibri", ComplexScript = "Calibri" },
                new FontSize { Val = "22" }
            ));
            styles.Append(normalStyle);
            changed = true;
        }

        if (!styles.Elements<Style>().Any(s => string.Equals(s.StyleId, "TableNormal", StringComparison.Ordinal)))
        {
            var tableNormal = new Style
            {
                Type = StyleValues.Table,
                StyleId = "TableNormal",
                Default = true
            };
            tableNormal.Append(new StyleName { Val = "Normal Table" });
            tableNormal.Append(new UIPriority { Val = 1 });
            tableNormal.Append(new PrimaryStyle());
            tableNormal.Append(new StyleTableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Nil },
                    new LeftBorder { Val = BorderValues.Nil },
                    new BottomBorder { Val = BorderValues.Nil },
                    new RightBorder { Val = BorderValues.Nil },
                    new InsideHorizontalBorder { Val = BorderValues.Nil },
                    new InsideVerticalBorder { Val = BorderValues.Nil }
                )));
            styles.Append(tableNormal);
            changed = true;
        }

        if (!styles.Elements<Style>().Any(s => string.Equals(s.StyleId, "TableGrid", StringComparison.Ordinal)))
        {
            var tableGrid = new Style
            {
                Type = StyleValues.Table,
                StyleId = "TableGrid"
            };
            tableGrid.Append(new StyleName { Val = "Table Grid" });
            tableGrid.Append(new BasedOn { Val = "TableNormal" });
            tableGrid.Append(new UIPriority { Val = 59 });
            tableGrid.Append(new PrimaryStyle());
            tableGrid.Append(new StyleTableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                )));
            styles.Append(tableGrid);
            changed = true;
        }

        if (changed)
        {
            styles.Save(stylesPart);
        }
    }

    private static void EnsureFontTable(MainDocumentPart mainPart)
    {
        if (mainPart == null)
            return;

        var fontPart = mainPart.FontTablePart ?? mainPart.AddNewPart<FontTablePart>();
        var fonts = fontPart.FontTable ?? new Fonts();
        bool changed = false;

        bool EnsureFont(string name)
        {
            if (fonts.Elements<Font>().Any(f => string.Equals(f.Name?.Value, name, StringComparison.OrdinalIgnoreCase)))
                return false;

            var font = new Font { Name = name };
            font.Append(new FontFamilyNumbering { Val = FontFamilyNumberingValues.Swiss });
            font.Append(new Pitch { Val = FontPitchValues.Variable });
            font.Append(new FontCharSet { Val = "00" });
            fonts.Append(font);
            return true;
        }

        if (EnsureFont("Calibri"))
            changed = true;
        if (EnsureFont("Arial"))
            changed = true;

        if (changed)
            fontPart.FontTable = fonts;
    }

    private static void EnsureDocumentSettings(MainDocumentPart mainPart)
    {
        if (mainPart == null)
            return;

        var settingsPart = mainPart.DocumentSettingsPart ?? mainPart.AddNewPart<DocumentSettingsPart>();
        var settings = settingsPart.Settings ?? new Settings();
        bool changed = false;

        if (!settings.Elements<Zoom>().Any())
        {
            settings.PrependChild(new Zoom { Percent = 100 });
            changed = true;
        }

        if (!settings.Elements<DefaultTabStop>().Any())
        {
            settings.Append(new DefaultTabStop { Val = 720 });
            changed = true;
        }

        CompatibilitySetting CreateCompatibilitySetting()
            => new()
            {
                Name = new EnumValue<CompatSettingNameValues>(CompatSettingNameValues.CompatSettingNameWord2010),
                Val = "14",
                Uri = "http://schemas.microsoft.com/office/word"
            };

        var compatibility = settings.Elements<Compatibility>().FirstOrDefault();
        if (compatibility == null)
        {
            compatibility = new Compatibility();
            compatibility.Append(CreateCompatibilitySetting());
            settings.Append(compatibility);
            changed = true;
        }
        else if (!compatibility.Elements<CompatibilitySetting>().Any(s => s.Name?.Value == CompatSettingNameValues.CompatSettingNameWord2010))
        {
            compatibility.Append(CreateCompatibilitySetting());
            changed = true;
        }

        if (!settings.Elements<UpdateFieldsOnOpen>().Any())
        {
            settings.Append(new UpdateFieldsOnOpen { Val = true });
            changed = true;
        }

        if (changed)
            settingsPart.Settings = settings;
    }

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

    private static void ApplyPageMargins(Body body, double leftCm, double rightCm, double? topCm, double? bottomCm)
    {
        if (body == null)
            return;

        var sectionProps = body.Elements<SectionProperties>().LastOrDefault();
        if (sectionProps != null)
        {
            sectionProps.Remove();
        }
        else
        {
            sectionProps = new SectionProperties();
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

        body.Append(sectionProps);
    }

    private static int TwipsFromPoints(double points) => (int)Math.Round(points * 20);

    private static int TwipsFromCentimeters(double centimeters)
        => (int)Math.Round(centimeters / 2.54 * 1440);

    private static string PointsToHalfPointString(double points)
        => Math.Round(points * 2).ToString(CultureInfo.InvariantCulture);

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
}
