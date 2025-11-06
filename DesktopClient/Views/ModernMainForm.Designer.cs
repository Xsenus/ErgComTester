using System.ComponentModel;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MicroluxErgConnect.Branding;
using MicroluxErgConnect.Models;

namespace MicroluxErgConnect.Views;

partial class ModernMainForm
{
    private IContainer components = null!;
    private TableLayoutPanel mainLayout;
    private Panel headerPanel;
    private TableLayoutPanel headerLayout;
    private PictureBox headerIconPictureBox;
    private TableLayoutPanel headerTextLayout;
    private Label headerTitleLabel;
    private Label headerSubtitleLabel;
    private FlowLayoutPanel headerBadgesPanel;
    private Label headerStatusLabel;
    private Label headerDeviceLabel;
    private Label headerPortLabel;
    private Label headerSyncLabel;
    private TableLayoutPanel contentLayout;
    private Panel detailsContainer;
    private TableLayoutPanel detailsLayout;
    private GroupBox connectionGroup;
    private TableLayoutPanel connectionLayout;
    private Label statusCaptionLabel;
    private Label statusValueLabel;
    private Label portCaptionLabel;
    private Label portValueLabel;
    private Label deviceCaptionLabel;
    private Label deviceValueLabel;
    private Label softwareCaptionLabel;
    private Label softwareValueLabel;
    private Label reportCaptionLabel;
    private Label reportValueLabel;
    private Label syncStatusLabel;
    private Button resetPortButton;
    private FlowLayoutPanel quickActionsPanel;
    private Button openSettingsButton;
    private Button openReportsButton;
    private Button convertBinButton;
    private Button btnGraphTuner;
    private Button openLogsButton;
    private Panel settingsContainer;
    private TableLayoutPanel settingsLayout;
    private GroupBox requisitesGroup;
    private TableLayoutPanel requisitesLayout;
    private Label scanIntervalLabel;
    private TextBox scanIntervalTextBox;
    private Label reconnectDelayLabel;
    private TextBox reconnectDelayTextBox;
    private Label backgroundSyncLabel;
    private TextBox backgroundSyncTextBox;
    private Label reportTemplateLabel;
    private ComboBox reportTemplateComboBox;
    private Label renderingModeLabel;
    private ComboBox renderingModeComboBox;
    private GroupBox updatesGroup;
    private TableLayoutPanel updatesLayout;
    private Label updateStatusCaptionLabel;
    private Label updateStatusValueLabel;
    private FlowLayoutPanel updatesButtonsPanel;
    private Button checkUpdatesButton;
    private Button installUpdateButton;
    private Label updateIntervalLabel;
    private TextBox updateIntervalTextBox;
    private Label manifestUrlLabel;
    private TextBox manifestUrlTextBox;
    private GroupBox logsGroup;
    private DataGridView logGridView;
    private DataGridViewTextBoxColumn timestampColumn;
    private DataGridViewTextBoxColumn levelColumn;
    private DataGridViewTextBoxColumn messageColumn;
    private BindingSource logsBindingSource;
    private StatusStrip mainStatusStrip;
    private ToolStripStatusLabel appNameStatusLabel;
    private ToolStripStatusLabel versionStatusLabel;
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private ToolStripMenuItem trayOpenMenuItem;
    private ToolStripMenuItem trayExitMenuItem;

    private void InitializeComponent()
    {
        components = new Container();
        var resources = new ComponentResourceManager(typeof(ModernMainForm));

        SuspendLayout();

        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        ForeColor = Color.FromArgb(28, 28, 30);
        BackColor = Color.White;
        ClientSize = new Size(1180, 720);
        MinimumSize = new Size(1024, 640);
        Icon = (Icon)resources.GetObject("$this.Icon")!;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Microlux ERG-Connect";

        logsBindingSource = new BindingSource(components);

        BuildMainLayout();
        BuildHeader(resources);
        BuildContent();
        BuildStatus(resources);

        Controls.Add(mainLayout);

        FormClosing += OnFormClosing;
        Load += OnFormLoaded;
        Resize += OnFormResized;

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildMainLayout()
    {
        mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(242, 244, 248),
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(30, 24, 30, 18)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle());
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
    }

