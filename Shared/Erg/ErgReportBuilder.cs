using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

                column.Item().Text("Первые 10 точек (правый глаз, график 1): " + BuildGraphPreview(_test.RightEye.Graphs, _test.GraphNumPoints)).FontSize(10);
                column.Item().Text("Первые 10 точек (левый глаз, график 1): " + BuildGraphPreview(_test.LeftEye.Graphs, _test.GraphNumPoints)).FontSize(10);
            });
        }
    }

    private sealed record TableRowData(string Caption, string Right, string Left);

    private sealed record GraphImage(byte[] Data, int Width, int Height)
    {
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
        var graphs = eye.Graphs;
        if (graphs == null || graphs.Length == 0)
            return null;

        int curves = Math.Clamp(eye.GraphCount, 0, graphs.Length);
        if (curves <= 0)
            return null;

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
            return null;

        const int width = 900;
        const int height = 360;
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        const float marginLeft = 80f;
        const float marginRight = 30f;
        const float marginTop = 20f;
        const float marginBottom = 60f;

        var chartRect = new SKRect(marginLeft, marginTop, width - marginRight, height - marginBottom);

        double xMin = test.GraphXScaleMin;
        double xMax = test.GraphXScaleMax;
        if (xMax <= xMin)
            xMax = xMin + 1;

        double yMin = test.GraphYScaleMin;
        double yMax = test.GraphYScaleMax;
        if (yMax <= yMin)
            yMax = yMin + 1;

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

        for (int graphIndex = 0; graphIndex < curves; graphIndex++)
        {
            var samples = graphs[graphIndex];
            if (samples == null || samples.Length == 0)
                continue;

            int count = test.GraphNumPoints > 1 ? Math.Min(test.GraphNumPoints, samples.Length) : samples.Length;
            if (count < 2)
                continue;

            using var path = new SKPath();
            bool hasPoint = false;
            for (int point = 0; point < count; point++)
            {
                double xValue = count == 1 ? xMin : xMin + (xMax - xMin) * point / (count - 1);
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

            var style = graphIndex < test.GraphStyles.Length ? test.GraphStyles[graphIndex] : null;
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
            float centerOffset = textHeight / 2f - metrics.Descent;

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
                    var textWidth = labelPaint.MeasureText(text);
                    canvas.DrawText(text, chartRect.Left - textWidth - 8, py + centerOffset, labelPaint);
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

    private static double DetermineAxisStep(double min, double max, int valueStep, int lineStep)
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

        body.Append(CreateParagraph("Первые 10 точек (правый глаз, график 1): " + BuildGraphPreview(test.RightEye.Graphs, test.GraphNumPoints), fontSizePt: 10));
        body.Append(CreateParagraph("Первые 10 точек (левый глаз, график 1): " + BuildGraphPreview(test.LeftEye.Graphs, test.GraphNumPoints), fontSizePt: 10));
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
