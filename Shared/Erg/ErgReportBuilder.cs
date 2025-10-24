using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

namespace ErgData;

public static class ErgReportBuilder
{
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

                    if (!string.IsNullOrWhiteSpace(rawFilePath))
                    {
                        column.Item().Text($"Источник бинарных данных: {rawFilePath}").FontSize(9).FontColor(Colors.Grey.Darken1);
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
        QuestDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(11));

                var clinicHeader = string.IsNullOrWhiteSpace(clinicName)
                    ? "Шапка [название организации]"
                    : clinicName!;

                page.Header().Column(column =>
                {
                    column.Spacing(3);
                    column.Item().AlignCenter().Text(clinicHeader).FontSize(12).SemiBold();
                    column.Item().AlignCenter().Text("Отчет по результатам ЭРГ-исследования сетчатки").FontSize(18).SemiBold();
                });

                page.Content().Column(column =>
                {
                    column.Spacing(18);
                    column.Item().Component(new ClientInfoComponent(patient, deviceInfo));

                    for (int i = 0; i < patient.Tests.Count; i++)
                    {
                        column.Item().Component(new ClientTestComponent(i + 1, patient.Tests[i]));
                    }

                    if (!string.IsNullOrWhiteSpace(patient.Description))
                    {
                        column.Item().Component(new ClientDescriptionComponent(patient.Description));
                    }

                    if (!string.IsNullOrWhiteSpace(rawFilePath))
                    {
                        column.Item().Text($"Источник бинарных данных: {rawFilePath}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.DefaultTextStyle(style => style.FontSize(9).FontColor(Colors.Grey.Darken1));
                    txt.Span("Стр. ");
                    txt.CurrentPageNumber();
                    txt.Span(" из ");
                    txt.TotalPages();
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

        using var document = WordprocessingDocument.Create(docxPath, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new WordDocument(new Body());
        var body = mainPart.Document.Body ?? throw new InvalidOperationException("Не удалось создать тело документа Word.");

        body.Append(CreateParagraph(headerTitle, fontSizePt: 16, bold: true, justification: JustificationValues.Center, spacingAfter: TwipsFromPoints(12)));
        body.Append(CreateHeaderTable(patient, deviceInfo));

        if (!string.IsNullOrWhiteSpace(patient.Description))
        {
            body.Append(CreateDescriptionTable(patient.Description));
        }

        if (!string.IsNullOrWhiteSpace(rawFilePath))
        {
            body.Append(CreateParagraph($"Источник бинарных данных: {rawFilePath}", fontSizePt: 9, colorHex: "666666", spacingBefore: TwipsFromPoints(6)));
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

        mainPart.Document.Save();
    }

    private static void BuildPatientWordReportClient(ErgPatient patient, string docxPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath)
    {
        using var document = WordprocessingDocument.Create(docxPath, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new WordDocument(new Body());
        var body = mainPart.Document.Body ?? throw new InvalidOperationException("Не удалось создать тело документа Word.");

        var clinicHeader = string.IsNullOrWhiteSpace(clinicName)
            ? "Шапка [название организации]"
            : clinicName!;
        body.Append(CreateParagraph(clinicHeader, fontSizePt: 12, bold: true, justification: JustificationValues.Center, spacingAfter: TwipsFromPoints(4)));

        body.Append(CreateParagraph("Отчет по результатам ЭРГ-исследования сетчатки", fontSizePt: 18, bold: true, justification: JustificationValues.Center, spacingAfter: TwipsFromPoints(14)));
        body.Append(CreateClientInfoTable(patient, deviceInfo));

        uint imageId = 1;

        for (int i = 0; i < patient.Tests.Count; i++)
        {
            var testTable = CreateClientTestTable(mainPart, patient.Tests[i], i + 1, ref imageId);
            if (i > 0)
            {
                body.Append(CreateParagraph(string.Empty, spacingBefore: TwipsFromPoints(12)));
            }
            body.Append(testTable);
        }

        if (!string.IsNullOrWhiteSpace(patient.Description))
        {
            AppendClientDescription(body, patient.Description);
        }

        if (!string.IsNullOrWhiteSpace(rawFilePath))
        {
            body.Append(CreateParagraph($"Источник бинарных данных: {rawFilePath}", fontSizePt: 9, colorHex: "777777", spacingBefore: TwipsFromPoints(6)));
        }

        mainPart.Document.Save();
    }

    private sealed class LegacyPdfRenderer : IDisposable
    {
        private const int Dpi = 200;
        private const int PageWidth = (int)(8.27f * Dpi);
        private const int PageHeight = (int)(11.69f * Dpi);

        private readonly ErgPatient _patient;
        private readonly string _pdfPath;
        private readonly CommonInfo? _deviceInfo;
        private readonly string? _clinicName;
        private readonly string? _rawFilePath;

        private readonly List<byte[]> _pages = new();
        private Bitmap? _bitmap;
        private Graphics? _graphics;
        private float _y;

        private readonly float _marginLeft = 0.8f * Dpi;
        private readonly float _marginRight = 0.8f * Dpi;
        private readonly float _marginTop = 0.8f * Dpi;
        private readonly float _marginBottom = 0.9f * Dpi;
        private readonly float _spacingSmall = 0.08f * Dpi;
        private readonly float _spacingMedium = 0.12f * Dpi;
        private readonly float _spacingLarge = 0.2f * Dpi;
        private readonly float _graphGap = 0.12f * Dpi;
        private readonly float _tableCellPadding = 0.08f * Dpi;

        private float ContentWidth => PageWidth - _marginLeft - _marginRight;

        private readonly System.Drawing.Font _titleFont = new("Arial", 26f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly System.Drawing.Font _sectionFont = new("Arial", 14f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly System.Drawing.Font _headerFont = new("Arial", 12f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly System.Drawing.Font _textFont = new("Arial", 12f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly System.Drawing.Font _smallFont = new("Arial", 10f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly System.Drawing.Font _italicSmallFont = new("Arial", 10f, FontStyle.Italic, GraphicsUnit.Point);
        private readonly System.Drawing.Font _tableHeaderFont = new("Arial", 11f, FontStyle.Bold, GraphicsUnit.Point);
        private readonly System.Drawing.Font _tableFont = new("Arial", 11f, FontStyle.Regular, GraphicsUnit.Point);
        private readonly System.Drawing.Font _graphLabelFont = new("Arial", 11f, FontStyle.Bold, GraphicsUnit.Point);

        private readonly StringFormat _formatLeft = new(StringFormatFlags.LineLimit)
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
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

        private readonly SolidBrush _mutedBrush = new(System.Drawing.Color.FromArgb(90, 90, 90));
        private readonly SolidBrush _descriptionBackgroundBrush = new(System.Drawing.Color.FromArgb(245, 245, 245));
        private readonly SolidBrush _headerBackgroundBrush = new(System.Drawing.Color.FromArgb(232, 232, 232));
        private readonly Pen _tablePen = new(System.Drawing.Color.FromArgb(200, 200, 200));

        public LegacyPdfRenderer(ErgPatient patient, string pdfPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath)
        {
            _patient = patient;
            _pdfPath = pdfPath;
            _deviceInfo = deviceInfo;
            _clinicName = clinicName;
            _rawFilePath = rawFilePath;
            _tablePen.Width = 1f;
        }

        public void Build()
        {
            StartNewPage();
            DrawTitle();
            DrawHeaderInfo();
            DrawDescription();
            DrawRawFilePath();

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
            _titleFont.Dispose();
            _sectionFont.Dispose();
            _headerFont.Dispose();
            _textFont.Dispose();
            _smallFont.Dispose();
            _italicSmallFont.Dispose();
            _tableHeaderFont.Dispose();
            _tableFont.Dispose();
            _graphLabelFont.Dispose();
            _formatLeft.Dispose();
            _formatRight.Dispose();
            _formatCenter.Dispose();
            _mutedBrush.Dispose();
            _descriptionBackgroundBrush.Dispose();
            _headerBackgroundBrush.Dispose();
            _tablePen.Dispose();
        }

        private void StartNewPage()
        {
            FinalizeCurrentPage();

            _bitmap = new Bitmap(PageWidth, PageHeight);
            _bitmap.SetResolution(Dpi, Dpi);
            _graphics = Graphics.FromImage(_bitmap);
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            _graphics.Clear(System.Drawing.Color.White);
            _y = _marginTop;
        }

        private void FinalizeCurrentPage()
        {
            if (_graphics == null || _bitmap == null)
                return;

            _graphics.Dispose();
            using var ms = new MemoryStream();
            _bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            _pages.Add(ms.ToArray());
            _bitmap.Dispose();
            _bitmap = null;
            _graphics = null;
        }

        private void SavePdf()
        {
            using var document = new PdfDocument();
            foreach (var pageImage in _pages)
            {
                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                using var gfx = XGraphics.FromPdfPage(page);
                using var image = XImage.FromStream(() => new MemoryStream(pageImage));
                gfx.DrawImage(image, 0, 0, page.Width, page.Height);
            }

            document.Save(_pdfPath);
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

        private float MeasureText(string text, System.Drawing.Font font, float width, StringFormat? format = null)
        {
            if (_graphics == null || string.IsNullOrWhiteSpace(text))
                return 0f;

            format ??= _formatLeft;
            var size = _graphics.MeasureString(text, font, new SizeF(width, float.MaxValue), format);
            return size.Height;
        }

        private void DrawParagraph(string text, System.Drawing.Font font, System.Drawing.Brush brush, float spacingBefore, float spacingAfter, StringFormat? format = null)
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

        private void DrawParagraphNoEnsure(string text, System.Drawing.Font font, System.Drawing.Brush brush, float spacingBefore, float spacingAfter, StringFormat? format = null)
        {
            if (_graphics == null)
                return;

            if (string.IsNullOrWhiteSpace(text))
            {
                _y += spacingBefore + spacingAfter;
                return;
            }

            format ??= _formatLeft;
            _y += spacingBefore;
            var height = MeasureText(text, font, ContentWidth, format);
            var rect = new RectangleF(_marginLeft, _y, ContentWidth, height);
            _graphics.DrawString(text, font, brush, rect, format);
            _y += height + spacingAfter;
        }

        private void DrawTitle()
        {
            var title = string.IsNullOrWhiteSpace(_clinicName)
                ? "Отчет по результатам ЭРГ-исследования сетчатки"
                : _clinicName!;

            DrawParagraph(title, _titleFont, Brushes.Black, 0, _spacingLarge);
        }

        private void DrawHeaderInfo()
        {
            var rows = new (string Left, string? Right)[]
            {
                ($"ID пациента: {_patient.PatientId}", $"Животное: {FormatAnimal(_patient.Animal)}"),
                ($"Дата/время исследования: {_patient.TestDateTime}", FormatDeviceInfo()),
                ($"Количество тестов: {_patient.Tests.Count} (в блоке указано: {_patient.TotalNumTests})", null)
            };

            foreach (var row in rows)
            {
                var leftHeight = MeasureText(row.Left, _textFont, ContentWidth / 2f, _formatLeft);
                var rightText = row.Right ?? string.Empty;
                var rightHeight = string.IsNullOrWhiteSpace(rightText)
                    ? 0f
                    : MeasureText(rightText, _textFont, ContentWidth / 2f, _formatRight);

                var baseHeight = _textFont.GetHeight(_graphics!);
                var height = Math.Max(baseHeight, Math.Max(leftHeight, rightHeight));
                EnsureSpace(height + _spacingSmall);

                var leftRect = new RectangleF(_marginLeft, _y, ContentWidth / 2f, height);
                _graphics!.DrawString(row.Left, _textFont, Brushes.Black, leftRect, _formatLeft);

                if (!string.IsNullOrWhiteSpace(rightText))
                {
                    var rightRect = new RectangleF(_marginLeft + ContentWidth / 2f, _y, ContentWidth / 2f, height);
                    _graphics.DrawString(rightText, _textFont, Brushes.Black, rightRect, _formatRight);
                }

                _y += height + _spacingSmall * 0.5f;
            }

            _y += _spacingMedium;
        }

        private string FormatDeviceInfo()
        {
            if (_deviceInfo == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(_deviceInfo.DeviceName) && string.IsNullOrWhiteSpace(_deviceInfo.SoftwareRev))
                return string.Empty;

            return $"Прибор: {_deviceInfo.DeviceName}, ПО: {_deviceInfo.SoftwareRev}";
        }

        private void DrawDescription()
        {
            if (_graphics == null || string.IsNullOrWhiteSpace(_patient.Description))
                return;

            var innerWidth = ContentWidth - _tableCellPadding * 2;
            var titleHeight = MeasureText("Автоматическое заключение", _headerFont, innerWidth, _formatLeft);
            var textHeight = MeasureText(_patient.Description, _textFont, innerWidth, _formatLeft);
            var blockHeight = titleHeight + textHeight + _tableCellPadding * 3;

            EnsureSpace(blockHeight + _spacingMedium);

            var outerRect = new RectangleF(_marginLeft, _y, ContentWidth, blockHeight);
            _graphics!.FillRectangle(_descriptionBackgroundBrush, outerRect);

            var titleRect = new RectangleF(
                _marginLeft + _tableCellPadding,
                _y + _tableCellPadding,
                innerWidth,
                titleHeight);

            _graphics.DrawString("Автоматическое заключение", _headerFont, Brushes.Black, titleRect, _formatLeft);

            var textRect = new RectangleF(
                _marginLeft + _tableCellPadding,
                titleRect.Bottom + _tableCellPadding / 2f,
                innerWidth,
                textHeight);

            _graphics.DrawString(_patient.Description, _textFont, Brushes.Black, textRect, _formatLeft);

            _y += blockHeight + _spacingMedium;
        }

        private void DrawRawFilePath()
        {
            if (string.IsNullOrWhiteSpace(_rawFilePath))
                return;

            DrawParagraph($"Источник бинарных данных: {_rawFilePath}", _smallFont, _mutedBrush, 0, _spacingMedium);
        }

        private void DrawTestSection(int index, ErgTest test)
        {
            DrawParagraph($"Тест №{index + 1}: {test.TestName}", _sectionFont, Brushes.Black, _spacingLarge, _spacingSmall);
            DrawParagraph($"Точек: {test.GraphNumPoints}, Δt: {test.GraphDt} мс, дискрет/мкВ: {test.GraphDiscrPerMkV}", _textFont, Brushes.Black, 0, _spacingSmall / 2f);
            DrawParagraph($"Вспышка: {test.GraphFlashPosition} мс", _textFont, Brushes.Black, 0, _spacingSmall / 2f);
            DrawParagraph($"Диапазон X: {test.GraphXScaleMin}…{test.GraphXScaleMax} мс (шаг {test.GraphXValueStep})", _textFont, Brushes.Black, 0, _spacingSmall / 2f);
            DrawParagraph($"Диапазон Y: {test.GraphYScaleMin}…{test.GraphYScaleMax} мкВ (шаг {test.GraphYValueStep})", _textFont, Brushes.Black, 0, _spacingSmall / 2f);
            DrawParagraph($"a-волна: {(test.AWaveExists ? "есть" : "нет")}, нормы ms: {FormatRange(test.AWaveMsNormalMin, test.AWaveMsNormalMax)}, мкВ: {FormatRange(test.AWaveMkVNormalMin, test.AWaveMkVNormalMax)}", _textFont, Brushes.Black, 0, _spacingSmall / 2f);
            DrawParagraph($"b-волна нормы ms: {FormatRange(test.BWaveMsNormalMin, test.BWaveMsNormalMax)}, мкВ: {FormatRange(test.BWaveMkVNormalMin, test.BWaveMkVNormalMax)}", _textFont, Brushes.Black, 0, _spacingSmall);

            DrawEyeTable(test);
            DrawGraphSection(test);
        }

        private void DrawEyeTable(ErgTest test)
        {
            if (_graphics == null)
                return;

            var rows = GetEyeTableRows(test).ToList();
            if (rows.Count == 0)
                return;

            var columnWidths = new[]
            {
                ContentWidth * 0.30f,
                ContentWidth * 0.35f,
                ContentWidth * 0.35f
            };

            var headerTexts = new[] { "Параметр", "Правый глаз", "Левый глаз" };
            var headerFormats = new[] { _formatLeft, _formatCenter, _formatCenter };
            var bodyFormats = new[] { _formatLeft, _formatLeft, _formatLeft };

            float headerHeight = MeasureTableRow(headerTexts, _tableHeaderFont, columnWidths, headerFormats);
            var rowHeights = new List<float>(rows.Count);
            foreach (var row in rows)
            {
                var values = new[] { row.Caption, row.Right, row.Left };
                rowHeights.Add(MeasureTableRow(values, _tableFont, columnWidths, bodyFormats));
            }

            float totalHeight = headerHeight + rowHeights.Sum();
            EnsureSpace(totalHeight + _spacingMedium);

            DrawTableRow(headerTexts, columnWidths, headerHeight, _tableHeaderFont, headerFormats, header: true);
            _y += headerHeight;

            for (int i = 0; i < rows.Count; i++)
            {
                var values = new[] { rows[i].Caption, rows[i].Right, rows[i].Left };
                DrawTableRow(values, columnWidths, rowHeights[i], _tableFont, bodyFormats, header: false);
                _y += rowHeights[i];
            }

            _y += _spacingMedium;
        }

        private float MeasureTableRow(string[] texts, System.Drawing.Font font, float[] widths, StringFormat[] formats)
        {
            float max = 0f;
            for (int i = 0; i < texts.Length; i++)
            {
                var width = Math.Max(10f, widths[i] - _tableCellPadding * 2);
                var height = MeasureText(texts[i], font, width, formats[i]) + _tableCellPadding * 2;
                max = Math.Max(max, height);
            }

            return max;
        }

        private void DrawTableRow(string[] texts, float[] widths, float height, System.Drawing.Font font, StringFormat[] formats, bool header)
        {
            if (_graphics == null)
                return;

            float x = _marginLeft;
            for (int i = 0; i < texts.Length; i++)
            {
                var rect = new RectangleF(x, _y, widths[i], height);
                if (header)
                {
                    _graphics.FillRectangle(_headerBackgroundBrush, rect);
                }

                _graphics.DrawRectangle(_tablePen, rect.X, rect.Y, rect.Width, rect.Height);

                var textRect = new RectangleF(
                    rect.X + _tableCellPadding,
                    rect.Y + _tableCellPadding / 2f,
                    rect.Width - _tableCellPadding * 2,
                    rect.Height - _tableCellPadding);

                var text = string.IsNullOrWhiteSpace(texts[i]) ? "—" : texts[i];
                _graphics.DrawString(text, font, Brushes.Black, textRect, formats[i]);

                x += widths[i];
            }
        }

        private void DrawGraphSection(ErgTest test)
        {
            if (_graphics == null)
                return;

            var rightGraph = TryRenderGraphImage(test, test.RightEye);
            var leftGraph = TryRenderGraphImage(test, test.LeftEye);
            var styles = DescribeGraphStyles(test);

            var infoLine = $"Правый глаз: {test.RightEye.GraphCount} граф., левый глаз: {test.LeftEye.GraphCount} граф.";
            var styleText = styles.Length > 0
                ? "Стили графиков: " + string.Join("; ", styles)
                : null;

            var previewRight = "Первые 10 точек (правый глаз, график 1): " + BuildGraphPreview(test.RightEye.GraphsNormalized, test.GraphNumPoints);
            var previewLeft = "Первые 10 точек (левый глаз, график 1): " + BuildGraphPreview(test.LeftEye.GraphsNormalized, test.GraphNumPoints);

            var graphWidth = (ContentWidth - _graphGap) / 2f;
            float graphHeight = 0f;
            if (rightGraph != null)
            {
                graphHeight = Math.Max(graphHeight, (float)rightGraph.Height / rightGraph.Width * graphWidth);
            }
            if (leftGraph != null)
            {
                graphHeight = Math.Max(graphHeight, (float)leftGraph.Height / leftGraph.Width * graphWidth);
            }
            if (graphHeight <= 0f)
            {
                graphHeight = graphWidth * 0.55f;
            }

            var titleHeight = MeasureText("Графические данные", _sectionFont, ContentWidth);
            var infoHeight = MeasureText(infoLine, _textFont, ContentWidth);
            var styleHeight = string.IsNullOrWhiteSpace(styleText) ? 0f : MeasureText(styleText!, _smallFont, ContentWidth);
            var labelHeight = _graphLabelFont.GetHeight(_graphics);
            var previewHeight1 = MeasureText(previewRight, _smallFont, ContentWidth);
            var previewHeight2 = MeasureText(previewLeft, _smallFont, ContentWidth);

            var blockHeight = titleHeight + infoHeight + styleHeight + graphHeight + labelHeight + previewHeight1 + previewHeight2
                               + _spacingMedium + _spacingSmall * 5f;

            EnsureSpace(blockHeight);

            DrawParagraphNoEnsure("Графические данные", _sectionFont, Brushes.Black, 0, _spacingSmall);
            DrawParagraphNoEnsure(infoLine, _textFont, Brushes.Black, 0, _spacingSmall / 2f);
            if (!string.IsNullOrWhiteSpace(styleText))
            {
                DrawParagraphNoEnsure(styleText!, _smallFont, _mutedBrush, 0, _spacingSmall / 2f);
            }

            var rowTop = _y;
            var rightRect = new RectangleF(_marginLeft, rowTop, graphWidth, graphHeight + labelHeight + _spacingSmall);
            var leftRect = new RectangleF(_marginLeft + graphWidth + _graphGap, rowTop, graphWidth, graphHeight + labelHeight + _spacingSmall);

            DrawGraphWithLabel(rightRect, "Правый глаз", rightGraph);
            DrawGraphWithLabel(leftRect, "Левый глаз", leftGraph);

            _y = rowTop + graphHeight + labelHeight + _spacingSmall;

            DrawParagraphNoEnsure(previewRight, _smallFont, _mutedBrush, _spacingSmall / 2f, _spacingSmall / 2f);
            DrawParagraphNoEnsure(previewLeft, _smallFont, _mutedBrush, 0, _spacingMedium);
        }

        private void DrawGraphWithLabel(RectangleF rect, string label, GraphImage? image)
        {
            if (_graphics == null)
                return;

            var labelHeight = _graphLabelFont.GetHeight(_graphics);
            var labelRect = new RectangleF(rect.X, rect.Y, rect.Width, labelHeight);
            _graphics.DrawString(label, _graphLabelFont, Brushes.Black, labelRect, _formatLeft);

            var imageTop = labelRect.Bottom + _spacingSmall / 4f;
            var imageHeight = rect.Height - (labelHeight + _spacingSmall / 4f);

            if (image != null)
            {
                using var stream = new MemoryStream(image.Data);
                using var bitmap = System.Drawing.Image.FromStream(stream);
                _graphics.DrawImage(bitmap, rect.X, imageTop, rect.Width, imageHeight);
            }
            else
            {
                var placeholderRect = new RectangleF(rect.X, imageTop, rect.Width, imageHeight);
                using var dashedPen = new Pen(System.Drawing.Color.FromArgb(200, 200, 200)) { DashPattern = new[] { 4f, 4f } };
                _graphics.DrawRectangle(dashedPen, placeholderRect.X, placeholderRect.Y, placeholderRect.Width, placeholderRect.Height);
                _graphics.DrawString("Нет данных", _italicSmallFont, _mutedBrush, placeholderRect, _formatCenter);
            }
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
                column.Item().Text($"b-волна нормы ms: {FormatRange(_test.BWaveMsNormalMin, _test.BWaveMsNormalMax)}, мкВ: {FormatRange(_test.BWaveMkVNormalMin, _test.BWaveMkVNormalMax)}");

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
                column.Spacing(3);
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

                if (!string.IsNullOrWhiteSpace(_deviceInfo?.ReportName))
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Название протокола: ").SemiBold();
                        text.Span(_deviceInfo.ReportName);
                    });
                }

                column.Item().Text(text =>
                {
                    text.Span("Версия отчета: ").SemiBold();
                    text.Span(GetApplicationVersion());
                });
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
                column.Spacing(12);
                column.Item().Text(FormatClientTestTitle(_index, _test)).FontSize(14).SemiBold();

                column.Item().Row(row =>
                {
                    row.Spacing(24);
                    row.RelativeItem().Component(new ClientEyeSummaryComponent("Правый глаз", _test, _test.RightEye));
                    row.RelativeItem().Component(new ClientEyeSummaryComponent("Левый глаз", _test, _test.LeftEye));
                });

                column.Item().Text("Графики").FontSize(11).SemiBold();
                column.Item().Row(row =>
                {
                    row.Spacing(24);
                    row.RelativeItem().Component(new ClientGraphComponent("Правый глаз", _test, _test.RightEye));
                    row.RelativeItem().Component(new ClientGraphComponent("Левый глаз", _test, _test.LeftEye));
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
                column.Item().Text(text =>
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
                    column.Item().AlignCenter().Text("FLAT").FontSize(24).SemiBold();
                    AppendFlatNorms(column, BuildWaveDisplay(_test, _eye, WaveKind.B));
                    return;
                }

                foreach (var (label, kind) in GetClientWaveOrder(_test))
                {
                    var display = BuildWaveDisplay(_test, _eye, kind);
                    if (IsWaveDisplayEmpty(display))
                        continue;

                    column.Item().Component(new ClientWaveValuesComponent(label, display));
                }
            });
        }

        private static void AppendFlatNorms(ColumnDescriptor column, WaveDisplay display)
        {
            AppendFlatNorm(column, display.MsNorm);
            AppendFlatNorm(column, display.MkVNorm);
        }

        private static void AppendFlatNorm(ColumnDescriptor column, string value)
        {
            var formatted = FormatNormForClient(value);
            if (formatted == null)
                return;

            column.Item().AlignCenter().Text(formatted).FontSize(10).FontColor(Colors.Grey.Darken1);
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
                column.Item().AlignCenter().Text(_label).FontSize(11).SemiBold();

                if (_display.IsFlat)
                {
                    column.Item().AlignCenter().Text("FLAT").FontSize(24).SemiBold();
                    AppendNorm(column, _display.MsNorm);
                    AppendNorm(column, _display.MkVNorm);
                    return;
                }

                column.Item().Row(row =>
                {
                    row.Spacing(16);

                    row.RelativeItem().Column(msColumn =>
                    {
                        msColumn.Spacing(2);
                        msColumn.Item().AlignCenter().Text(_display.MsValue).FontSize(22).SemiBold();
                        AppendNorm(msColumn, _display.MsNorm);
                    });

                    row.RelativeItem().Column(mkvColumn =>
                    {
                        mkvColumn.Spacing(2);
                        mkvColumn.Item().AlignCenter().Text(_display.MkVValue).FontSize(22).SemiBold();
                        AppendNorm(mkvColumn, _display.MkVNorm);
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
    }

    private sealed class ClientGraphComponent : IComponent
    {
        private readonly string _label;
        private readonly ErgTest _test;
        private readonly EyeData _eye;

        public ClientGraphComponent(string label, ErgTest test, EyeData eye)
        {
            _label = label;
            _test = test;
            _eye = eye;
        }

        public void Compose(IContainer container)
        {
            var graph = TryRenderGraphImage(_test, _eye);

            container.Column(column =>
            {
                column.Spacing(4);
                column.Item().Text(_label).SemiBold();

                if (graph != null)
                {
                    column.Item().Image(graph.Data).FitWidth();
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
        if (value <= 0)
            return null;

        return new string('*', value);
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
            yield return ("a-волна", WaveKind.A);

        yield return ("b-волна", WaveKind.B);
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

    private static string? FormatNormForClient(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "—")
            return null;

        return $"[{value}]";
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
            return "—";

        if (!string.IsNullOrWhiteSpace(info.DeviceName))
            return info.DeviceName;

        if (!string.IsNullOrWhiteSpace(info.ReportName))
            return info.ReportName;

        return "—";
    }

    private static string? GetClientSoftwareVersion(CommonInfo? info)
        => string.IsNullOrWhiteSpace(info?.SoftwareRev) ? null : info!.SoftwareRev;

    private static string FormatClientTestTitle(int index, ErgTest test)
    {
        var name = FormatClientTestName(test.TestName);
        return $"Тест № {index}: {name}";
    }

    private static string FormatClientTestName(string testName)
    {
        if (string.IsNullOrWhiteSpace(testName))
            return "—";

        var normalized = testName.Trim();

        normalized = ReplaceInvariant(normalized, "Flash", "Вспышка");
        normalized = ReplaceInvariant(normalized, "Background", "Фон");
        normalized = ReplaceInvariant(normalized, "Amplitude", "Амплитуда");
        normalized = ReplaceInvariant(normalized, "Test", "Тест");

        normalized = ReplaceInvariant(normalized, "cd*s/m2", "кд·с/м²");
        normalized = ReplaceInvariant(normalized, "cd/m2", "кд/м²");
        normalized = ReplaceInvariant(normalized, "Hz", "Гц");
        normalized = ReplaceInvariant(normalized, " ms", " мс");
        normalized = ReplaceInvariant(normalized, " mV", " мкВ");
        normalized = ReplaceInvariant(normalized, " uV", " мкВ");
        normalized = ReplaceInvariant(normalized, " µV", " мкВ");

        normalized = normalized.Replace(" :", ":", StringComparison.InvariantCulture);
        normalized = normalized.Replace(" ,", ",", StringComparison.InvariantCulture);
        normalized = normalized.Replace("  ", " ", StringComparison.InvariantCulture);

        normalized = normalized.Replace("(", " (", StringComparison.InvariantCulture);
        normalized = normalized.Replace(" )", ")", StringComparison.InvariantCulture);

        normalized = normalized.Replace("Гц", " Гц", StringComparison.InvariantCulture);
        normalized = normalized.Replace("кд·с/м²", " кд·с/м²", StringComparison.InvariantCulture);
        normalized = normalized.Replace("кд/м²", " кд/м²", StringComparison.InvariantCulture);
        while (normalized.Contains("  ", StringComparison.InvariantCulture))
            normalized = normalized.Replace("  ", " ", StringComparison.InvariantCulture);

        return normalized.Trim();
    }

    private static string ReplaceInvariant(string text, string search, string replacement)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(search))
            return text;

        return text.Replace(search, replacement, StringComparison.InvariantCultureIgnoreCase);
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
            const int height = 360;
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
            const float marginTop = 20f;
            const float marginBottom = 60f;
            const float tickInside = 4f;
            const float tickOutside = 6f;

            var chartRect = new SKRect(marginLeft, marginTop, width - marginRight, height - marginBottom);

            double xMin = context.XMin;
            double xMax = context.XMax;
            double yMin = context.YMin;
            double yMax = context.YMax;

            float TransformX(double value) => (float)(chartRect.Left + (value - xMin) / (xMax - xMin) * chartRect.Width);
            float TransformY(double value) => (float)(chartRect.Bottom - (value - yMin) / (yMax - yMin) * chartRect.Height);

            var xStep = DetermineAxisStep(xMin, xMax, test.GraphXValueStep, test.GraphXLineStep);
            var yStep = DetermineAxisStep(yMin, yMax, test.GraphYValueStep, test.GraphYLineStep);
            var xTicks = BuildAxisTicks(xMin, xMax, xStep);
            var yTicks = BuildAxisTicks(yMin, yMax, yStep);

            using (var borderPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.5f, IsAntialias = true, Style = SKPaintStyle.Stroke })
            {
                canvas.DrawRect(chartRect, borderPaint);
            }

            using (var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.8f, IsAntialias = true })
            {
                canvas.DrawLine(chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom, axisPaint);
                canvas.DrawLine(chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom, axisPaint);
            }

            using (var tickPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.2f, IsAntialias = true })
            {
                foreach (var tick in xTicks)
                {
                    var px = TransformX(tick);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;
                    canvas.DrawLine(px, chartRect.Bottom - tickInside, px, chartRect.Bottom + tickOutside, tickPaint);
                }

                foreach (var tick in yTicks)
                {
                    var py = TransformY(tick);
                    if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                        continue;
                    canvas.DrawLine(chartRect.Left - tickOutside, py, chartRect.Left + tickInside, py, tickPaint);
                }
            }

            using (var zeroPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.2f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0) })
            {
                if (yMin < 0 && yMax > 0)
                {
                    var zeroY = TransformY(0);
                    canvas.DrawLine(chartRect.Left, zeroY, chartRect.Right, zeroY, zeroPaint);
                }

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

                using var linePaint = new SKPaint { Color = color, StrokeWidth = 2f, IsAntialias = true, Style = SKPaintStyle.Stroke };
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
                    var px = TransformX(tick);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;
                    var text = FormatAxisValue(tick);
                    var textWidth = labelPaint.MeasureText(text);
                    canvas.DrawText(text, px - textWidth / 2f, chartRect.Bottom + textHeight, labelPaint);
                }

                foreach (var tick in yTicks)
                {
                    var py = TransformY(tick);
                    if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                        continue;
                    var text = FormatAxisValue(tick);
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
        const int height = 360;

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
            const float marginTop = 20f;
            const float marginBottom = 60f;
            const float tickInside = 4f;
            const float tickOutside = 6f;

            var chartRect = new RectangleF(marginLeft, marginTop, width - marginLeft - marginRight, height - marginTop - marginBottom);

            double xMin = context.XMin;
            double xMax = context.XMax;
            double yMin = context.YMin;
            double yMax = context.YMax;

            float TransformX(double value) => (float)(chartRect.Left + (value - xMin) / (xMax - xMin) * chartRect.Width);
            float TransformY(double value) => (float)(chartRect.Bottom - (value - yMin) / (yMax - yMin) * chartRect.Height);

            var xStep = DetermineAxisStep(xMin, xMax, test.GraphXValueStep, test.GraphXLineStep);
            var yStep = DetermineAxisStep(yMin, yMax, test.GraphYValueStep, test.GraphYLineStep);
            var xTicks = BuildAxisTicks(xMin, xMax, xStep);
            var yTicks = BuildAxisTicks(yMin, yMax, yStep);

            using (var borderPen = new Pen(System.Drawing.Color.Black, 1.5f))
            {
                graphics.DrawRectangle(borderPen, chartRect.X, chartRect.Y, chartRect.Width, chartRect.Height);
            }

            using (var axisPen = new Pen(System.Drawing.Color.Black, 1.8f))
            {
                graphics.DrawLine(axisPen, chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom);
                graphics.DrawLine(axisPen, chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom);
            }

            using (var tickPen = new Pen(System.Drawing.Color.Black, 1.2f))
            {
                foreach (var tick in xTicks)
                {
                    var px = TransformX(tick);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;
                    graphics.DrawLine(tickPen, px, chartRect.Bottom - tickInside, px, chartRect.Bottom + tickOutside);
                }

                foreach (var tick in yTicks)
                {
                    var py = TransformY(tick);
                    if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                        continue;
                    graphics.DrawLine(tickPen, chartRect.Left - tickOutside, py, chartRect.Left + tickInside, py);
                }
            }

            using (var dashedPen = new Pen(System.Drawing.Color.Black, 1.2f) { DashPattern = new[] { 4f, 4f } })
            {
                if (yMin < 0 && yMax > 0)
                {
                    var zeroY = TransformY(0);
                    graphics.DrawLine(dashedPen, chartRect.Left, zeroY, chartRect.Right, zeroY);
                }

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

                using var pen = new Pen(color, 2f) { LineJoin = LineJoin.Round };
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
                var px = TransformX(tick);
                if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                    continue;
                var text = FormatAxisValue(tick);
                var size = graphics.MeasureString(text, tickFont);
                graphics.DrawString(text, tickFont, Brushes.Black, px - size.Width / 2f, chartRect.Bottom + size.Height);
            }

            using var tickFormatLeft = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

            foreach (var tick in yTicks)
            {
                var py = TransformY(tick);
                if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                    continue;
                var rect = new RectangleF(chartRect.Left - 16f, py - tickFont.GetHeight(graphics) / 2f, 40f, tickFont.GetHeight(graphics));
                graphics.DrawString(FormatAxisValue(tick), tickFont, Brushes.Black, rect, tickFormatLeft);
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

    private static double DetermineAxisStep
(double min, double max, int valueStep, int lineStep)
    {
        var range = max - min;
        if (range <= 0)
            return 0;

        if (valueStep > 0 && lineStep > 0)
            return valueStep * lineStep;

        if (valueStep > 0)
            return valueStep;

        var roughStep = range / 8.0;
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

    private static double[] BuildAxisTicks(double min, double max, double step)
    {
        if (max <= min)
            return Array.Empty<double>();

        var values = new List<double>();

        if (step > 0)
        {
            double start = Math.Ceiling(min / step) * step;
            int guard = 0;
            for (double value = start; value <= max + 1e-6 && guard < 512; value += step, guard++)
            {
                values.Add(value);
            }
        }
        else
        {
            const int fallbackSegments = 5;
            double range = max - min;
            for (int i = 0; i <= fallbackSegments; i++)
            {
                double value = min + range * i / fallbackSegments;
                values.Add(value);
            }
        }

        values.Add(min);
        values.Add(max);

        return values
            .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
            .Select(v => Math.Round(v, 6))
            .Distinct()
            .Where(v => v >= min - 1e-6 && v <= max + 1e-6)
            .OrderBy(v => v)
            .ToArray();
    }

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
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa }
        };
        props.Append(margin);
        if (gridSpan > 1)
            props.Append(new GridSpan { Val = gridSpan });

        var cell = new TableCell(props);
        cell.Append(CreateParagraph(text ?? string.Empty, fontSizePt: 11, justification: justification));
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
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableLook { Val = "04A0", FirstRow = true, LastRow = false, NoHorizontalBand = false, NoVerticalBand = false },
                new TableCellMarginDefault(
                    new TopMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                    new TableCellLeftMargin { Type = TableWidthValues.Dxa, Width = 120 },
                    new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                    new TableCellRightMargin { Type = TableWidthValues.Dxa, Width = 120 }
                )
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

        if (!string.IsNullOrWhiteSpace(deviceInfo?.ReportName))
        {
            table.Append(new TableRow(CreateInfoCell($"Название протокола: {deviceInfo.ReportName}", JustificationValues.Left)));
        }

        table.Append(new TableRow(CreateInfoCell($"Версия отчета: {GetApplicationVersion()}", JustificationValues.Left)));

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
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableCellMarginDefault(
                    new TopMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                    new TableCellLeftMargin { Type = TableWidthValues.Dxa, Width = 120 },
                    new BottomMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                    new TableCellRightMargin { Type = TableWidthValues.Dxa, Width = 120 }
                ),
                new TableLook { Val = "04A0", FirstRow = true, LastRow = false, NoHorizontalBand = false, NoVerticalBand = false }
            ),
            new TableGrid(new GridColumn { Width = "2500" }, new GridColumn { Width = "2500" })
        );

        var headerRow = new TableRow();
        headerRow.Append(CreateClientHeaderCell(FormatClientTestTitle(index, test), gridSpan: 2));
        table.Append(headerRow);

        var contentRow = new TableRow();
        contentRow.Append(CreateClientEyeCell(mainPart, test, test.RightEye, "Правый глаз", index, "right", ref imageId));
        contentRow.Append(CreateClientEyeCell(mainPart, test, test.LeftEye, "Левый глаз", index, "left", ref imageId));
        table.Append(contentRow);

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
        cell.Append(CreateParagraph(text, fontSizePt: 12, bold: true));
        return cell;
    }

    private static TableCell CreateClientEyeCell(MainDocumentPart mainPart, ErgTest test, EyeData eye, string label, int index, string suffix, ref uint imageId)
    {
        var props = new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top });
        props.Append(new TableCellMargin
        {
            LeftMargin = new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            RightMargin = new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            TopMargin = new TopMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "80", Type = TableWidthUnitValues.Dxa }
        });

        var cell = new TableCell(props);
        var quality = FormatQualityCompact(eye.QualityIndex);
        var labelText = quality != null ? $"{label} {quality}" : label;
        cell.Append(CreateParagraph(labelText, fontSizePt: 11, bold: true, spacingAfter: TwipsFromPoints(4)));

        if (eye.IsFlat)
        {
            var display = BuildWaveDisplay(test, eye, WaveKind.B);
            cell.Append(CreateParagraph("FLAT", fontSizePt: 22, bold: true, justification: JustificationValues.Center, spacingAfter: TwipsFromPoints(4)));
            AppendClientNormParagraph(cell, display.MsNorm);
            AppendClientNormParagraph(cell, display.MkVNorm);
        }
        else
        {
            foreach (var (waveLabel, kind) in GetClientWaveOrder(test))
            {
                var display = BuildWaveDisplay(test, eye, kind);
                if (IsWaveDisplayEmpty(display))
                    continue;

                AppendClientWaveParagraphs(cell, waveLabel, display);
            }
        }

        cell.Append(CreateParagraph("График", fontSizePt: 10, colorHex: "666666", justification: JustificationValues.Center, spacingBefore: TwipsFromPoints(12)));

        var graph = TryRenderGraphImage(test, eye);
        if (graph != null)
        {
            var drawing = CreateImageDrawing(mainPart, graph, $"client-{suffix}-{index}", ref imageId, maxWidthInches: 3.6, maxHeightInches: 3.0);
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
        cell.Append(CreateParagraph(label, fontSizePt: 11, bold: true, justification: JustificationValues.Center, spacingBefore: TwipsFromPoints(6)));

        if (display.IsFlat)
        {
            cell.Append(CreateParagraph("FLAT", fontSizePt: 22, bold: true, justification: JustificationValues.Center));
            AppendClientNormParagraph(cell, display.MsNorm);
            AppendClientNormParagraph(cell, display.MkVNorm);
            return;
        }

        cell.Append(CreateClientWaveValuesTable(display));
    }

    private static Table CreateClientWaveValuesTable(WaveDisplay display)
    {
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
                ),
                new TableCellMarginDefault(
                    new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                    new TableCellLeftMargin { Type = TableWidthValues.Dxa, Width = 80 },
                    new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                    new TableCellRightMargin { Type = TableWidthValues.Dxa, Width = 80 }
                )
            ),
            new TableGrid(new GridColumn { Width = "2400" }, new GridColumn { Width = "2400" })
        );

        var row = new TableRow();
        row.Append(CreateClientWaveValueCell(display.MsValue, display.MsNorm));
        row.Append(CreateClientWaveValueCell(display.MkVValue, display.MkVNorm));
        table.Append(row);

        return table;
    }

    private static TableCell CreateClientWaveValueCell(string value, string norm)
    {
        var cell = new TableCell
        {
            TableCellProperties = new TableCellProperties(
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
            )
        };

        cell.Append(CreateParagraph(value, fontSizePt: 22, bold: true, justification: JustificationValues.Center));

        var formatted = FormatNormForClient(norm);
        if (formatted != null)
        {
            cell.Append(CreateParagraph(formatted, fontSizePt: 9, colorHex: "666666", justification: JustificationValues.Center));
        }

        return cell;
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

    private static Paragraph CreateParagraph(string text, double fontSizePt = 11, bool bold = false, JustificationValues? justification = null, int spacingBefore = 0, int spacingAfter = 0, bool italic = false, string? colorHex = null)
    {
        var paragraph = new Paragraph();
        var paragraphProps = new ParagraphProperties();
        if (justification.HasValue)
            paragraphProps.Append(new Justification { Val = justification.Value });
        if (spacingBefore > 0 || spacingAfter > 0)
        {
            paragraphProps.Append(new SpacingBetweenLines
            {
                Before = spacingBefore > 0 ? spacingBefore.ToString(CultureInfo.InvariantCulture) : null,
                After = spacingAfter > 0 ? spacingAfter.ToString(CultureInfo.InvariantCulture) : null
            });
        }
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

    private static int TwipsFromPoints(double points) => (int)Math.Round(points * 20);

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