    private void BuildHeader(ComponentResourceManager resources)
    {
        headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(24),
            Margin = new Padding(0, 0, 0, 24)
        };

        headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ColumnCount = 2,
            Padding = new Padding(24)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        headerIconPictureBox = new PictureBox
        {
            Image = resources.GetObject("headerIconPictureBox.Image") as Image ?? BrandingResources.AppIcon.ToBitmap(),
            SizeMode = PictureBoxSizeMode.StretchImage,
            Size = new Size(64, 64),
            Margin = new Padding(0, 0, 24, 0)
        };

        headerTextLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        headerTextLayout.RowStyles.Add(new RowStyle());
        headerTextLayout.RowStyles.Add(new RowStyle());
        headerTextLayout.RowStyles.Add(new RowStyle());

        headerTitleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(28, 28, 30),
            Text = "Microlux ERG-Connect",
            Margin = new Padding(0, 0, 0, 6)
        };

        headerSubtitleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(99, 99, 102),
            Text = "Интеллигентный контроль синхронизации и отчетов",
            Margin = new Padding(0, 0, 0, 12)
        };

        headerBadgesPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };

        headerStatusLabel = CreateBadgeLabel("Статус: готов");
        headerDeviceLabel = CreateBadgeLabel("Устройство: не найдено");
        headerPortLabel = CreateBadgeLabel("Порт: неизвестно");
        headerSyncLabel = CreateBadgeLabel("Синхронизация: пауза");

        headerBadgesPanel.Controls.Add(headerStatusLabel);
        headerBadgesPanel.Controls.Add(headerDeviceLabel);
        headerBadgesPanel.Controls.Add(headerPortLabel);
        headerBadgesPanel.Controls.Add(headerSyncLabel);

        headerTextLayout.Controls.Add(headerTitleLabel, 0, 0);
        headerTextLayout.Controls.Add(headerSubtitleLabel, 0, 1);
        headerTextLayout.Controls.Add(headerBadgesPanel, 0, 2);

        headerLayout.Controls.Add(headerIconPictureBox, 0, 0);
        headerLayout.Controls.Add(headerTextLayout, 1, 0);

        headerPanel.Controls.Add(headerLayout);
        mainLayout.Controls.Add(headerPanel, 0, 0);
    }

    private void BuildContent()
    {
        contentLayout = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47F));
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        BuildDeviceCard();
        BuildSettingsCard();
        BuildLogsCard();

        mainLayout.Controls.Add(contentLayout, 0, 1);
    }

    private void BuildDeviceCard()
    {
        detailsContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 24, 24),
            BackColor = Color.Transparent
        };

        detailsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        connectionGroup = ConfigureGroupBox("Карточка прибора");

        connectionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        statusCaptionLabel = CreateCaptionLabel("Статус");
        statusValueLabel = CreateValueLabel();
        statusValueLabel.ForeColor = Color.FromArgb(0, 122, 255);

        portCaptionLabel = CreateCaptionLabel("Порт");
        portValueLabel = CreateValueLabel();

        deviceCaptionLabel = CreateCaptionLabel("Устройство");
        deviceValueLabel = CreateValueLabel();

        softwareCaptionLabel = CreateCaptionLabel("ПО прибора");
        softwareValueLabel = CreateValueLabel();

        reportCaptionLabel = CreateCaptionLabel("Текущий отчёт");
        reportValueLabel = CreateValueLabel();

        resetPortButton = new Button
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Text = "Сброс",
            Margin = new Padding(0, 0, 12, 0),
            BackColor = Color.FromArgb(230, 231, 235),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(28, 28, 30),
            Padding = new Padding(12, 3, 12, 3),
            MinimumSize = new Size(0, 32)
        };
        resetPortButton.FlatAppearance.BorderSize = 0;
        resetPortButton.Click += OnResetPortClicked;

        syncStatusLabel = CreateValueLabel();
        syncStatusLabel.ForeColor = Color.FromArgb(0, 122, 255);
        syncStatusLabel.Margin = new Padding(0);

        connectionLayout.Controls.Add(statusCaptionLabel, 0, 0);
        connectionLayout.Controls.Add(statusValueLabel, 1, 0);
        connectionLayout.Controls.Add(portCaptionLabel, 0, 1);
        connectionLayout.Controls.Add(portValueLabel, 1, 1);
        connectionLayout.Controls.Add(deviceCaptionLabel, 0, 2);
        connectionLayout.Controls.Add(deviceValueLabel, 1, 2);
        connectionLayout.Controls.Add(softwareCaptionLabel, 0, 3);
        connectionLayout.Controls.Add(softwareValueLabel, 1, 3);
        connectionLayout.Controls.Add(reportCaptionLabel, 0, 4);
        connectionLayout.Controls.Add(reportValueLabel, 1, 4);
        connectionLayout.Controls.Add(resetPortButton, 0, 5);
        connectionLayout.Controls.Add(syncStatusLabel, 1, 5);

        connectionGroup.Controls.Add(connectionLayout);

        quickActionsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            WrapContents = false
        };

        openSettingsButton = CreateActionButton("Настройки", Color.FromArgb(52, 120, 246), OnOpenSettingsClicked);
        openReportsButton = CreateActionButton("Папка отчётов", Color.FromArgb(52, 199, 89), OnOpenReportsClicked);
        convertBinButton = CreateActionButton("Конвертация BIN", Color.FromArgb(255, 159, 10), OnConvertBinClicked);
        btnGraphTuner = CreateActionButton("Графики", Color.FromArgb(175, 82, 222), btnGraphTuner_Click);
        openLogsButton = CreateActionButton("Логи", Color.FromArgb(142, 142, 147), OnOpenLogsClicked);
        openLogsButton.Margin = new Padding(0);

        quickActionsPanel.Controls.Add(openSettingsButton);
        quickActionsPanel.Controls.Add(openReportsButton);
        quickActionsPanel.Controls.Add(convertBinButton);
        quickActionsPanel.Controls.Add(btnGraphTuner);
        quickActionsPanel.Controls.Add(openLogsButton);

        detailsLayout.Controls.Add(connectionGroup, 0, 0);
        detailsLayout.Controls.Add(quickActionsPanel, 0, 1);

        detailsContainer.Controls.Add(detailsLayout);
        contentLayout.Controls.Add(detailsContainer, 0, 0);
    }

    private void BuildSettingsCard()
    {
        settingsContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 24)
        };

        settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));

        requisitesGroup = ConfigureGroupBox("Реквизиты");
        requisitesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        requisitesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        requisitesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        scanIntervalLabel = CreateCaptionLabel("Сканирование (секунды)");
        scanIntervalTextBox = CreateNumericTextBox();
        scanIntervalTextBox.Validating += OnNumericTextBoxValidating;
        scanIntervalTextBox.Validated += OnScanIntervalValidated;

        reconnectDelayLabel = CreateCaptionLabel("Задержка переподключения");
        reconnectDelayTextBox = CreateNumericTextBox();
        reconnectDelayTextBox.Validating += OnNumericTextBoxValidating;
        reconnectDelayTextBox.Validated += OnReconnectDelayValidated;

        backgroundSyncLabel = CreateCaptionLabel("Фоновая синхронизация (мин)");
        backgroundSyncTextBox = CreateNumericTextBox();
        backgroundSyncTextBox.Validating += OnNumericTextBoxValidating;
        backgroundSyncTextBox.Validated += OnBackgroundSyncValidated;

        reportTemplateLabel = CreateCaptionLabel("Шаблон отчётов");
        reportTemplateComboBox = CreateComboBox();

        renderingModeLabel = CreateCaptionLabel("Генерация отчётов");
        renderingModeComboBox = CreateComboBox();

        requisitesLayout.Controls.Add(scanIntervalLabel, 0, 0);
        requisitesLayout.Controls.Add(scanIntervalTextBox, 1, 0);
        requisitesLayout.Controls.Add(reconnectDelayLabel, 0, 1);
        requisitesLayout.Controls.Add(reconnectDelayTextBox, 1, 1);
        requisitesLayout.Controls.Add(backgroundSyncLabel, 0, 2);
        requisitesLayout.Controls.Add(backgroundSyncTextBox, 1, 2);
        requisitesLayout.Controls.Add(reportTemplateLabel, 0, 3);
        requisitesLayout.Controls.Add(reportTemplateComboBox, 1, 3);
        requisitesLayout.Controls.Add(renderingModeLabel, 0, 4);
        requisitesLayout.Controls.Add(renderingModeComboBox, 1, 4);

        requisitesGroup.Controls.Add(requisitesLayout);

        updatesGroup = ConfigureGroupBox("Обновления");
        updatesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        updatesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        updatesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        updateStatusCaptionLabel = CreateCaptionLabel("Текущий статус");
        updateStatusValueLabel = CreateValueLabel();

        updatesButtonsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 12),
            WrapContents = false
        };

        checkUpdatesButton = CreateActionButton("Проверить", Color.FromArgb(52, 120, 246), OnCheckUpdatesClicked);
        installUpdateButton = CreateActionButton("Установить", Color.FromArgb(52, 199, 89), OnInstallUpdateClicked);
        updatesButtonsPanel.Controls.Add(checkUpdatesButton);
        updatesButtonsPanel.Controls.Add(installUpdateButton);

        updateIntervalLabel = CreateCaptionLabel("Интервал проверки (м)");
        updateIntervalTextBox = CreateNumericTextBox();
        updateIntervalTextBox.Validating += OnNumericTextBoxValidating;
        updateIntervalTextBox.Validated += OnUpdateIntervalValidated;

        manifestUrlLabel = CreateCaptionLabel("URL манифеста");
        manifestUrlTextBox = new TextBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point)
        };
        manifestUrlTextBox.Validated += OnManifestUrlValidated;

        updatesLayout.Controls.Add(updateStatusCaptionLabel, 0, 0);
        updatesLayout.Controls.Add(updateStatusValueLabel, 1, 0);
        updatesLayout.Controls.Add(updatesButtonsPanel, 1, 1);
        updatesLayout.Controls.Add(updateIntervalLabel, 0, 2);
        updatesLayout.Controls.Add(updateIntervalTextBox, 1, 2);
        updatesLayout.Controls.Add(manifestUrlLabel, 0, 3);
        updatesLayout.Controls.Add(manifestUrlTextBox, 1, 3);

        updatesGroup.Controls.Add(updatesLayout);

        settingsLayout.Controls.Add(requisitesGroup, 0, 0);
        settingsLayout.Controls.Add(updatesGroup, 0, 1);

        settingsContainer.Controls.Add(settingsLayout);
        contentLayout.Controls.Add(settingsContainer, 1, 0);
    }

    private void BuildLogsCard()
    {
        logsGroup = ConfigureGroupBox("Журнал событий");
        logsGroup.Padding = new Padding(20);
        logsGroup.Margin = new Padding(0);
        contentLayout.SetColumnSpan(logsGroup, 2);

        logGridView = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            DataSource = logsBindingSource,
            EnableHeadersVisualStyles = false,
            GridColor = Color.FromArgb(229, 231, 235),
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        var headerStyle = new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(242, 244, 248),
            ForeColor = Color.FromArgb(28, 28, 30),
            SelectionBackColor = Color.FromArgb(209, 213, 219),
            SelectionForeColor = Color.FromArgb(28, 28, 30),
            WrapMode = DataGridViewTriState.True,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
        };
        logGridView.ColumnHeadersDefaultCellStyle = headerStyle;

        var cellStyle = new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(28, 28, 30),
            SelectionBackColor = Color.FromArgb(229, 231, 235),
            SelectionForeColor = Color.FromArgb(28, 28, 30),
            WrapMode = DataGridViewTriState.False,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
        };
        logGridView.DefaultCellStyle = cellStyle;
        logGridView.RowsDefaultCellStyle = cellStyle;

        timestampColumn = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(LogEntry.Timestamp),
            HeaderText = "Время",
            MinimumWidth = 150,
            Width = 150,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            ReadOnly = true
        };

        levelColumn = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(LogEntry.Level),
            HeaderText = "Уровень",
            MinimumWidth = 80,
            Width = 80,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            ReadOnly = true
        };

        messageColumn = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(LogEntry.Message),
            HeaderText = "Сообщение",
            MinimumWidth = 200,
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };

        logGridView.Columns.AddRange(timestampColumn, levelColumn, messageColumn);

        logsGroup.Controls.Add(logGridView);
        contentLayout.Controls.Add(logsGroup, 0, 1);
    }

    private void BuildStatus(ComponentResourceManager resources)
    {
        mainStatusStrip = new StatusStrip
        {
            Dock = DockStyle.Fill,
            SizingGrip = false,
            Padding = new Padding(10, 0, 10, 0),
            AutoSize = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
        };

        appNameStatusLabel = new ToolStripStatusLabel
        {
            Text = "Microlux ERG-Connect",
            ForeColor = Color.FromArgb(99, 99, 102)
        };

        versionStatusLabel = new ToolStripStatusLabel
        {
            Text = "Версия: -",
            ForeColor = Color.FromArgb(99, 99, 102),
            Margin = new Padding(12, 3, 0, 2)
        };

        mainStatusStrip.Items.AddRange(new ToolStripItem[] { appNameStatusLabel, versionStatusLabel });
        mainLayout.Controls.Add(mainStatusStrip, 0, 2);

        trayMenu = new ContextMenuStrip
        {
            ImageScalingSize = new Size(20, 20)
        };

        trayOpenMenuItem = new ToolStripMenuItem("Открыть", null, TrayOpenMenuItem_Click);
        trayExitMenuItem = new ToolStripMenuItem("Выход", null, TrayExitMenuItem_Click);
        trayMenu.Items.AddRange(new ToolStripItem[] { trayOpenMenuItem, trayExitMenuItem });

        trayIcon = new NotifyIcon(components)
        {
            Icon = (Icon)resources.GetObject("trayIcon.Icon")!,
            Text = "Microlux ERG-Connect",
            Visible = true,
            ContextMenuStrip = trayMenu
        };
        trayIcon.DoubleClick += TrayIcon_DoubleClick;
    }

    private static Label CreateBadgeLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            BackColor = Color.FromArgb(94, 94, 237),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point),
            Text = text,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(14, 6, 14, 6)
        };
    }

    private static Label CreateCaptionLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(99, 99, 102),
            Margin = new Padding(0, 0, 16, 12)
        };
    }

    private static Label CreateValueLabel()
    {
        return new Label
        {
            AutoSize = true,
            Text = "-",
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(44, 44, 46),
            Margin = new Padding(0, 0, 0, 12)
        };
    }

    private static TextBox CreateNumericTextBox()
    {
        return new TextBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            MaxLength = 4,
            Margin = new Padding(0, 0, 0, 12)
        };
    }

    private static ComboBox CreateComboBox()
    {
        return new ComboBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            Margin = new Padding(0, 0, 0, 12)
        };
    }

    private static GroupBox ConfigureGroupBox(string text)
    {
        return new GroupBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(28, 28, 30),
            BackColor = Color.White,
            Padding = new Padding(20, 24, 20, 24),
            Margin = new Padding(0, 0, 0, 18)
        };
    }

    private static Button CreateActionButton(string text, Color background, EventHandler handler)
    {
        var button = new Button
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Text = text,
            BackColor = background,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.SemiBold, GraphicsUnit.Point),
            ForeColor = Color.White,
            Padding = new Padding(16, 4, 16, 4),
            MinimumSize = new Size(140, 36),
            Margin = new Padding(0, 0, 12, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += handler;
        return button;
    }
}
