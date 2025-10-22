using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MicroluxErgConnect.Models;

namespace MicroluxErgConnect.Views;

partial class MainForm
{
    private IContainer components = null!;
    private TableLayoutPanel mainLayout;
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
        mainLayout.RowCount = 3;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Padding = new Padding(8);
        mainLayout.Controls.Add(CreateTopLayout(), 0, 0);
        mainLayout.Controls.Add(CreateLogsGroup(), 0, 1);
        mainLayout.Controls.Add(CreateStatusStrip(), 0, 2);
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 720);
        Controls.Add(mainLayout);
        MinimumSize = new Size(900, 600);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Микролюкс ERG-Connect";
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
        ConfigureTrayIcon();
        ResumeLayout(false);
    }

    private Control CreateTopLayout()
    {
        var topLayout = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8)
        };
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

        ConfigureConnectionGroup();
        ConfigureSettingsGroup();
        ConfigureUpdatesGroup();

        topLayout.Controls.Add(connectionGroup, 0, 0);
        topLayout.Controls.Add(settingsGroup, 1, 0);
        topLayout.Controls.Add(updatesGroup, 2, 0);
        return topLayout;
    }

    private void ConfigureConnectionGroup()
    {
        connectionGroup.Text = "Подключение";
        connectionGroup.Dock = DockStyle.Fill;
        connectionGroup.Padding = new Padding(10);

        connectionLayout.ColumnCount = 2;
        connectionLayout.RowCount = 7;
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.Dock = DockStyle.Fill;
        connectionLayout.Padding = new Padding(0, 0, 0, 8);

        statusCaptionLabel.Text = "Статус:";
        statusCaptionLabel.AutoSize = true;
        statusValueLabel.AutoSize = true;
        statusValueLabel.MaximumSize = new Size(0, 0);

        portCaptionLabel.Text = "Порт:";
        portCaptionLabel.AutoSize = true;
        portValueLabel.AutoSize = true;

        deviceCaptionLabel.Text = "Прибор:";
        deviceCaptionLabel.AutoSize = true;
        deviceValueLabel.AutoSize = true;

        softwareCaptionLabel.Text = "ПО:";
        softwareCaptionLabel.AutoSize = true;
        softwareValueLabel.AutoSize = true;

        reportCaptionLabel.Text = "Отчет:";
        reportCaptionLabel.AutoSize = true;
        reportValueLabel.AutoSize = true;

        syncStatusLabel.AutoSize = true;
        syncStatusLabel.Margin = new Padding(0, 6, 0, 6);

        resetPortButton.Text = "Сбросить порт";
        resetPortButton.AutoSize = true;
        resetPortButton.Margin = new Padding(0, 6, 0, 0);
        resetPortButton.Click += OnResetPortClicked;

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
        settingsGroup.Padding = new Padding(10);

        settingsLayout.ColumnCount = 2;
        settingsLayout.RowCount = 5;
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.Dock = DockStyle.Fill;
        settingsLayout.Padding = new Padding(0, 0, 0, 8);
        settingsLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;

        scanIntervalLabel.Text = "Интервал поиска (с):";
        scanIntervalLabel.AutoSize = true;
        scanIntervalTextBox.Width = 80;
        scanIntervalTextBox.Validating += OnNumericTextBoxValidating;
        scanIntervalTextBox.Validated += OnScanIntervalValidated;

        reconnectDelayLabel.Text = "Задержка перепроверки (с):";
        reconnectDelayLabel.AutoSize = true;
        reconnectDelayTextBox.Width = 80;
        reconnectDelayTextBox.Validating += OnNumericTextBoxValidating;
        reconnectDelayTextBox.Validated += OnReconnectDelayValidated;

        backgroundSyncLabel.Text = "Период синхронизации (мин):";
        backgroundSyncLabel.AutoSize = true;
        backgroundSyncTextBox.Width = 80;
        backgroundSyncTextBox.Validating += OnNumericTextBoxValidating;
        backgroundSyncTextBox.Validated += OnBackgroundSyncValidated;

        openReportsButton.Text = "Открыть каталог отчетов";
        openReportsButton.AutoSize = true;
        openReportsButton.Click += OnOpenReportsClicked;
        convertBinButton.Text = "Конвертировать .bin в отчет";
        convertBinButton.AutoSize = true;
        convertBinButton.Click += OnConvertBinClicked;
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
        updatesGroup.Padding = new Padding(10);

        updatesLayout.ColumnCount = 2;
        updatesLayout.RowCount = 5;
        updatesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        updatesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.Dock = DockStyle.Fill;
        updatesLayout.Padding = new Padding(0, 0, 0, 8);

        updateStatusCaptionLabel.Text = "Статус обновлений:";
        updateStatusCaptionLabel.AutoSize = true;
        updateStatusValueLabel.AutoSize = true;
        updateStatusValueLabel.MaximumSize = new Size(0, 0);

        checkUpdatesButton.Text = "Проверить";
        checkUpdatesButton.AutoSize = true;
        checkUpdatesButton.Click += OnCheckUpdatesClicked;

        installUpdateButton.Text = "Установить";
        installUpdateButton.AutoSize = true;
        installUpdateButton.Click += OnInstallUpdateClicked;

        updateIntervalLabel.Text = "Интервал проверки (мин):";
        updateIntervalLabel.AutoSize = true;
        updateIntervalTextBox.Width = 80;
        updateIntervalTextBox.Validating += OnNumericTextBoxValidating;
        updateIntervalTextBox.Validated += OnUpdateIntervalValidated;

        manifestUrlLabel.Text = "URL манифеста:";
        manifestUrlLabel.AutoSize = true;
        manifestUrlTextBox.Width = 260;
        manifestUrlTextBox.Validated += OnManifestUrlValidated;

        openLogsButton.Text = "Открыть каталог логов";
        openLogsButton.AutoSize = true;
        openLogsButton.Click += OnOpenLogsClicked;

        updatesLayout.Controls.Add(updateStatusCaptionLabel, 0, 0);
        updatesLayout.Controls.Add(updateStatusValueLabel, 0, 1);
        updatesLayout.SetColumnSpan(updateStatusValueLabel, 2);

        var buttonsPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
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
        logsGroup.Padding = new Padding(10);

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
        appNameStatusLabel.Text = "Микролюкс ERG-Connect";
        appNameStatusLabel.Spring = true;
        versionStatusLabel.Text = "Версия:";
        mainStatusStrip.Items.AddRange(new ToolStripItem[] { appNameStatusLabel, versionStatusLabel });
        return mainStatusStrip;
    }

    private void ConfigureTrayIcon()
    {
        trayIcon.Icon = System.Drawing.SystemIcons.Application;
        trayIcon.Visible = true;
        trayIcon.Text = "Микролюкс ERG-Connect";

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
