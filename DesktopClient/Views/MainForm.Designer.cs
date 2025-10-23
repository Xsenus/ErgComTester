using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MicroluxErgConnect.Branding;
using MicroluxErgConnect.Models;

namespace MicroluxErgConnect.Views;

partial class MainForm
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
    private GroupBox settingsGroup;
    private TableLayoutPanel settingsLayout;
    private Label scanIntervalLabel;
    private TextBox scanIntervalTextBox;
    private Label reconnectDelayLabel;
    private TextBox reconnectDelayTextBox;
    private Label backgroundSyncLabel;
    private TextBox backgroundSyncTextBox;
    private Button openReportsButton;
    private Button convertBinButton;
    private GroupBox updatesGroup;
    private TableLayoutPanel updatesLayout;
    private Label updateStatusCaptionLabel;
    private Label updateStatusValueLabel;
    private Button checkUpdatesButton;
    private Button installUpdateButton;
    private Label updateIntervalLabel;
    private TextBox updateIntervalTextBox;
    private Label manifestUrlLabel;
    private TextBox manifestUrlTextBox;
    private Button openLogsButton;
    private GroupBox logsGroup;
    private DataGridView logGridView;
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
        mainLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        headerLayout = new TableLayoutPanel();
        headerIconPictureBox = new PictureBox();
        headerTextLayout = new TableLayoutPanel();
        headerTitleLabel = new Label();
        headerSubtitleLabel = new Label();
        headerBadgesPanel = new FlowLayoutPanel();
        headerStatusLabel = new Label();
        headerDeviceLabel = new Label();
        headerPortLabel = new Label();
        headerSyncLabel = new Label();
        connectionGroup = new GroupBox();
        connectionLayout = new TableLayoutPanel();
        statusCaptionLabel = new Label();
        statusValueLabel = new Label();
        portCaptionLabel = new Label();
        portValueLabel = new Label();
        deviceCaptionLabel = new Label();
        deviceValueLabel = new Label();
        softwareCaptionLabel = new Label();
        softwareValueLabel = new Label();
        reportCaptionLabel = new Label();
        reportValueLabel = new Label();
        syncStatusLabel = new Label();
        resetPortButton = new Button();
        settingsGroup = new GroupBox();
        settingsLayout = new TableLayoutPanel();
        scanIntervalLabel = new Label();
        scanIntervalTextBox = new TextBox();
        reconnectDelayLabel = new Label();
        reconnectDelayTextBox = new TextBox();
        backgroundSyncLabel = new Label();
        backgroundSyncTextBox = new TextBox();
        openReportsButton = new Button();
        convertBinButton = new Button();
        updatesGroup = new GroupBox();
        updatesLayout = new TableLayoutPanel();
        updateStatusCaptionLabel = new Label();
        updateStatusValueLabel = new Label();
        checkUpdatesButton = new Button();
        installUpdateButton = new Button();
        updateIntervalLabel = new Label();
        updateIntervalTextBox = new TextBox();
        manifestUrlLabel = new Label();
        manifestUrlTextBox = new TextBox();
        openLogsButton = new Button();
        logsGroup = new GroupBox();
        logGridView = new DataGridView();
        logsBindingSource = new BindingSource(components);
        mainStatusStrip = new StatusStrip();
        appNameStatusLabel = new ToolStripStatusLabel();
        versionStatusLabel = new ToolStripStatusLabel();
        trayIcon = new NotifyIcon(components);
        trayMenu = new ContextMenuStrip(components);
        trayOpenMenuItem = new ToolStripMenuItem();
        trayExitMenuItem = new ToolStripMenuItem();
        ((ISupportInitialize)headerIconPictureBox).BeginInit();
        mainLayout.SuspendLayout();
        connectionGroup.SuspendLayout();
        connectionLayout.SuspendLayout();
        settingsGroup.SuspendLayout();
        settingsLayout.SuspendLayout();
        updatesGroup.SuspendLayout();
        updatesLayout.SuspendLayout();
        logsGroup.SuspendLayout();
        ((ISupportInitialize)logGridView).BeginInit();
        mainStatusStrip.SuspendLayout();
        trayMenu.SuspendLayout();
        SuspendLayout();
        //
        // mainLayout
        // 
        mainLayout.ColumnCount = 1;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowCount = 4;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.BackColor = Color.FromArgb(245, 248, 252);
        mainLayout.Padding = new Padding(20, 20, 20, 12);
        mainLayout.Controls.Add(CreateHeaderPanel(), 0, 0);
        mainLayout.Controls.Add(CreateTopLayout(), 0, 1);
        mainLayout.Controls.Add(CreateLogsGroup(), 0, 2);
        mainLayout.Controls.Add(CreateStatusStrip(), 0, 3);
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(245, 248, 252);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        ClientSize = new Size(1200, 720);
        Controls.Add(mainLayout);
        Icon = AppBranding.CreateWindowIcon();
        MinimumSize = new Size(960, 620);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Microlux ERG-Connect";
        FormClosing += OnFormClosing;
        Load += OnFormLoaded;
        Resize += OnFormResized;
        mainLayout.ResumeLayout(false);
        connectionGroup.ResumeLayout(false);
        connectionGroup.PerformLayout();
        connectionLayout.ResumeLayout(false);
        connectionLayout.PerformLayout();
        settingsGroup.ResumeLayout(false);
        settingsLayout.ResumeLayout(false);
        settingsLayout.PerformLayout();
        updatesGroup.ResumeLayout(false);
        updatesLayout.ResumeLayout(false);
        updatesLayout.PerformLayout();
        logsGroup.ResumeLayout(false);
        ((ISupportInitialize)logGridView).EndInit();
        mainStatusStrip.ResumeLayout(false);
        mainStatusStrip.PerformLayout();
        trayMenu.ResumeLayout(false);
        ((ISupportInitialize)headerIconPictureBox).EndInit();
        ConfigureTrayIcon();
        ResumeLayout(false);
    }

    private Control CreateHeaderPanel()
    {
        headerPanel.BackColor = Color.FromArgb(26, 38, 55);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Padding = new Padding(24, 18, 24, 18);
        headerPanel.Margin = new Padding(0, 0, 0, 16);

        headerLayout.ColumnCount = 2;
        headerLayout.ColumnStyles.Clear();
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.RowCount = 1;
        headerLayout.RowStyles.Clear();
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerLayout.Dock = DockStyle.Fill;
        headerLayout.Margin = new Padding(0);

        headerIconPictureBox.Size = new Size(88, 88);
        headerIconPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        headerIconPictureBox.Margin = new Padding(0, 0, 24, 0);
        headerIconPictureBox.Image = AppBranding.GetHeaderImage();

        headerTextLayout.ColumnCount = 1;
        headerTextLayout.ColumnStyles.Clear();
        headerTextLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerTextLayout.RowCount = 3;
        headerTextLayout.RowStyles.Clear();
        headerTextLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerTextLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerTextLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerTextLayout.Dock = DockStyle.Fill;
        headerTextLayout.Margin = new Padding(0);

        headerTitleLabel.AutoSize = true;
        headerTitleLabel.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold, GraphicsUnit.Point);
        headerTitleLabel.ForeColor = Color.White;
        headerTitleLabel.Text = "Microlux ERG-Connect";

        headerSubtitleLabel.AutoSize = true;
        headerSubtitleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
        headerSubtitleLabel.ForeColor = Color.FromArgb(189, 206, 223);
        headerSubtitleLabel.Margin = new Padding(0, 6, 0, 12);
        headerSubtitleLabel.Text = "Мониторинг и синхронизация оборудования Microlux";

        headerBadgesPanel.AutoSize = true;
        headerBadgesPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        headerBadgesPanel.Dock = DockStyle.Fill;
        headerBadgesPanel.FlowDirection = FlowDirection.LeftToRight;
        headerBadgesPanel.Margin = new Padding(0, 10, 0, 0);
        headerBadgesPanel.Padding = new Padding(0);
        headerBadgesPanel.WrapContents = true;

        ConfigureHeaderBadge(headerStatusLabel, "Статус: -");
        ConfigureHeaderBadge(headerDeviceLabel, "Устройство: -");
        ConfigureHeaderBadge(headerPortLabel, "Порт: -");
        ConfigureHeaderBadge(headerSyncLabel, "Синхронизация: -");
        headerDeviceLabel.BackColor = Color.FromArgb(60, 87, 119);
        headerPortLabel.BackColor = Color.FromArgb(74, 102, 135);

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
        return headerPanel;
    }

    private static void ConfigureHeaderBadge(Label label, string text)
    {
        label.AutoSize = true;
        label.Text = text;
        label.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        label.ForeColor = Color.White;
        label.BackColor = Color.FromArgb(48, 149, 177);
        label.Margin = new Padding(0, 0, 12, 8);
        label.Padding = new Padding(12, 6, 12, 6);
        label.BorderStyle = BorderStyle.None;
    }

    private Control CreateTopLayout()
    {
        var topLayout = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 16),
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        ConfigureConnectionGroup();
        ConfigureSettingsGroup();
        ConfigureUpdatesGroup();

        connectionGroup.Margin = new Padding(0, 0, 16, 0);
        settingsGroup.Margin = new Padding(0, 0, 16, 0);
        updatesGroup.Margin = new Padding(0);

        topLayout.Controls.Add(connectionGroup, 0, 0);
        topLayout.Controls.Add(settingsGroup, 1, 0);
        topLayout.Controls.Add(updatesGroup, 2, 0);
        return topLayout;
    }

    private void ConfigureConnectionGroup()
    {
        connectionGroup.Text = "Подключение";
        connectionGroup.Dock = DockStyle.Fill;
        ApplyGroupBoxStyle(connectionGroup);

        connectionLayout.ColumnCount = 2;
        connectionLayout.RowCount = 7;
        connectionLayout.ColumnStyles.Clear();
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        connectionLayout.RowStyles.Clear();
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.Dock = DockStyle.Fill;
        connectionLayout.AutoSize = true;
        connectionLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        connectionLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        connectionLayout.Padding = new Padding(0, 4, 0, 0);
        connectionLayout.Margin = new Padding(0);
        connectionLayout.BackColor = Color.White;

        statusCaptionLabel.Text = "Статус:";
        StyleCaptionLabel(statusCaptionLabel);
        statusCaptionLabel.Margin = new Padding(0, 0, 16, 0);
        StyleValueLabel(statusValueLabel, accent: true);
        statusValueLabel.Margin = new Padding(8, 0, 0, 0);

        portCaptionLabel.Text = "Порт:";
        StyleCaptionLabel(portCaptionLabel);
        StyleValueLabel(portValueLabel);

        deviceCaptionLabel.Text = "Прибор:";
        StyleCaptionLabel(deviceCaptionLabel);
        StyleValueLabel(deviceValueLabel);

        softwareCaptionLabel.Text = "ПО:";
        StyleCaptionLabel(softwareCaptionLabel);
        StyleValueLabel(softwareValueLabel);

        reportCaptionLabel.Text = "Отчет:";
        StyleCaptionLabel(reportCaptionLabel);
        StyleValueLabel(reportValueLabel);

        syncStatusLabel.AutoSize = true;
        syncStatusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        syncStatusLabel.ForeColor = Color.FromArgb(54, 127, 151);
        syncStatusLabel.Margin = new Padding(0, 14, 0, 4);

        resetPortButton.Text = "Сбросить порт";
        StylePrimaryButton(resetPortButton);
        resetPortButton.Click += OnResetPortClicked;
        resetPortButton.Margin = new Padding(0, 16, 0, 0);

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
        connectionLayout.Controls.Add(syncStatusLabel, 0, 5);
        connectionLayout.SetColumnSpan(syncStatusLabel, 2);
        connectionLayout.Controls.Add(resetPortButton, 0, 6);
        connectionLayout.SetColumnSpan(resetPortButton, 2);
        connectionGroup.Controls.Add(connectionLayout);
    }

    private void ConfigureSettingsGroup()
    {
        settingsGroup.Text = "Настройки опроса";
        settingsGroup.Dock = DockStyle.Fill;
        ApplyGroupBoxStyle(settingsGroup);

        settingsLayout.ColumnCount = 2;
        settingsLayout.RowCount = 5;
        settingsLayout.ColumnStyles.Clear();
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        settingsLayout.RowStyles.Clear();
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.Dock = DockStyle.Fill;
        settingsLayout.Padding = new Padding(0, 4, 0, 0);
        settingsLayout.Margin = new Padding(0);
        settingsLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        settingsLayout.BackColor = Color.White;

        scanIntervalLabel.Text = "Интервал поиска (с):";
        StyleCaptionLabel(scanIntervalLabel);
        scanIntervalLabel.Margin = new Padding(0, 0, 16, 0);
        scanIntervalTextBox.Width = 80;
        scanIntervalTextBox.BorderStyle = BorderStyle.FixedSingle;
        scanIntervalTextBox.Margin = new Padding(0, 4, 0, 0);
        scanIntervalTextBox.TextAlign = HorizontalAlignment.Center;
        scanIntervalTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        scanIntervalTextBox.MaximumSize = new Size(160, 0);
        scanIntervalTextBox.MinimumSize = new Size(80, 0);
        scanIntervalTextBox.Validating += OnNumericTextBoxValidating;
        scanIntervalTextBox.Validated += OnScanIntervalValidated;

        reconnectDelayLabel.Text = "Задержка перепроверки (с):";
        StyleCaptionLabel(reconnectDelayLabel);
        reconnectDelayLabel.Margin = new Padding(0, 10, 16, 0);
        reconnectDelayTextBox.Width = 80;
        reconnectDelayTextBox.BorderStyle = BorderStyle.FixedSingle;
        reconnectDelayTextBox.Margin = new Padding(0, 4, 0, 0);
        reconnectDelayTextBox.TextAlign = HorizontalAlignment.Center;
        reconnectDelayTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        reconnectDelayTextBox.MaximumSize = new Size(160, 0);
        reconnectDelayTextBox.MinimumSize = new Size(80, 0);
        reconnectDelayTextBox.Validating += OnNumericTextBoxValidating;
        reconnectDelayTextBox.Validated += OnReconnectDelayValidated;

        backgroundSyncLabel.Text = "Период синхронизации (мин):";
        StyleCaptionLabel(backgroundSyncLabel);
        backgroundSyncLabel.Margin = new Padding(0, 10, 16, 0);
        backgroundSyncTextBox.Width = 80;
        backgroundSyncTextBox.BorderStyle = BorderStyle.FixedSingle;
        backgroundSyncTextBox.Margin = new Padding(0, 4, 0, 0);
        backgroundSyncTextBox.TextAlign = HorizontalAlignment.Center;
        backgroundSyncTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        backgroundSyncTextBox.MaximumSize = new Size(160, 0);
        backgroundSyncTextBox.MinimumSize = new Size(80, 0);
        backgroundSyncTextBox.Validating += OnNumericTextBoxValidating;
        backgroundSyncTextBox.Validated += OnBackgroundSyncValidated;

        openReportsButton.Text = "Открыть каталог отчетов";
        StyleSecondaryButton(openReportsButton);
        openReportsButton.Click += OnOpenReportsClicked;
        openReportsButton.Margin = new Padding(0, 14, 0, 0);
        convertBinButton.Text = "Конвертировать .bin в отчет";
        StylePrimaryButton(convertBinButton);
        convertBinButton.Click += OnConvertBinClicked;
        convertBinButton.Margin = new Padding(0, 10, 0, 0);
        settingsLayout.Controls.Add(scanIntervalLabel, 0, 0);
        settingsLayout.Controls.Add(scanIntervalTextBox, 1, 0);
        settingsLayout.Controls.Add(reconnectDelayLabel, 0, 1);
        settingsLayout.Controls.Add(reconnectDelayTextBox, 1, 1);
        settingsLayout.Controls.Add(backgroundSyncLabel, 0, 2);
        settingsLayout.Controls.Add(backgroundSyncTextBox, 1, 2);
        settingsLayout.Controls.Add(openReportsButton, 0, 3);
        settingsLayout.SetColumnSpan(openReportsButton, 2);
        settingsLayout.Controls.Add(convertBinButton, 0, 4);
        settingsLayout.SetColumnSpan(convertBinButton, 2);
        settingsGroup.Controls.Add(settingsLayout);
    }

    private void ConfigureUpdatesGroup()
    {
        updatesGroup.Text = "Обновления";
        updatesGroup.Dock = DockStyle.Fill;
        ApplyGroupBoxStyle(updatesGroup);

        updatesLayout.ColumnCount = 2;
        updatesLayout.RowCount = 6;
        updatesLayout.ColumnStyles.Clear();
        updatesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        updatesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        updatesLayout.RowStyles.Clear();
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.Dock = DockStyle.Fill;
        updatesLayout.Padding = new Padding(0, 4, 0, 0);
        updatesLayout.Margin = new Padding(0);
        updatesLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        updatesLayout.BackColor = Color.White;

        updateStatusCaptionLabel.Text = "Статус обновлений:";
        StyleCaptionLabel(updateStatusCaptionLabel);
        updateStatusCaptionLabel.Margin = new Padding(0, 0, 16, 0);
        updateStatusValueLabel.AutoSize = true;
        updateStatusValueLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        updateStatusValueLabel.ForeColor = Color.FromArgb(59, 179, 115);
        updateStatusValueLabel.Margin = new Padding(0, 8, 0, 0);

        checkUpdatesButton.Text = "Проверить";
        StyleSecondaryButton(checkUpdatesButton);
        checkUpdatesButton.Click += OnCheckUpdatesClicked;

        installUpdateButton.Text = "Установить";
        StylePrimaryButton(installUpdateButton);
        installUpdateButton.Click += OnInstallUpdateClicked;

        updateIntervalLabel.Text = "Интервал проверки (мин):";
        StyleCaptionLabel(updateIntervalLabel);
        updateIntervalLabel.Margin = new Padding(0, 12, 16, 0);
        updateIntervalTextBox.Width = 80;
        updateIntervalTextBox.BorderStyle = BorderStyle.FixedSingle;
        updateIntervalTextBox.Margin = new Padding(0, 4, 0, 0);
        updateIntervalTextBox.TextAlign = HorizontalAlignment.Center;
        updateIntervalTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        updateIntervalTextBox.MaximumSize = new Size(160, 0);
        updateIntervalTextBox.MinimumSize = new Size(80, 0);
        updateIntervalTextBox.Validating += OnNumericTextBoxValidating;
        updateIntervalTextBox.Validated += OnUpdateIntervalValidated;

        manifestUrlLabel.Text = "URL манифеста:";
        StyleCaptionLabel(manifestUrlLabel);
        manifestUrlLabel.Margin = new Padding(0, 12, 16, 0);
        manifestUrlTextBox.Width = 260;
        manifestUrlTextBox.BorderStyle = BorderStyle.FixedSingle;
        manifestUrlTextBox.Margin = new Padding(0, 4, 0, 0);
        manifestUrlTextBox.Dock = DockStyle.Fill;
        manifestUrlTextBox.Validated += OnManifestUrlValidated;

        openLogsButton.Text = "Открыть каталог логов";
        StyleSecondaryButton(openLogsButton);
        openLogsButton.Click += OnOpenLogsClicked;
        openLogsButton.Margin = new Padding(0, 14, 0, 0);

        updatesLayout.Controls.Add(updateStatusCaptionLabel, 0, 0);
        updatesLayout.Controls.Add(updateStatusValueLabel, 0, 1);
        updatesLayout.SetColumnSpan(updateStatusValueLabel, 2);

        var buttonsPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 10, 0, 6)
        };
        buttonsPanel.Controls.Add(checkUpdatesButton);
        buttonsPanel.Controls.Add(installUpdateButton);
        updatesLayout.Controls.Add(buttonsPanel, 0, 2);
        updatesLayout.SetColumnSpan(buttonsPanel, 2);

        updatesLayout.Controls.Add(updateIntervalLabel, 0, 3);
        updatesLayout.Controls.Add(updateIntervalTextBox, 1, 3);
        updatesLayout.Controls.Add(manifestUrlLabel, 0, 4);
        updatesLayout.Controls.Add(manifestUrlTextBox, 1, 4);
        updatesLayout.Controls.Add(openLogsButton, 0, 5);
        updatesLayout.SetColumnSpan(openLogsButton, 2);
        updatesGroup.Controls.Add(updatesLayout);
    }

    private Control CreateLogsGroup()
    {
        logsGroup.Text = "Журнал";
        logsGroup.Dock = DockStyle.Fill;
        ApplyGroupBoxStyle(logsGroup);
        logsGroup.Margin = new Padding(0, 0, 0, 16);

        logGridView.Dock = DockStyle.Fill;
        logGridView.ReadOnly = true;
        logGridView.AllowUserToAddRows = false;
        logGridView.AllowUserToDeleteRows = false;
        logGridView.AllowUserToResizeRows = false;
        logGridView.AutoGenerateColumns = false;
        logGridView.RowHeadersVisible = false;
        logGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        logGridView.MultiSelect = false;
        logGridView.DataSource = logsBindingSource;
        logGridView.BackgroundColor = Color.White;
        logGridView.BorderStyle = BorderStyle.None;
        logGridView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        logGridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        logGridView.EnableHeadersVisualStyles = false;
        logGridView.RowTemplate.Height = 28;
        logGridView.GridColor = Color.FromArgb(230, 236, 244);

        logGridView.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(26, 38, 55),
            ForeColor = Color.White,
            SelectionBackColor = Color.FromArgb(26, 38, 55),
            SelectionForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };

        logGridView.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(33, 37, 41),
            SelectionBackColor = Color.FromArgb(220, 244, 247),
            SelectionForeColor = Color.FromArgb(33, 37, 41),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            WrapMode = DataGridViewTriState.False
        };

        logGridView.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(245, 249, 252),
            ForeColor = Color.FromArgb(33, 37, 41),
            SelectionBackColor = Color.FromArgb(210, 238, 242),
            SelectionForeColor = Color.FromArgb(33, 37, 41)
        };

        var timestampColumn = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(LogEntry.Timestamp),
            HeaderText = "Время",
            Width = 180,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" }
        };
        var levelColumn = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(LogEntry.Level),
            HeaderText = "Уровень",
            Width = 80,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        var messageColumn = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(LogEntry.Message),
            HeaderText = "Сообщение",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };

        logGridView.Columns.AddRange(timestampColumn, levelColumn, messageColumn);
        logsGroup.Controls.Add(logGridView);
        return logsGroup;
    }

    private Control CreateStatusStrip()
    {
        mainStatusStrip.Dock = DockStyle.Fill;
        mainStatusStrip.SizingGrip = false;
        mainStatusStrip.GripStyle = ToolStripGripStyle.Hidden;
        mainStatusStrip.BackColor = Color.FromArgb(26, 38, 55);
        mainStatusStrip.ForeColor = Color.White;
        mainStatusStrip.Padding = new Padding(12, 0, 12, 0);
        mainStatusStrip.ImageScalingSize = new Size(16, 16);
        mainStatusStrip.RenderMode = ToolStripRenderMode.System;
        appNameStatusLabel.Text = "Microlux ERG-Connect";
        appNameStatusLabel.Spring = true;
        appNameStatusLabel.ForeColor = Color.White;
        appNameStatusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        versionStatusLabel.Text = "Версия:";
        versionStatusLabel.ForeColor = Color.FromArgb(189, 206, 223);
        versionStatusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        mainStatusStrip.Items.AddRange(new ToolStripItem[] { appNameStatusLabel, versionStatusLabel });
        return mainStatusStrip;
    }

    private static void ApplyGroupBoxStyle(GroupBox groupBox)
    {
        groupBox.BackColor = Color.White;
        groupBox.ForeColor = Color.FromArgb(33, 37, 41);
        groupBox.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        groupBox.Padding = new Padding(16, 20, 16, 16);
    }

    private static void StyleCaptionLabel(Label label)
    {
        label.AutoSize = true;
        label.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        label.ForeColor = Color.FromArgb(120, 128, 145);
        label.Margin = new Padding(0, 8, 16, 0);
        label.Anchor = AnchorStyles.Left;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static void StyleValueLabel(Label label, bool accent = false)
    {
        label.AutoSize = true;
        label.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        label.ForeColor = accent ? Color.FromArgb(34, 158, 189) : Color.FromArgb(33, 37, 41);
        label.Margin = new Padding(8, 8, 0, 0);
        label.Anchor = AnchorStyles.Left;
        label.AutoEllipsis = true;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static void StylePrimaryButton(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(140, 36);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 182, 214);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 138, 166);
        button.BackColor = Color.FromArgb(34, 158, 189);
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        button.Margin = new Padding(0, 10, 0, 0);
        button.Padding = new Padding(10, 6, 10, 6);
        button.Cursor = Cursors.Hand;
        button.Anchor = AnchorStyles.Left;
    }

    private static void StyleSecondaryButton(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(140, 36);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 236, 242);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 228, 236);
        button.BackColor = Color.FromArgb(239, 246, 249);
        button.ForeColor = Color.FromArgb(33, 37, 41);
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        button.Margin = new Padding(0, 10, 0, 0);
        button.Padding = new Padding(10, 6, 10, 6);
        button.Cursor = Cursors.Hand;
        button.Anchor = AnchorStyles.Left;
    }

    private void ConfigureTrayIcon()
    {
        trayIcon.Icon = AppBranding.CreateTrayIcon();
        trayIcon.Visible = true;
        trayIcon.Text = "Microlux ERG-Connect";

        trayOpenMenuItem.Text = "Открыть";
        trayOpenMenuItem.Click += (_, _) => RestoreFromTray();
        trayExitMenuItem.Text = "Выход";
        trayExitMenuItem.Click += (_, _) => ExitApplication();

        trayMenu.Items.AddRange(new ToolStripItem[] { trayOpenMenuItem, trayExitMenuItem });
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
