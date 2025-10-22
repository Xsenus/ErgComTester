using System;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        Document.Create(container =>
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
                        row.RelativeItem().AlignRight().Text($"Животное: {patient.Animal}");
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

                column.Item().Component(new GraphSummaryComponent(_test));
            });
        }

        private static string FormatRange(byte min, byte max)
        {
            if (min == 255 && max == 255) return "—";
            if (min == 255) return $"≤ {max}";
            if (max == 255) return $"≥ {min}";
            return $"{min}…{max}";
        }

        private static string FormatRange(uint min, uint max)
        {
            bool minMissing = min == 65535 || min == uint.MaxValue;
            bool maxMissing = max == 65535 || max == uint.MaxValue;
            if (minMissing && maxMissing) return "—";
            if (minMissing) return $"≤ {max}";
            if (maxMissing) return $"≥ {min}";
            return $"{min}…{max}";
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

                AddRow(table, "FLAT", BoolText(_test.RightEye.IsFlat), BoolText(_test.LeftEye.IsFlat));
                AddRow(table, "QI", Quality(_test.RightEye.QualityIndex), Quality(_test.LeftEye.QualityIndex));
                AddRow(table, "Маркер a", FormatMarker(_test.RightEye.AWaveMarker), FormatMarker(_test.LeftEye.AWaveMarker));
                AddRow(table, "Маркер b", FormatMarker(_test.RightEye.BWaveMarker), FormatMarker(_test.LeftEye.BWaveMarker));

                var maxValues = Math.Max(_test.RightEye.ValueCount, _test.LeftEye.ValueCount);
                for (int i = 0; i < maxValues; i++)
                {
                    var caption = $"Замер #{i + 1}";
                    var right = FormatMeasurement(_test.RightEye, i);
                    var left = FormatMeasurement(_test.LeftEye, i);
                    AddRow(table, caption, right, left);
                }
            });
        }

        private static string BoolText(bool value) => value ? "Да" : "Нет";

        private static string Quality(byte quality)
        {
            quality = (byte)Math.Clamp(quality, (byte)0, (byte)3);
            return new string('★', quality) + new string('☆', 3 - quality);
        }

        private static string FormatMarker(byte marker)
        {
            if (marker == 255) return "—";
            return $"{marker} мс";
        }

        private static string FormatMeasurement(EyeData eye, int index)
        {
            if (index >= eye.AWaveMs.Length || index >= eye.AWaveMkV.Length || index >= eye.BWaveMs.Length || index >= eye.BWaveMkV.Length)
                return string.Empty;

            var aMs = eye.AWaveMs[index];
            var aMkV = eye.AWaveMkV[index];
            var bMs = eye.BWaveMs[index];
            var bMkV = eye.BWaveMkV[index];

            string FormatMs(byte value) => value == 255 ? "—" : $"{value} мс";
            string FormatMkV(uint value) => (value == 65535 || value == uint.MaxValue) ? "—" : $"{value} мкВ";

            return $"a: {FormatMs(aMs)}, {FormatMkV(aMkV)}" +
                   $"b: {FormatMs(bMs)}, {FormatMkV(bMkV)}";
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

    private sealed class GraphSummaryComponent : IComponent
    {
        private readonly ErgTest _test;

        public GraphSummaryComponent(ErgTest test) => _test = test;

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(4);
                column.Item().Text("Графические данные").SemiBold();
                column.Item().Text($"Правый глаз: {_test.RightEye.GraphCount} граф., левый глаз: {_test.LeftEye.GraphCount} граф.");

                var styleDescriptions = _test.GraphStyles
                    .Where(s => s.Index < 6)
                    .Select(s => $"{s.Index + 1}: RGB({s.Red},{s.Green},{s.Blue}){(s.Dotted ? ", пунктир" : string.Empty)}")
                    .ToArray();

                if (styleDescriptions.Length > 0)
                {
                    column.Item().Text("Стили графиков: " + string.Join("; ", styleDescriptions)).FontSize(10);
                }

                column.Item().Text("Первые 10 точек (правый глаз, график 1): " + Preview(_test.RightEye.Graphs));
                column.Item().Text("Первые 10 точек (левый глаз, график 1): " + Preview(_test.LeftEye.Graphs));
            });
        }

        private static string Preview(int[][] graphs)
        {
            if (graphs == null || graphs.Length == 0 || graphs[0] == null)
                return "нет данных";

            return string.Join(", ", graphs[0].Take(10));
        }
    }
}
