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

    public static void BuildPatientReport(ErgPatient patient, string pdfPath, CommonInfo? deviceInfo = null, string? clinicName = null, string? rawFilePath = null)
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
            BuildPatientReportLegacyPdf(patient, pdfPath, deviceInfo, clinicName, rawFilePath);
        }
        else
        {
            BuildPatientReportQuestPdf(patient, pdfPath, deviceInfo, clinicName, rawFilePath);
        }
    }

    private static void BuildPatientReportQuestPdf(ErgPatient patient, string pdfPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath)
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

    private static void BuildPatientReportLegacyPdf(ErgPatient patient, string pdfPath, CommonInfo? deviceInfo, string? clinicName, string? rawFilePath)
    {
        using var renderer = new LegacyPdfRenderer(patient, pdfPath, deviceInfo, clinicName, rawFilePath);
        renderer.Build();
    }

    public static void BuildPatientWordReport(ErgPatient patient, string docxPath, CommonInfo? deviceInfo = null, string? clinicName = null, string? rawFilePath = null)
    {
        if (patient == null) throw new ArgumentNullException(nameof(patient));
        if (string.IsNullOrWhiteSpace(docxPath)) throw new ArgumentNullException(nameof(docxPath));

        var directory = Path.GetDirectoryName(docxPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

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
        return $"{min}…{max}";
    }

    private static string FormatRange(uint? min, uint? max)
    {
        if (!min.HasValue && !max.HasValue) return "—";
        if (!min.HasValue) return $"≤ {max}";
        if (!max.HasValue) return $"≥ {min}";
        return $"{min}…{max}";
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

            var chartRect = new SKRect(marginLeft, marginTop, width - marginRight, height - marginBottom);

            double xMin = context.XMin;
            double xMax = context.XMax;
            double yMin = context.YMin;
            double yMax = context.YMax;

            float TransformX(double value) => (float)(chartRect.Left + (value - xMin) / (xMax - xMin) * chartRect.Width);
            float TransformY(double value) => (float)(chartRect.Bottom - (value - yMin) / (yMax - yMin) * chartRect.Height);

            using (var backgroundPaint = new SKPaint { Color = new SKColor(248, 248, 248), Style = SKPaintStyle.Fill })
            {
                canvas.DrawRect(chartRect, backgroundPaint);
            }

            var xStep = DetermineAxisStep(xMin, xMax, test.GraphXValueStep, test.GraphXLineStep);
            var yStep = DetermineAxisStep(yMin, yMax, test.GraphYValueStep, test.GraphYLineStep);

            using (var gridPaint = new SKPaint { Color = new SKColor(215, 215, 215), StrokeWidth = 1f, IsAntialias = true })
            {
                gridPaint.PathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0);

                if (xStep > 0)
                {
                    for (double x = Math.Ceiling(xMin / xStep) * xStep, count = 0; x <= xMax + 1e-6 && count < 512; x += xStep, count++)
                    {
                        var px = TransformX(x);
                        if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                            continue;
                        canvas.DrawLine(px, chartRect.Top, px, chartRect.Bottom, gridPaint);
                    }
                }

                if (yStep > 0)
                {
                    for (double y = Math.Ceiling(yMin / yStep) * yStep, count = 0; y <= yMax + 1e-6 && count < 512; y += yStep, count++)
                    {
                        var py = TransformY(y);
                        if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                            continue;
                        canvas.DrawLine(chartRect.Left, py, chartRect.Right, py, gridPaint);
                    }
                }
            }

            using (var axisPaint = new SKPaint { Color = new SKColor(120, 120, 120), StrokeWidth = 1.5f, IsAntialias = true })
            {
                canvas.DrawLine(chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom, axisPaint);
                canvas.DrawLine(chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom, axisPaint);
            }

            if (yMin < 0 && yMax > 0)
            {
                using var zeroPaint = new SKPaint { Color = new SKColor(180, 180, 180), StrokeWidth = 1f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 6f, 6f }, 0) };
                var zeroY = TransformY(0);
                canvas.DrawLine(chartRect.Left, zeroY, chartRect.Right, zeroY, zeroPaint);
            }

            if (xMin < 0 && xMax > 0)
            {
                using var zeroPaint = new SKPaint { Color = new SKColor(180, 180, 180), StrokeWidth = 1f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 6f, 6f }, 0) };
                var zeroX = TransformX(0);
                canvas.DrawLine(zeroX, chartRect.Top, zeroX, chartRect.Bottom, zeroPaint);
            }

            if (test.GraphFlashPosition >= xMin && test.GraphFlashPosition <= xMax)
            {
                using var flashPaint = new SKPaint { Color = new SKColor(220, 0, 0), StrokeWidth = 1.5f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 6f, 6f }, 0) };
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

            using (var labelPaint = new SKPaint { Color = SKColors.Black, TextSize = 16f, IsAntialias = true })
            {
                var metrics = labelPaint.FontMetrics;
                float textHeight = metrics.Descent - metrics.Ascent;

                if (xStep > 0)
                {
                    for (double x = Math.Ceiling(xMin / xStep) * xStep, count = 0; x <= xMax + 1e-6 && count < 512; x += xStep, count++)
                    {
                        var px = TransformX(x);
                        if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                            continue;
                        var text = FormatAxisValue(x);
                        var textWidth = labelPaint.MeasureText(text);
                        canvas.DrawText(text, px - textWidth / 2f, chartRect.Bottom + textHeight, labelPaint);
                    }
                }

                if (yStep > 0)
                {
                    for (double y = Math.Ceiling(yMin / yStep) * yStep, count = 0; y <= yMax + 1e-6 && count < 512; y += yStep, count++)
                    {
                        var py = TransformY(y);
                        if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                            continue;
                        var text = FormatAxisValue(y);
                        canvas.DrawText(text, chartRect.Left - 10 - labelPaint.MeasureText(text), py + textHeight / 3f, labelPaint);
                    }
                }
            }

            using (var titlePaint = new SKPaint { Color = SKColors.Black, TextSize = 18f, IsAntialias = true })
            {
                var xLabel = "Время, мс";
                var xWidth = titlePaint.MeasureText(xLabel);
                var midX = (chartRect.Left + chartRect.Right) / 2f;
                canvas.DrawText(xLabel, midX - xWidth / 2f, height - 12, titlePaint);

                var yLabel = "Амплитуда, мкВ";
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

            var chartRect = new RectangleF(marginLeft, marginTop, width - marginLeft - marginRight, height - marginTop - marginBottom);

            double xMin = context.XMin;
            double xMax = context.XMax;
            double yMin = context.YMin;
            double yMax = context.YMax;

            float TransformX(double value) => (float)(chartRect.Left + (value - xMin) / (xMax - xMin) * chartRect.Width);
            float TransformY(double value) => (float)(chartRect.Bottom - (value - yMin) / (yMax - yMin) * chartRect.Height);

            using (var backgroundBrush = new SolidBrush(System.Drawing.Color.FromArgb(248, 248, 248)))
            {
                graphics.FillRectangle(backgroundBrush, chartRect);
            }

            var xStep = DetermineAxisStep(xMin, xMax, test.GraphXValueStep, test.GraphXLineStep);
            var yStep = DetermineAxisStep(yMin, yMax, test.GraphYValueStep, test.GraphYLineStep);

            using (var gridPen = new Pen(System.Drawing.Color.FromArgb(215, 215, 215), 1f) { DashPattern = new[] { 4f, 4f } })
            {
                if (xStep > 0)
                {
                    for (double x = Math.Ceiling(xMin / xStep) * xStep, count = 0; x <= xMax + 1e-6 && count < 512; x += xStep, count++)
                    {
                        var px = TransformX(x);
                        if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                            continue;
                        graphics.DrawLine(gridPen, px, chartRect.Top, px, chartRect.Bottom);
                    }
                }

                if (yStep > 0)
                {
                    for (double y = Math.Ceiling(yMin / yStep) * yStep, count = 0; y <= yMax + 1e-6 && count < 512; y += yStep, count++)
                    {
                        var py = TransformY(y);
                        if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                            continue;
                        graphics.DrawLine(gridPen, chartRect.Left, py, chartRect.Right, py);
                    }
                }
            }

            using (var axisPen = new Pen(System.Drawing.Color.FromArgb(120, 120, 120), 1.5f))
            {
                graphics.DrawLine(axisPen, chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom);
                graphics.DrawLine(axisPen, chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom);
            }

            using (var dashedPen = new Pen(System.Drawing.Color.FromArgb(180, 180, 180), 1f) { DashPattern = new[] { 6f, 6f } })
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
                using var flashPen = new Pen(System.Drawing.Color.FromArgb(220, 0, 0), 1.5f) { DashPattern = new[] { 6f, 6f } };
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

            using var tickFont = new System.Drawing.Font("Arial", 10f, FontStyle.Regular, GraphicsUnit.Point);

            if (xStep > 0)
            {
                for (double x = Math.Ceiling(xMin / xStep) * xStep, count = 0; x <= xMax + 1e-6 && count < 512; x += xStep, count++)
                {
                    var px = TransformX(x);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;
                    var text = FormatAxisValue(x);
                    var size = graphics.MeasureString(text, tickFont);
                    graphics.DrawString(text, tickFont, Brushes.Black, px - size.Width / 2f, chartRect.Bottom + size.Height / 4f);
                }
            }

            using var tickFormatLeft = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

            if (yStep > 0)
            {
                for (double y = Math.Ceiling(yMin / yStep) * yStep, count = 0; y <= yMax + 1e-6 && count < 512; y += yStep, count++)
                {
                    var py = TransformY(y);
                    if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                        continue;
                    var rect = new RectangleF(chartRect.Left - 12f, py - tickFont.GetHeight(graphics) / 2f, 40f, tickFont.GetHeight(graphics));
                    graphics.DrawString(FormatAxisValue(y), tickFont, Brushes.Black, rect, tickFormatLeft);
                }
            }

            using var axisTitleFont = new System.Drawing.Font("Arial", 12f, FontStyle.Regular, GraphicsUnit.Point);
            var xLabelSize = graphics.MeasureString("Время, мс", axisTitleFont);
            graphics.DrawString("Время, мс", axisTitleFont, Brushes.Black, chartRect.Left + (chartRect.Width - xLabelSize.Width) / 2f, height - xLabelSize.Height - 6f);

            graphics.TranslateTransform(20f, chartRect.Top + chartRect.Height / 2f);
            graphics.RotateTransform(-90f);
            var yLabelSize = graphics.MeasureString("Амплитуда, мкВ", axisTitleFont);
            graphics.DrawString("Амплитуда, мкВ", axisTitleFont, Brushes.Black, -yLabelSize.Width / 2f, -yLabelSize.Height / 2f);
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
