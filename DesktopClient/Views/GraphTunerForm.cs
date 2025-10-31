using System;
using ErgData;
using MicroluxErgConnect.Infrastructure;
using System.Text.Json;

namespace MicroluxErgConnect.Views
{
    public partial class GraphTunerForm : Form
    {
        private readonly ErgTest _test;
        private readonly EyeData _eye;

        private readonly PictureBox _preview = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White
        };

        private readonly Panel _right = new Panel
        {
            Dock = DockStyle.Right,
            Width = 420,
            BackColor = Color.FromArgb(248, 248, 248)
        };

        // Верхняя панель действий
        private readonly ToolStrip _tool = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            Dock = DockStyle.Top,
            BackColor = Color.White
        };

        private readonly ToolStripButton _btnReset = new("Восстановить") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        private readonly ToolStripButton _btnExportPreset = new("Экспорт в файл") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        private readonly ToolStripButton _btnImportPreset = new("Импорт из файла") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        private readonly ToolStripButton _btnSaveToSettings = new("Сохранить в настройки") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        private readonly ToolStripButton _btnLoadFromSettings = new("Загрузить из настроек") { DisplayStyle = ToolStripItemDisplayStyle.Text };

        private readonly FlowLayoutPanel _flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(10)
        };

        private readonly NumericUpDown nudMajorMm = MakeNud(0.5m, 10m, 0.1m, 3m);
        private readonly NumericUpDown nudMinorMm = MakeNud(0.5m, 10m, 0.1m, 1.5m);
        private readonly NumericUpDown nudAxisPx = MakeNud(0.5m, 12m, 0.1m, 3.6m);
        private readonly NumericUpDown nudTickPx = MakeNud(0.5m, 12m, 0.1m, 3.0m);
        private readonly NumericUpDown nudCurvePx = MakeNud(0.5m, 12m, 0.1m, 8.0m);
        private readonly NumericUpDown nudLabelPt = MakeNud(6m, 24m, 0.5m, 7m);
        private readonly NumericUpDown nudUnitsPt = MakeNud(6m, 24m, 0.5m, 6.5m);
        private readonly NumericUpDown nudMarginL = MakeNud(0m, 400m, 1m, 165m);
        private readonly NumericUpDown nudMarginR = MakeNud(0m, 200m, 1m, 20m);
        private readonly NumericUpDown nudMarginT = MakeNud(0m, 200m, 1m, 5m);
        private readonly NumericUpDown nudMarginB = MakeNud(0m, 300m, 1m, 120m);
        private readonly NumericUpDown nudGapH = MakeNud(0m, 200m, 1m, 20m);
        private readonly NumericUpDown nudGapV = MakeNud(0m, 200m, 1m, 20m);
        private readonly NumericUpDown nudXDigitsOff = MakeNud(0m, 40m, 1m, 18m);
        private readonly NumericUpDown nudXUnitsGap = MakeNud(0m, 40m, 1m, 10m);
        private readonly NumericUpDown nudMinGapX = MakeNud(0m, 30m, 1m, 8m);
        private readonly NumericUpDown nudMinGapY = MakeNud(0m, 30m, 1m, 4m);
        private readonly NumericUpDown nudYPad = MakeNud(0m, 60m, 1m, 16m);
        private readonly NumericUpDown nudYUnitsGap = MakeNud(0m, 60m, 1m, 22m);
        private readonly NumericUpDown nudYUnitsFb = MakeNud(0m, 120m, 1m, 70m);
        private readonly NumericUpDown nudExtremumPx = MakeNud(0.1m, 12m, 0.1m, 1.2m);
        private readonly NumericUpDown nudGridPx = MakeNud(0.1m, 6m, 0.1m, 1.0m);
        private readonly NumericUpDown nudDottedDash = MakeNud(0.1m, 20m, 0.1m, 4.0m);
        private readonly NumericUpDown nudDottedGap = MakeNud(0.1m, 20m, 0.1m, 3.0m);

        // Debounce перерисовки
        private readonly System.Windows.Forms.Timer _debounce = new System.Windows.Forms.Timer { Interval = 120 };

        // DTO для сохранения пресета
        private sealed class PresetDto
        {
            public float MajorTickLenMm { get; set; }

            public float MinorTickLenMm { get; set; }

            public float AxisThicknessPx { get; set; }

            public float TickThicknessPx { get; set; }

            public float CurveThicknessPx { get; set; }

            public float ExtremumThicknessPx { get; set; }

            public float GridThicknessPx { get; set; }

            public float DottedDashLengthPx { get; set; }

            public float DottedGapLengthPx { get; set; }

            public float LabelFontPt { get; set; }

            public float UnitsFontPt { get; set; }

            public float MarginLeft { get; set; }

            public float MarginRight { get; set; }

            public float MarginTop { get; set; }

            public float MarginBottom { get; set; }

            public float AxisGapHorizontal { get; set; }

            public float AxisGapVertical { get; set; }

            public float XDigitsOffsetPx { get; set; }

            public float XUnitsGapPx { get; set; }

            public float MinLabelGapXPx { get; set; }

            public float MinLabelGapYPx { get; set; }

            public float YDigitsLeftPadPx { get; set; }

            public float YUnitsGapFromNumbersPx { get; set; }

            public float YUnitsFallbackFromAxisPx { get; set; }
        }

        public GraphTunerForm(ErgTest test, EyeData eye)
        {
            InitializeComponent();

            Text = "Graph Tuner";
            Width = 1200;
            Height = 800;
            DoubleBuffered = true;

            _test = test;
            _eye = eye;

            Controls.Add(_preview);
            Controls.Add(_right);

            // Toolstrip
            _tool.Items.AddRange(new ToolStripItem[] {
                _btnReset, new ToolStripSeparator(),
                _btnExportPreset, _btnImportPreset,
                _btnSaveToSettings, _btnLoadFromSettings
            });

            _tool.ShowItemToolTips = true;
            _btnExportPreset.ToolTipText = "Сохранить текущие параметры графика в JSON-файл";
            _btnImportPreset.ToolTipText = "Загрузить параметры графика из JSON-файла";
            _btnSaveToSettings.ToolTipText = "Записать текущий профиль в settings.json (будет применяться при старте)";
            _btnLoadFromSettings.ToolTipText = "Применить профиль из settings.json к текущему сеансу";

            // Делаем тулбар "многострочным"
            _tool.LayoutStyle = ToolStripLayoutStyle.Flow; // вместо HorizontalStackWithOverflow
            _tool.CanOverflow = false;                     // не уводим в ">>", лучше переносим
            _tool.AutoSize = true;                         // высота подтянется под 2 строки
            _tool.Padding = new Padding(6, 4, 6, 4);

            // Немного красоты: равномерные отступы между кнопками
            foreach (ToolStripItem it in _tool.Items)
            {
                it.Overflow = ToolStripItemOverflow.Never; // жёстко запрещаем overflow
                it.Margin = new Padding(0, 0, 6, 4);       // правый и нижний отступ
            }


            _right.Controls.Add(_flow);
            _right.Controls.Add(_tool);

            _flow.SuspendLayout();

            // Группы настроек
            _flow.Controls.Add(MakeGroup("Ticks (мм)", Row("Major", nudMajorMm, "mm"), Row("Minor", nudMinorMm, "mm")));
            _flow.Controls
                .Add(
                    MakeGroup(
                        "Thickness (px)",
                        Row("Axis", nudAxisPx, "px"),
                        Row("Tick", nudTickPx, "px"),
                        Row("Curve", nudCurvePx, "px"),
                        Row("Extremum", nudExtremumPx, "px"),
                        Row("Grid", nudGridPx, "px")));
            _flow.Controls.Add(MakeGroup("Dotted pattern (px)", Row("Dash length", nudDottedDash, "px"), Row("Gap length", nudDottedGap, "px")));
            _flow.Controls.Add(MakeGroup("Fonts (pt)", Row("Labels", nudLabelPt, "pt"), Row("Units", nudUnitsPt, "pt")));
            _flow.Controls
                .Add(
                    MakeGroup(
                        "Margins (px)",
                        Row("Left", nudMarginL, "px"),
                        Row("Right", nudMarginR, "px"),
                        Row("Top", nudMarginT, "px"),
                        Row("Bottom", nudMarginB, "px")));
            _flow.Controls.Add(MakeGroup("Axis gaps (px)", Row("H gap", nudGapH, "px"), Row("V gap", nudGapV, "px")));
            _flow.Controls
                .Add(
                    MakeGroup(
                        "Labels layout (px)",
                        Row("X digits offset", nudXDigitsOff, "px"),
                        Row("X units gap", nudXUnitsGap, "px"),
                        Row("Min gap X", nudMinGapX, "px"),
                        Row("Min gap Y", nudMinGapY, "px"),
                        Row("Y digits left pad", nudYPad, "px"),
                        Row("Y units gap", nudYUnitsGap, "px"),
                        Row("Y units fallback", nudYUnitsFb, "px")));

            _flow.ResumeLayout(performLayout: true);

            // Подсказки
            var tip = new ToolTip { InitialDelay = 200, ReshowDelay = 100, AutoPopDelay = 8000, ShowAlways = true };
            tip.SetToolTip(nudMajorMm, "Длина длинной риски (мм)");
            tip.SetToolTip(nudMinorMm, "Длина короткой риски (мм)");
            tip.SetToolTip(nudAxisPx, "Толщина линий осей (px)");
            tip.SetToolTip(nudTickPx, "Толщина рисок (px)");
            tip.SetToolTip(nudCurvePx, "Толщина кривых (px)");
            tip.SetToolTip(nudLabelPt, "Размер шрифта цифр (pt)");
            tip.SetToolTip(nudUnitsPt, "Размер шрифта единиц (pt)");
            tip.SetToolTip(nudMarginL, "Левое поле (px)");
            tip.SetToolTip(nudMarginR, "Правое поле (px)");
            tip.SetToolTip(nudMarginT, "Верхнее поле (px)");
            tip.SetToolTip(nudMarginB, "Нижнее поле (px)");
            tip.SetToolTip(nudGapH, "Горизонтальный зазор оси Y (px)");
            tip.SetToolTip(nudGapV, "Вертикальный зазор оси X (px)");
            tip.SetToolTip(nudXDigitsOff, "Смещение цифр X ниже рисок (px)");
            tip.SetToolTip(nudXUnitsGap, "Зазор между цифрами X и 'ms' (px)");
            tip.SetToolTip(nudMinGapX, "Мин. зазор между подписями X (px)");
            tip.SetToolTip(nudMinGapY, "Мин. зазор между подписями Y (px)");
            tip.SetToolTip(nudYPad, "Отступ чисел Y от оси (px)");
            tip.SetToolTip(nudYUnitsGap, "Зазор 'µV' от чисел (px)");
            tip.SetToolTip(nudYUnitsFb, "Фоллбек-отступ 'µV' от оси (px)");
            tip.SetToolTip(nudExtremumPx, "Толщина отметок экстремумов (px)");
            tip.SetToolTip(nudGridPx, "Толщина линий сетки (px)");
            tip.SetToolTip(nudDottedDash, "Длина штриха пунктира (px)");
            tip.SetToolTip(nudDottedGap, "Зазор между штрихами пунктира (px)");

            // Адаптация ширины групп
            _flow.SizeChanged += (_, __) => ResizeGroups();
            ResizeGroups();

            // Значения из GraphOptions → NUD
            LoadFromOptions();

            // Подписки
            foreach(var n in GetAll<NumericUpDown>(_flow))
            {
                n.ValueChanged += OnAnyChanged;
                n.KeyDown += OnNudKeyDown;
                n.KeyUp += OnNudKeyUp;
            }

            _btnReset.Click += (_, __) =>
            {
                ResetToDefaults();
                Redraw();
            };
            _btnExportPreset.Click += (_, __) => SavePreset();
            _btnImportPreset.Click += (_, __) =>
            {
                if(LoadPreset())
                    Redraw();
            };

            _btnSaveToSettings.Click += async (_, __) =>
            {
                try
                {
                    // Текущие значения уже перенесены в ErgReportBuilder.GraphOptions в Redraw()
                    await AppServices.PersistGraphOptionsToSettingsAsync().ConfigureAwait(false);
                    MessageBox.Show(
                        this,
                        "Сохранено в настройки приложения.",
                        "Graph Tuner",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                } catch(Exception ex)
                {
                    MessageBox.Show(
                        this,
                        "Не удалось сохранить настройки: " + ex.Message,
                        "Graph Tuner",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            _btnLoadFromSettings.Click += (_, __) =>
            {
                try
                {
                    AppServices.ApplyGraphOptionsFromSettings();
                    LoadFromOptions();
                    Redraw();
                } catch(Exception ex)
                {
                    MessageBox.Show(
                        this,
                        "Не удалось загрузить настройки: " + ex.Message,
                        "Graph Tuner",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            _debounce.Tick += (_, __) =>
            {
                _debounce.Stop();
                Redraw();
            };

            // Первичная отрисовка
            ResizeGroups();
            Redraw();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            var img = _preview.Image;
            _preview.Image = null;
            img?.Dispose();

            try
            {
                // Откатываем все временные правки формы.
                // Рабочим профилем остаётся то, что в settings.json.
                AppServices.ApplyGraphOptionsFromSettings();
            }
            catch
            {
                // best-effort: не мешаем закрытию формы
            }

            base.OnFormClosed(e);
        }

        // ---------- UI helpers ----------

        private static IEnumerable<T> GetAll<T>(Control parent) where T : Control
        {
            foreach(Control c in parent.Controls)
            {
                if(c is T t)
                    yield return t;
                foreach(var child in GetAll<T>(c))
                    yield return child;
            }
        }

        private static NumericUpDown MakeNud(decimal min, decimal max, decimal inc, decimal val)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Increment = inc,
                DecimalPlaces = Math.Max(0, (decimal.GetBits(inc)[3] >> 16)),
                Value = val,
                Width = 100,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
        }

        private static (Label lbl, Control ctrl, Label unit) Row(string label, Control ctrl, string units)
        {
            var l = new Label
            {
                Text = label,
                AutoSize = true,
                Margin = new Padding(3, 6, 3, 3),
                Anchor = AnchorStyles.Left
            };
            var u = new Label
            {
                Text = units,
                AutoSize = true,
                Margin = new Padding(6, 6, 3, 3),
                ForeColor = Color.DimGray,
                Anchor = AnchorStyles.Left
            };
            return (l, ctrl, u);
        }

        // ВАРИАНТ А: новый — под тройки (Label, Control, UnitsLabel)
        private Control MakeGroup(string title, params (Label lbl, Control ctrl, Label unit)[] rows)
        {
            var grp = new GroupBox
            {
                Text = title,
                AutoSize = true,                            // высота под содержимое
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.FromArgb(252, 252, 252)
            };

            var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f)); // Label
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // Control
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44f));  // Units

            foreach (var (lbl, ctrl, unit) in rows)
            {
                table.Controls.Add(lbl, 0, table.RowCount);
                table.Controls.Add(ctrl, 1, table.RowCount);
                table.Controls.Add(unit, 2, table.RowCount);
                table.RowCount++;
            }

            grp.Controls.Add(table);
            return grp;
        }

        private static string UnitsByTitle(string title)
        {
            var t = title.ToLowerInvariant();
            if(t.Contains("(мм)") || t.Contains("(mm)") || t.Contains("ticks"))
                return "mm";
            if(t.Contains("(pt)") || t.Contains("fonts"))
                return "pt";
            return "px";
        }

        private void ResizeGroups()
        {
            int w = Math.Max(120, _flow.ClientSize.Width - _flow.Padding.Horizontal);
            foreach(var gb in _flow.Controls.OfType<GroupBox>())
            {
                // фиксируем ровно эту ширину, высота — авто
                gb.MinimumSize = new Size(w, 0);
                gb.MaximumSize = new Size(w, 0);  // 0 по высоте = без ограничения
                gb.Width = w;
            }
        }

        private static decimal Clamp(NumericUpDown nud, decimal v)
        {
            if(v < nud.Minimum)
                return nud.Minimum;
            if(v > nud.Maximum)
                return nud.Maximum;
            return v;
        }

        // Ускорение ввода: Shift = x5, Ctrl = x0.2
        private void OnNudKeyDown(object? sender, KeyEventArgs e)
        {
            if(sender is not NumericUpDown nud)
                return;
            if(e.Shift)
                nud.Increment *= 5;
            if(e.Control)
                nud.Increment /= 5;
        }

        private void OnNudKeyUp(object? sender, KeyEventArgs e)
        {
            if(sender is not NumericUpDown nud)
                return;
            if(nud == nudLabelPt || nud == nudUnitsPt)
                nud.Increment = 0.5m;
            else if(nud == nudMajorMm ||
                nud == nudMinorMm ||
                nud == nudAxisPx ||
                nud == nudTickPx ||
                nud == nudCurvePx ||
                nud == nudExtremumPx ||
                nud == nudGridPx ||
                nud == nudDottedDash ||
                nud == nudDottedGap)
                nud.Increment = 0.1m;
            else
                nud.Increment = 1m;
        }

        // ---------- Опции / пресеты ----------

        private void LoadFromOptions()
        {
            var o = ErgReportBuilder.GraphOptions;

            nudMajorMm.Value = Clamp(nudMajorMm, (decimal)o.MajorTickLenMm);
            nudMinorMm.Value = Clamp(nudMinorMm, (decimal)o.MinorTickLenMm);
            nudAxisPx.Value = Clamp(nudAxisPx, (decimal)o.AxisThicknessPx);
            nudTickPx.Value = Clamp(nudTickPx, (decimal)o.TickThicknessPx);
            nudCurvePx.Value = Clamp(nudCurvePx, (decimal)o.CurveThicknessPx);
            nudLabelPt.Value = Clamp(nudLabelPt, (decimal)o.LabelFontPt);
            nudUnitsPt.Value = Clamp(nudUnitsPt, (decimal)o.UnitsFontPt);

            nudMarginL.Value = Clamp(nudMarginL, (decimal)o.MarginLeft);
            nudMarginR.Value = Clamp(nudMarginR, (decimal)o.MarginRight);
            nudMarginT.Value = Clamp(nudMarginT, (decimal)o.MarginTop);
            nudMarginB.Value = Clamp(nudMarginB, (decimal)o.MarginBottom);
            nudGapH.Value = Clamp(nudGapH, (decimal)o.AxisGapHorizontal);
            nudGapV.Value = Clamp(nudGapV, (decimal)o.AxisGapVertical);

            nudXDigitsOff.Value = Clamp(nudXDigitsOff, (decimal)o.XDigitsOffsetPx);
            nudXUnitsGap.Value = Clamp(nudXUnitsGap, (decimal)o.XUnitsGapPx);
            nudMinGapX.Value = Clamp(nudMinGapX, (decimal)o.MinLabelGapXPx);
            nudMinGapY.Value = Clamp(nudMinGapY, (decimal)o.MinLabelGapYPx);
            nudYPad.Value = Clamp(nudYPad, (decimal)o.YDigitsLeftPadPx);
            nudYUnitsGap.Value = Clamp(nudYUnitsGap, (decimal)o.YUnitsGapFromNumbersPx);
            nudYUnitsFb.Value = Clamp(nudYUnitsFb, (decimal)o.YUnitsFallbackFromAxisPx);

            nudExtremumPx.Value = Clamp(nudExtremumPx, (decimal)o.ExtremumThicknessPx);
            nudGridPx.Value = Clamp(nudGridPx, (decimal)o.GridThicknessPx);
            nudDottedDash.Value = Clamp(nudDottedDash, (decimal)o.DottedDashLengthPx);
            nudDottedGap.Value = Clamp(nudDottedGap, (decimal)o.DottedGapLengthPx);
        }

        private PresetDto CapturePreset()
        {
            var o = ErgReportBuilder.GraphOptions;
            return new PresetDto
            {
                MajorTickLenMm = (float)nudMajorMm.Value,
                MinorTickLenMm = (float)nudMinorMm.Value,
                AxisThicknessPx = (float)nudAxisPx.Value,
                TickThicknessPx = (float)nudTickPx.Value,
                CurveThicknessPx = (float)nudCurvePx.Value,
                ExtremumThicknessPx = o.ExtremumThicknessPx,
                GridThicknessPx = o.GridThicknessPx,
                DottedDashLengthPx = (float)nudDottedDash.Value,
                DottedGapLengthPx = (float)nudDottedGap.Value,
                LabelFontPt = (float)nudLabelPt.Value,
                UnitsFontPt = (float)nudUnitsPt.Value,
                MarginLeft = (float)nudMarginL.Value,
                MarginRight = (float)nudMarginR.Value,
                MarginTop = (float)nudMarginT.Value,
                MarginBottom = (float)nudMarginB.Value,
                AxisGapHorizontal = (float)nudGapH.Value,
                AxisGapVertical = (float)nudGapV.Value,
                XDigitsOffsetPx = (float)nudXDigitsOff.Value,
                XUnitsGapPx = (float)nudXUnitsGap.Value,
                MinLabelGapXPx = (float)nudMinGapX.Value,
                MinLabelGapYPx = (float)nudMinGapY.Value,
                YDigitsLeftPadPx = (float)nudYPad.Value,
                YUnitsGapFromNumbersPx = (float)nudYUnitsGap.Value,
                YUnitsFallbackFromAxisPx = (float)nudYUnitsFb.Value
            };
        }

        private void ApplyPreset(PresetDto p)
        {
            var o = ErgReportBuilder.GraphOptions;
            var defaults = new ErgReportBuilder.GraphRenderOptions();
            o.MajorTickLenMm = p.MajorTickLenMm;
            o.MinorTickLenMm = p.MinorTickLenMm;
            o.AxisThicknessPx = p.AxisThicknessPx;
            o.TickThicknessPx = p.TickThicknessPx;
            o.CurveThicknessPx = p.CurveThicknessPx;
            o.ExtremumThicknessPx = p.ExtremumThicknessPx;
            o.GridThicknessPx = p.GridThicknessPx;
            o.DottedDashLengthPx = Math.Max(0.1f, p.DottedDashLengthPx > 0f ? p.DottedDashLengthPx : defaults.DottedDashLengthPx);
            o.DottedGapLengthPx = Math.Max(0.1f, p.DottedGapLengthPx > 0f ? p.DottedGapLengthPx : defaults.DottedGapLengthPx);
            o.LabelFontPt = p.LabelFontPt;
            o.UnitsFontPt = p.UnitsFontPt;
            o.MarginLeft = p.MarginLeft;
            o.MarginRight = p.MarginRight;
            o.MarginTop = p.MarginTop;
            o.MarginBottom = p.MarginBottom;
            o.AxisGapHorizontal = p.AxisGapHorizontal;
            o.AxisGapVertical = p.AxisGapVertical;
            o.XDigitsOffsetPx = p.XDigitsOffsetPx;
            o.XUnitsGapPx = p.XUnitsGapPx;
            o.MinLabelGapXPx = p.MinLabelGapXPx;
            o.MinLabelGapYPx = p.MinLabelGapYPx;
            o.YDigitsLeftPadPx = p.YDigitsLeftPadPx;
            o.YUnitsGapFromNumbersPx = p.YUnitsGapFromNumbersPx;
            o.YUnitsFallbackFromAxisPx = p.YUnitsFallbackFromAxisPx;

            LoadFromOptions();
        }

        private void ResetToDefaults()
        {
            var d = new ErgReportBuilder.GraphRenderOptions();
            ApplyPreset(
                new PresetDto
                {
                    MajorTickLenMm = d.MajorTickLenMm,
                    MinorTickLenMm = d.MinorTickLenMm,
                    AxisThicknessPx = d.AxisThicknessPx,
                    TickThicknessPx = d.TickThicknessPx,
                    CurveThicknessPx = d.CurveThicknessPx,
                    LabelFontPt = d.LabelFontPt,
                    UnitsFontPt = d.UnitsFontPt,
                    MarginLeft = d.MarginLeft,
                    MarginRight = d.MarginRight,
                    MarginTop = d.MarginTop,
                    MarginBottom = d.MarginBottom,
                    AxisGapHorizontal = d.AxisGapHorizontal,
                    AxisGapVertical = d.AxisGapVertical,
                    XDigitsOffsetPx = d.XDigitsOffsetPx,
                    XUnitsGapPx = d.XUnitsGapPx,
                    MinLabelGapXPx = d.MinLabelGapXPx,
                    MinLabelGapYPx = d.MinLabelGapYPx,
                    YDigitsLeftPadPx = d.YDigitsLeftPadPx,
                    YUnitsGapFromNumbersPx = d.YUnitsGapFromNumbersPx,
                    YUnitsFallbackFromAxisPx = d.YUnitsFallbackFromAxisPx,
                    ExtremumThicknessPx = d.ExtremumThicknessPx,
                    GridThicknessPx = d.GridThicknessPx,
                    DottedDashLengthPx = d.DottedDashLengthPx,
                    DottedGapLengthPx = d.DottedGapLengthPx
                });
        }

        private void SavePreset()
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Save preset",
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
                FileName = "graph-preset.json"
            };
            if(dlg.ShowDialog(this) != DialogResult.OK)
                return;

            var json = JsonSerializer.Serialize(CapturePreset(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dlg.FileName, json);
        }

        private bool LoadPreset()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Load preset",
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*"
            };
            if(dlg.ShowDialog(this) != DialogResult.OK)
                return false;

            var json = File.ReadAllText(dlg.FileName);
            var p = JsonSerializer.Deserialize<PresetDto>(json);
            if(p is null)
                return false;

            ApplyPreset(p);
            return true;
        }

        // ---------- Рендер превью ----------

        private void OnAnyChanged(object? sender, EventArgs e)
        {
            _debounce.Stop();
            _debounce.Start();
        }

        private void Redraw()
        {
            var o = ErgReportBuilder.GraphOptions;

            o.MajorTickLenMm = (float)nudMajorMm.Value;
            o.MinorTickLenMm = (float)nudMinorMm.Value;
            o.AxisThicknessPx = (float)nudAxisPx.Value;
            o.TickThicknessPx = (float)nudTickPx.Value;
            o.CurveThicknessPx = (float)nudCurvePx.Value;
            o.LabelFontPt = (float)nudLabelPt.Value;
            o.UnitsFontPt = (float)nudUnitsPt.Value;

            o.MarginLeft = (float)nudMarginL.Value;
            o.MarginRight = (float)nudMarginR.Value;
            o.MarginTop = (float)nudMarginT.Value;
            o.MarginBottom = (float)nudMarginB.Value;
            o.AxisGapHorizontal = (float)nudGapH.Value;
            o.AxisGapVertical = (float)nudGapV.Value;

            o.XDigitsOffsetPx = (float)nudXDigitsOff.Value;
            o.XUnitsGapPx = (float)nudXUnitsGap.Value;
            o.MinLabelGapXPx = (float)nudMinGapX.Value;
            o.MinLabelGapYPx = (float)nudMinGapY.Value;
            o.YDigitsLeftPadPx = (float)nudYPad.Value;
            o.YUnitsGapFromNumbersPx = (float)nudYUnitsGap.Value;
            o.YUnitsFallbackFromAxisPx = (float)nudYUnitsFb.Value;

            o.ExtremumThicknessPx = (float)nudExtremumPx.Value;
            o.GridThicknessPx = (float)nudGridPx.Value;
            o.DottedDashLengthPx = (float)nudDottedDash.Value;
            o.DottedGapLengthPx = (float)nudDottedGap.Value;

            var bytes = ErgReportBuilder.RenderGraphPng(_test, _eye);
            if(bytes == null)
            {
                _preview.Image?.Dispose();
                _preview.Image = null;
                return;
            }

            using var ms = new MemoryStream(bytes);
            using var img = Image.FromStream(ms);
            var old = _preview.Image;
            _preview.Image = (Image)img.Clone();
            old?.Dispose();
        }
    }
}
