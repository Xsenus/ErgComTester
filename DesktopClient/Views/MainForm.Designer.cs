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
    private GroupBox settingsGroup;
    private TableLayoutPanel settingsLayout;
    private Label scanIntervalLabel;
    private TextBox scanIntervalTextBox;
    private Label reconnectDelayLabel;
    private TextBox reconnectDelayTextBox;
    private Label backgroundSyncLabel;
    private TextBox backgroundSyncTextBox;
    private FlowLayoutPanel settingsButtonsPanel;
    private Button openReportsButton;
    private Button convertBinButton;
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
    private Button openLogsButton;
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
        contentLayout = new TableLayoutPanel();
        detailsContainer = new Panel();
        detailsLayout = new TableLayoutPanel();
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
        settingsButtonsPanel = new FlowLayoutPanel();
        openReportsButton = new Button();
        convertBinButton = new Button();
        updatesGroup = new GroupBox();
        updatesLayout = new TableLayoutPanel();
        updateStatusCaptionLabel = new Label();
        updateStatusValueLabel = new Label();
        updatesButtonsPanel = new FlowLayoutPanel();
        checkUpdatesButton = new Button();
        installUpdateButton = new Button();
        updateIntervalLabel = new Label();
        updateIntervalTextBox = new TextBox();
        manifestUrlLabel = new Label();
        manifestUrlTextBox = new TextBox();
        openLogsButton = new Button();
        logsGroup = new GroupBox();
        logGridView = new DataGridView();
        timestampColumn = new DataGridViewTextBoxColumn();
        levelColumn = new DataGridViewTextBoxColumn();
        messageColumn = new DataGridViewTextBoxColumn();
        logsBindingSource = new BindingSource(components);
        mainStatusStrip = new StatusStrip();
        appNameStatusLabel = new ToolStripStatusLabel();
        versionStatusLabel = new ToolStripStatusLabel();
        trayIcon = new NotifyIcon(components);
        trayMenu = new ContextMenuStrip(components);
        trayOpenMenuItem = new ToolStripMenuItem();
        trayExitMenuItem = new ToolStripMenuItem();
        ((ISupportInitialize)headerIconPictureBox).BeginInit();
        ((ISupportInitialize)logGridView).BeginInit();
        mainLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        headerLayout.SuspendLayout();
        headerTextLayout.SuspendLayout();
        connectionGroup.SuspendLayout();
        connectionLayout.SuspendLayout();
        settingsGroup.SuspendLayout();
        settingsLayout.SuspendLayout();
        settingsButtonsPanel.SuspendLayout();
        updatesGroup.SuspendLayout();
        updatesLayout.SuspendLayout();
        updatesButtonsPanel.SuspendLayout();
        logsGroup.SuspendLayout();
        mainStatusStrip.SuspendLayout();
        trayMenu.SuspendLayout();
        SuspendLayout();
        // 
        // mainLayout
        // 
        mainLayout.BackColor = Color.FromArgb(245, 248, 252);
        mainLayout.ColumnCount = 1;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(contentLayout, 0, 1);
        mainLayout.Controls.Add(mainStatusStrip, 0, 2);
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Location = new Point(0, 0);
        mainLayout.Name = "mainLayout";
        mainLayout.Padding = new Padding(20, 20, 20, 12);
        mainLayout.RowCount = 3;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        mainLayout.Size = new Size(1160, 680);
        // 
        // headerPanel
        // 
        headerPanel.BackColor = Color.FromArgb(26, 38, 55);
        headerPanel.Controls.Add(headerLayout);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Margin = new Padding(0, 0, 0, 16);
        headerPanel.Name = "headerPanel";
        headerPanel.Padding = new Padding(24, 18, 24, 18);
        headerPanel.Size = new Size(1120, 124);
        // 
        // headerLayout
        // 
        headerLayout.ColumnCount = 2;
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.Controls.Add(headerIconPictureBox, 0, 0);
        headerLayout.Controls.Add(headerTextLayout, 1, 0);
        headerLayout.Dock = DockStyle.Fill;
        headerLayout.Location = new Point(24, 18);
        headerLayout.Margin = new Padding(0);
        headerLayout.Name = "headerLayout";
        headerLayout.RowCount = 1;
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        headerLayout.Size = new Size(1112, 88);
        // 
        // headerIconPictureBox
        // 
        headerIconPictureBox.Image = AppBranding.GetHeaderImage();
        headerIconPictureBox.Location = new Point(0, 0);
        headerIconPictureBox.Margin = new Padding(0, 0, 24, 0);
        headerIconPictureBox.Name = "headerIconPictureBox";
        headerIconPictureBox.Size = new Size(88, 88);
        headerIconPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        headerIconPictureBox.TabIndex = 0;
        headerIconPictureBox.TabStop = false;
        // 
        // headerTextLayout
        // 
        headerTextLayout.ColumnCount = 1;
        headerTextLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerTextLayout.Controls.Add(headerTitleLabel, 0, 0);
        headerTextLayout.Controls.Add(headerSubtitleLabel, 0, 1);
        headerTextLayout.Controls.Add(headerBadgesPanel, 0, 2);
        headerTextLayout.Dock = DockStyle.Fill;
        headerTextLayout.Location = new Point(112, 0);
        headerTextLayout.Margin = new Padding(0);
        headerTextLayout.Name = "headerTextLayout";
        headerTextLayout.RowCount = 3;
        headerTextLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerTextLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerTextLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerTextLayout.Size = new Size(1000, 88);
        // 
        // headerTitleLabel
        // 
        headerTitleLabel.AutoSize = true;
        headerTitleLabel.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold, GraphicsUnit.Point);
        headerTitleLabel.ForeColor = Color.White;
        headerTitleLabel.Location = new Point(0, 0);
        headerTitleLabel.Margin = new Padding(0);
        headerTitleLabel.Name = "headerTitleLabel";
        headerTitleLabel.Size = new Size(305, 37);
        headerTitleLabel.Text = "Microlux ERG-Connect";
        // 
        // headerSubtitleLabel
        // 
        headerSubtitleLabel.AutoSize = true;
        headerSubtitleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
        headerSubtitleLabel.ForeColor = Color.FromArgb(189, 206, 223);
        headerSubtitleLabel.Location = new Point(0, 43);
        headerSubtitleLabel.Margin = new Padding(0, 6, 0, 12);
        headerSubtitleLabel.Name = "headerSubtitleLabel";
        headerSubtitleLabel.Size = new Size(363, 20);
        headerSubtitleLabel.Text = "Мониторинг и синхронизация оборудования Microlux";
        // 
        // headerBadgesPanel
        // 
        headerBadgesPanel.AutoSize = true;
        headerBadgesPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        headerBadgesPanel.Controls.Add(headerStatusLabel);
        headerBadgesPanel.Controls.Add(headerDeviceLabel);
        headerBadgesPanel.Controls.Add(headerPortLabel);
        headerBadgesPanel.Controls.Add(headerSyncLabel);
        headerBadgesPanel.Dock = DockStyle.Fill;
        headerBadgesPanel.FlowDirection = FlowDirection.LeftToRight;
        headerBadgesPanel.Location = new Point(0, 75);
        headerBadgesPanel.Margin = new Padding(0, 10, 0, 0);
        headerBadgesPanel.Name = "headerBadgesPanel";
        headerBadgesPanel.Padding = new Padding(0);
        headerBadgesPanel.WrapContents = true;
        // 
        // headerStatusLabel
        // 
        headerStatusLabel.AutoSize = true;
        headerStatusLabel.BackColor = Color.FromArgb(48, 149, 177);
        headerStatusLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        headerStatusLabel.ForeColor = Color.White;
        headerStatusLabel.Location = new Point(0, 0);
        headerStatusLabel.Margin = new Padding(0, 0, 12, 8);
        headerStatusLabel.Name = "headerStatusLabel";
        headerStatusLabel.Padding = new Padding(12, 6, 12, 6);
        headerStatusLabel.Size = new Size(88, 25);
        headerStatusLabel.Text = "Статус: -";
        // 
        // headerDeviceLabel
        // 
        headerDeviceLabel.AutoSize = true;
        headerDeviceLabel.BackColor = Color.FromArgb(60, 87, 119);
        headerDeviceLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        headerDeviceLabel.ForeColor = Color.White;
        headerDeviceLabel.Location = new Point(100, 0);
        headerDeviceLabel.Margin = new Padding(0, 0, 12, 8);
        headerDeviceLabel.Name = "headerDeviceLabel";
        headerDeviceLabel.Padding = new Padding(12, 6, 12, 6);
        headerDeviceLabel.Size = new Size(111, 25);
        headerDeviceLabel.Text = "Устройство: -";
        // 
        // headerPortLabel
        // 
        headerPortLabel.AutoSize = true;
        headerPortLabel.BackColor = Color.FromArgb(74, 102, 135);
        headerPortLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        headerPortLabel.ForeColor = Color.White;
        headerPortLabel.Location = new Point(223, 0);
        headerPortLabel.Margin = new Padding(0, 0, 12, 8);
        headerPortLabel.Name = "headerPortLabel";
        headerPortLabel.Padding = new Padding(12, 6, 12, 6);
        headerPortLabel.Size = new Size(78, 25);
        headerPortLabel.Text = "Порт: -";
        // 
        // headerSyncLabel
        // 
        headerSyncLabel.AutoSize = true;
        headerSyncLabel.BackColor = Color.FromArgb(48, 149, 177);
        headerSyncLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        headerSyncLabel.ForeColor = Color.White;
        headerSyncLabel.Location = new Point(311, 0);
        headerSyncLabel.Margin = new Padding(0, 0, 12, 8);
        headerSyncLabel.Name = "headerSyncLabel";
        headerSyncLabel.Padding = new Padding(12, 6, 12, 6);
        headerSyncLabel.Size = new Size(126, 25);
        headerSyncLabel.Text = "Синхронизация: -";
        // 
        // contentLayout
        // 
        contentLayout.BackColor = Color.Transparent;
        contentLayout.ColumnCount = 2;
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        contentLayout.Controls.Add(detailsContainer, 0, 0);
        contentLayout.Controls.Add(logsGroup, 1, 0);
        contentLayout.Dock = DockStyle.Fill;
        contentLayout.Location = new Point(0, 140);
        contentLayout.Margin = new Padding(0, 0, 0, 16);
        contentLayout.Name = "contentLayout";
        contentLayout.RowCount = 1;
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        contentLayout.Size = new Size(1160, 514);
        // 
        // detailsContainer
        // 
        detailsContainer.AutoScroll = true;
        detailsContainer.AutoScrollMargin = new Size(0, 16);
        detailsContainer.Controls.Add(detailsLayout);
        detailsContainer.Dock = DockStyle.Fill;
        detailsContainer.Location = new Point(0, 0);
        detailsContainer.Margin = new Padding(0);
        detailsContainer.MinimumSize = new Size(320, 0);
        detailsContainer.Name = "detailsContainer";
        detailsContainer.Size = new Size(440, 514);
        // 
        // detailsLayout
        // 
        detailsLayout.AutoSize = true;
        detailsLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        detailsLayout.ColumnCount = 1;
        detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        detailsLayout.Controls.Add(connectionGroup, 0, 0);
        detailsLayout.Controls.Add(settingsGroup, 0, 1);
        detailsLayout.Controls.Add(updatesGroup, 0, 2);
        detailsLayout.Dock = DockStyle.Top;
        detailsLayout.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
        detailsLayout.Location = new Point(0, 0);
        detailsLayout.Margin = new Padding(0);
        detailsLayout.Name = "detailsLayout";
        detailsLayout.Padding = new Padding(0);
        detailsLayout.RowCount = 3;
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.Size = new Size(424, 0);
        // 
        // connectionGroup
        // 
        connectionGroup.AutoSize = true;
        connectionGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        connectionGroup.Controls.Add(connectionLayout);
        connectionGroup.Dock = DockStyle.Top;
        connectionGroup.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        connectionGroup.ForeColor = Color.FromArgb(33, 37, 41);
        connectionGroup.Location = new Point(0, 0);
        connectionGroup.Margin = new Padding(0, 0, 0, 16);
        connectionGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        connectionGroup.BackColor = Color.White;
        connectionGroup.Name = "connectionGroup";
        connectionGroup.Padding = new Padding(16, 20, 16, 16);
        connectionGroup.Size = new Size(424, 0);
        connectionGroup.TabStop = false;
        connectionGroup.Text = "Подключение";
        // 
        // connectionLayout
        // 
        connectionLayout.AutoSize = true;
        connectionLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        connectionLayout.BackColor = Color.White;
        connectionLayout.ColumnCount = 2;
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
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
        connectionLayout.Controls.Add(resetPortButton, 0, 6);
        connectionLayout.Dock = DockStyle.Fill;
        connectionLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        connectionLayout.Location = new Point(16, 38);
        connectionLayout.Margin = new Padding(0);
        connectionLayout.Name = "connectionLayout";
        connectionLayout.Padding = new Padding(0, 4, 0, 8);
        connectionLayout.RowCount = 7;
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        connectionLayout.Size = new Size(392, 0);
        // 
        // statusCaptionLabel
        // 
        statusCaptionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        statusCaptionLabel.AutoSize = true;
        statusCaptionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        statusCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        statusCaptionLabel.Location = new Point(0, 4);
        statusCaptionLabel.Margin = new Padding(0, 0, 16, 0);
        statusCaptionLabel.Name = "statusCaptionLabel";
        statusCaptionLabel.Size = new Size(46, 15);
        statusCaptionLabel.Text = "Статус:";
        // 
        // statusValueLabel
        // 
        statusValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        statusValueLabel.AutoSize = true;
        statusValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        statusValueLabel.ForeColor = Color.FromArgb(34, 158, 189);
        statusValueLabel.Location = new Point(62, 10);
        statusValueLabel.Margin = new Padding(8, 6, 0, 4);
        statusValueLabel.MinimumSize = new Size(0, 24);
        statusValueLabel.Name = "statusValueLabel";
        statusValueLabel.Size = new Size(0, 24);
        statusValueLabel.Text = string.Empty;
        // 
        // portCaptionLabel
        // 
        portCaptionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        portCaptionLabel.AutoSize = true;
        portCaptionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        portCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        portCaptionLabel.Location = new Point(0, 42);
        portCaptionLabel.Margin = new Padding(0, 0, 16, 0);
        portCaptionLabel.Name = "portCaptionLabel";
        portCaptionLabel.Size = new Size(35, 15);
        portCaptionLabel.Text = "Порт:";
        // 
        // portValueLabel
        // 
        portValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        portValueLabel.AutoSize = true;
        portValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        portValueLabel.ForeColor = Color.FromArgb(33, 37, 41);
        portValueLabel.Location = new Point(62, 48);
        portValueLabel.Margin = new Padding(8, 6, 0, 4);
        portValueLabel.MinimumSize = new Size(0, 24);
        portValueLabel.Name = "portValueLabel";
        portValueLabel.Size = new Size(0, 24);
        portValueLabel.Text = string.Empty;
        // 
        // deviceCaptionLabel
        // 
        deviceCaptionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        deviceCaptionLabel.AutoSize = true;
        deviceCaptionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        deviceCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        deviceCaptionLabel.Location = new Point(0, 80);
        deviceCaptionLabel.Margin = new Padding(0, 0, 16, 0);
        deviceCaptionLabel.Name = "deviceCaptionLabel";
        deviceCaptionLabel.Size = new Size(56, 15);
        deviceCaptionLabel.Text = "Прибор:";
        // 
        // deviceValueLabel
        // 
        deviceValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        deviceValueLabel.AutoSize = true;
        deviceValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        deviceValueLabel.ForeColor = Color.FromArgb(33, 37, 41);
        deviceValueLabel.Location = new Point(62, 86);
        deviceValueLabel.Margin = new Padding(8, 6, 0, 4);
        deviceValueLabel.MinimumSize = new Size(0, 24);
        deviceValueLabel.Name = "deviceValueLabel";
        deviceValueLabel.Size = new Size(0, 24);
        deviceValueLabel.Text = string.Empty;
        // 
        // softwareCaptionLabel
        // 
        softwareCaptionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        softwareCaptionLabel.AutoSize = true;
        softwareCaptionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        softwareCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        softwareCaptionLabel.Location = new Point(0, 118);
        softwareCaptionLabel.Margin = new Padding(0, 0, 16, 0);
        softwareCaptionLabel.Name = "softwareCaptionLabel";
        softwareCaptionLabel.Size = new Size(28, 15);
        softwareCaptionLabel.Text = "ПО:";
        // 
        // softwareValueLabel
        // 
        softwareValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        softwareValueLabel.AutoSize = true;
        softwareValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        softwareValueLabel.ForeColor = Color.FromArgb(33, 37, 41);
        softwareValueLabel.Location = new Point(62, 124);
        softwareValueLabel.Margin = new Padding(8, 6, 0, 4);
        softwareValueLabel.MinimumSize = new Size(0, 24);
        softwareValueLabel.Name = "softwareValueLabel";
        softwareValueLabel.Size = new Size(0, 24);
        softwareValueLabel.Text = string.Empty;
        // 
        // reportCaptionLabel
        // 
        reportCaptionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        reportCaptionLabel.AutoSize = true;
        reportCaptionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        reportCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        reportCaptionLabel.Location = new Point(0, 156);
        reportCaptionLabel.Margin = new Padding(0, 0, 16, 0);
        reportCaptionLabel.Name = "reportCaptionLabel";
        reportCaptionLabel.Size = new Size(45, 15);
        reportCaptionLabel.Text = "Отчет:";
        // 
        // reportValueLabel
        // 
        reportValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        reportValueLabel.AutoSize = true;
        reportValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        reportValueLabel.ForeColor = Color.FromArgb(33, 37, 41);
        reportValueLabel.Location = new Point(62, 162);
        reportValueLabel.Margin = new Padding(8, 6, 0, 4);
        reportValueLabel.MinimumSize = new Size(0, 24);
        reportValueLabel.Name = "reportValueLabel";
        reportValueLabel.Size = new Size(0, 24);
        reportValueLabel.Text = string.Empty;
        // 
        // syncStatusLabel
        // 
        syncStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        syncStatusLabel.AutoSize = true;
        syncStatusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        syncStatusLabel.ForeColor = Color.FromArgb(54, 127, 151);
        syncStatusLabel.Location = new Point(0, 200);
        syncStatusLabel.Margin = new Padding(0, 14, 0, 4);
        syncStatusLabel.Name = "syncStatusLabel";
        syncStatusLabel.Size = new Size(0, 15);
        syncStatusLabel.Text = string.Empty;
        connectionLayout.SetColumnSpan(syncStatusLabel, 2);
        // 
        // resetPortButton
        // 
        resetPortButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        resetPortButton.AutoSize = true;
        resetPortButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        resetPortButton.BackColor = Color.FromArgb(34, 158, 189);
        resetPortButton.Cursor = Cursors.Hand;
        resetPortButton.FlatAppearance.BorderSize = 0;
        resetPortButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 138, 166);
        resetPortButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 182, 214);
        resetPortButton.FlatStyle = FlatStyle.Flat;
        resetPortButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        resetPortButton.ForeColor = Color.White;
        resetPortButton.Location = new Point(0, 219);
        resetPortButton.Margin = new Padding(0, 16, 0, 0);
        resetPortButton.MinimumSize = new Size(140, 36);
        resetPortButton.Name = "resetPortButton";
        resetPortButton.Padding = new Padding(10, 6, 10, 6);
        resetPortButton.Size = new Size(140, 36);
        resetPortButton.Text = "Сбросить порт";
        resetPortButton.UseVisualStyleBackColor = false;
        resetPortButton.Click += OnResetPortClicked;
        connectionLayout.SetColumnSpan(resetPortButton, 2);
        // 
        // settingsGroup
        // 
        settingsGroup.AutoSize = true;
        settingsGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        settingsGroup.Controls.Add(settingsLayout);
        settingsGroup.Dock = DockStyle.Top;
        settingsGroup.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        settingsGroup.ForeColor = Color.FromArgb(33, 37, 41);
        settingsGroup.Location = new Point(0, 0);
        settingsGroup.Margin = new Padding(0, 0, 0, 16);
        settingsGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        settingsGroup.BackColor = Color.White;
        settingsGroup.Name = "settingsGroup";
        settingsGroup.Padding = new Padding(16, 20, 16, 16);
        settingsGroup.Size = new Size(424, 0);
        settingsGroup.TabStop = false;
        settingsGroup.Text = "Настройки опроса";
        // 
        // settingsLayout
        // 
        settingsLayout.AutoSize = true;
        settingsLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        settingsLayout.BackColor = Color.White;
        settingsLayout.ColumnCount = 2;
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        settingsLayout.Controls.Add(scanIntervalLabel, 0, 0);
        settingsLayout.Controls.Add(scanIntervalTextBox, 1, 0);
        settingsLayout.Controls.Add(reconnectDelayLabel, 0, 1);
        settingsLayout.Controls.Add(reconnectDelayTextBox, 1, 1);
        settingsLayout.Controls.Add(backgroundSyncLabel, 0, 2);
        settingsLayout.Controls.Add(backgroundSyncTextBox, 1, 2);
        settingsLayout.Controls.Add(settingsButtonsPanel, 0, 3);
        settingsLayout.Dock = DockStyle.Fill;
        settingsLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        settingsLayout.Location = new Point(16, 38);
        settingsLayout.Margin = new Padding(0);
        settingsLayout.Name = "settingsLayout";
        settingsLayout.Padding = new Padding(0, 4, 0, 8);
        settingsLayout.RowCount = 4;
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settingsLayout.Size = new Size(392, 0);
        // 
        // scanIntervalLabel
        // 
        scanIntervalLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        scanIntervalLabel.AutoSize = true;
        scanIntervalLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        scanIntervalLabel.ForeColor = Color.FromArgb(120, 128, 145);
        scanIntervalLabel.Location = new Point(0, 4);
        scanIntervalLabel.Margin = new Padding(0, 0, 16, 0);
        scanIntervalLabel.Name = "scanIntervalLabel";
        scanIntervalLabel.Size = new Size(136, 15);
        scanIntervalLabel.Text = "Интервал поиска (с):";
        // 
        // scanIntervalTextBox
        // 
        scanIntervalTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        scanIntervalTextBox.BorderStyle = BorderStyle.FixedSingle;
        scanIntervalTextBox.Location = new Point(152, 4);
        scanIntervalTextBox.Margin = new Padding(0, 4, 0, 0);
        scanIntervalTextBox.MaximumSize = new Size(160, 0);
        scanIntervalTextBox.MinimumSize = new Size(80, 0);
        scanIntervalTextBox.Name = "scanIntervalTextBox";
        scanIntervalTextBox.Size = new Size(80, 23);
        scanIntervalTextBox.TextAlign = HorizontalAlignment.Center;
        scanIntervalTextBox.Validating += OnNumericTextBoxValidating;
        scanIntervalTextBox.Validated += OnScanIntervalValidated;
        // 
        // reconnectDelayLabel
        // 
        reconnectDelayLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        reconnectDelayLabel.AutoSize = true;
        reconnectDelayLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        reconnectDelayLabel.ForeColor = Color.FromArgb(120, 128, 145);
        reconnectDelayLabel.Location = new Point(0, 42);
        reconnectDelayLabel.Margin = new Padding(0, 10, 16, 0);
        reconnectDelayLabel.Name = "reconnectDelayLabel";
        reconnectDelayLabel.Size = new Size(170, 15);
        reconnectDelayLabel.Text = "Задержка перепроверки (с):";
        // 
        // reconnectDelayTextBox
        // 
        reconnectDelayTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        reconnectDelayTextBox.BorderStyle = BorderStyle.FixedSingle;
        reconnectDelayTextBox.Location = new Point(152, 46);
        reconnectDelayTextBox.Margin = new Padding(0, 4, 0, 0);
        reconnectDelayTextBox.MaximumSize = new Size(160, 0);
        reconnectDelayTextBox.MinimumSize = new Size(80, 0);
        reconnectDelayTextBox.Name = "reconnectDelayTextBox";
        reconnectDelayTextBox.Size = new Size(80, 23);
        reconnectDelayTextBox.TextAlign = HorizontalAlignment.Center;
        reconnectDelayTextBox.Validating += OnNumericTextBoxValidating;
        reconnectDelayTextBox.Validated += OnReconnectDelayValidated;
        // 
        // backgroundSyncLabel
        // 
        backgroundSyncLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        backgroundSyncLabel.AutoSize = true;
        backgroundSyncLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        backgroundSyncLabel.ForeColor = Color.FromArgb(120, 128, 145);
        backgroundSyncLabel.Location = new Point(0, 80);
        backgroundSyncLabel.Margin = new Padding(0, 10, 16, 0);
        backgroundSyncLabel.Name = "backgroundSyncLabel";
        backgroundSyncLabel.Size = new Size(170, 15);
        backgroundSyncLabel.Text = "Период синхронизации (мин):";
        // 
        // backgroundSyncTextBox
        // 
        backgroundSyncTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        backgroundSyncTextBox.BorderStyle = BorderStyle.FixedSingle;
        backgroundSyncTextBox.Location = new Point(152, 84);
        backgroundSyncTextBox.Margin = new Padding(0, 4, 0, 0);
        backgroundSyncTextBox.MaximumSize = new Size(160, 0);
        backgroundSyncTextBox.MinimumSize = new Size(80, 0);
        backgroundSyncTextBox.Name = "backgroundSyncTextBox";
        backgroundSyncTextBox.Size = new Size(80, 23);
        backgroundSyncTextBox.TextAlign = HorizontalAlignment.Center;
        backgroundSyncTextBox.Validating += OnNumericTextBoxValidating;
        backgroundSyncTextBox.Validated += OnBackgroundSyncValidated;
        // 
        // settingsButtonsPanel
        // 
        settingsButtonsPanel.AutoSize = true;
        settingsButtonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        settingsButtonsPanel.Controls.Add(openReportsButton);
        settingsButtonsPanel.Controls.Add(convertBinButton);
        settingsButtonsPanel.FlowDirection = FlowDirection.LeftToRight;
        settingsButtonsPanel.Location = new Point(0, 121);
        settingsButtonsPanel.Margin = new Padding(0, 14, 0, 0);
        settingsButtonsPanel.Name = "settingsButtonsPanel";
        settingsButtonsPanel.Size = new Size(329, 36);
        settingsLayout.SetColumnSpan(settingsButtonsPanel, 2);
        // 
        // openReportsButton
        // 
        openReportsButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        openReportsButton.AutoSize = true;
        openReportsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        openReportsButton.BackColor = Color.FromArgb(239, 246, 249);
        openReportsButton.Cursor = Cursors.Hand;
        openReportsButton.FlatAppearance.BorderSize = 0;
        openReportsButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 228, 236);
        openReportsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 236, 242);
        openReportsButton.FlatStyle = FlatStyle.Flat;
        openReportsButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        openReportsButton.ForeColor = Color.FromArgb(33, 37, 41);
        openReportsButton.Location = new Point(0, 0);
        openReportsButton.Margin = new Padding(0, 0, 12, 0);
        openReportsButton.MinimumSize = new Size(140, 36);
        openReportsButton.Name = "openReportsButton";
        openReportsButton.Padding = new Padding(10, 6, 10, 6);
        openReportsButton.Size = new Size(197, 36);
        openReportsButton.Text = "Открыть каталог отчетов";
        openReportsButton.UseVisualStyleBackColor = false;
        openReportsButton.Click += OnOpenReportsClicked;
        // 
        // convertBinButton
        // 
        convertBinButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        convertBinButton.AutoSize = true;
        convertBinButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        convertBinButton.BackColor = Color.FromArgb(34, 158, 189);
        convertBinButton.Cursor = Cursors.Hand;
        convertBinButton.FlatAppearance.BorderSize = 0;
        convertBinButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 138, 166);
        convertBinButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 182, 214);
        convertBinButton.FlatStyle = FlatStyle.Flat;
        convertBinButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        convertBinButton.ForeColor = Color.White;
        convertBinButton.Location = new Point(209, 0);
        convertBinButton.Margin = new Padding(0);
        convertBinButton.MinimumSize = new Size(140, 36);
        convertBinButton.Name = "convertBinButton";
        convertBinButton.Padding = new Padding(10, 6, 10, 6);
        convertBinButton.Size = new Size(120, 36);
        convertBinButton.Text = "Конвертировать .bin в отчет";
        convertBinButton.UseVisualStyleBackColor = false;
        convertBinButton.Click += OnConvertBinClicked;
        // 
        // updatesGroup
        // 
        updatesGroup.AutoSize = true;
        updatesGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        updatesGroup.Controls.Add(updatesLayout);
        updatesGroup.Dock = DockStyle.Top;
        updatesGroup.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        updatesGroup.ForeColor = Color.FromArgb(33, 37, 41);
        updatesGroup.Location = new Point(0, 0);
        updatesGroup.Margin = new Padding(0);
        updatesGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        updatesGroup.BackColor = Color.White;
        updatesGroup.Name = "updatesGroup";
        updatesGroup.Padding = new Padding(16, 20, 16, 16);
        updatesGroup.Size = new Size(424, 0);
        updatesGroup.TabStop = false;
        updatesGroup.Text = "Обновления";
        // 
        // updatesLayout
        // 
        updatesLayout.AutoSize = true;
        updatesLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        updatesLayout.BackColor = Color.White;
        updatesLayout.ColumnCount = 2;
        updatesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        updatesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        updatesLayout.Controls.Add(updateStatusCaptionLabel, 0, 0);
        updatesLayout.Controls.Add(updateStatusValueLabel, 0, 1);
        updatesLayout.Controls.Add(updatesButtonsPanel, 0, 2);
        updatesLayout.Controls.Add(updateIntervalLabel, 0, 3);
        updatesLayout.Controls.Add(updateIntervalTextBox, 1, 3);
        updatesLayout.Controls.Add(manifestUrlLabel, 0, 4);
        updatesLayout.Controls.Add(manifestUrlTextBox, 1, 4);
        updatesLayout.Controls.Add(openLogsButton, 0, 5);
        updatesLayout.Dock = DockStyle.Fill;
        updatesLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        updatesLayout.Location = new Point(16, 38);
        updatesLayout.Margin = new Padding(0);
        updatesLayout.Name = "updatesLayout";
        updatesLayout.Padding = new Padding(0, 4, 0, 8);
        updatesLayout.RowCount = 6;
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        updatesLayout.Size = new Size(392, 0);
        // 
        // updateStatusCaptionLabel
        // 
        updateStatusCaptionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        updateStatusCaptionLabel.AutoSize = true;
        updateStatusCaptionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        updateStatusCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        updateStatusCaptionLabel.Location = new Point(0, 4);
        updateStatusCaptionLabel.Margin = new Padding(0, 0, 16, 0);
        updateStatusCaptionLabel.Name = "updateStatusCaptionLabel";
        updateStatusCaptionLabel.Size = new Size(115, 15);
        updateStatusCaptionLabel.Text = "Статус обновлений:";
        // 
        // updateStatusValueLabel
        // 
        updateStatusValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        updateStatusValueLabel.AutoSize = false;
        updateStatusValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        updateStatusValueLabel.ForeColor = Color.FromArgb(33, 37, 41);
        updateStatusValueLabel.Location = new Point(0, 23);
        updateStatusValueLabel.Margin = new Padding(0, 6, 0, 4);
        updateStatusValueLabel.MinimumSize = new Size(0, 24);
        updateStatusValueLabel.MaximumSize = new Size(0, 24);
        updateStatusValueLabel.Name = "updateStatusValueLabel";
        updateStatusValueLabel.Size = new Size(392, 24);
        updateStatusValueLabel.Text = string.Empty;
        updateStatusValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        updateStatusValueLabel.AutoEllipsis = true;
        updateStatusValueLabel.Dock = DockStyle.Fill;
        updatesLayout.SetColumnSpan(updateStatusValueLabel, 2);
        // 
        // updatesButtonsPanel
        // 
        updatesButtonsPanel.AutoSize = true;
        updatesButtonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        updatesButtonsPanel.Controls.Add(checkUpdatesButton);
        updatesButtonsPanel.Controls.Add(installUpdateButton);
        updatesButtonsPanel.FlowDirection = FlowDirection.LeftToRight;
        updatesButtonsPanel.Location = new Point(0, 53);
        updatesButtonsPanel.Margin = new Padding(0, 10, 0, 6);
        updatesButtonsPanel.Name = "updatesButtonsPanel";
        updatesButtonsPanel.Size = new Size(218, 36);
        updatesLayout.SetColumnSpan(updatesButtonsPanel, 2);
        // 
        // checkUpdatesButton
        // 
        checkUpdatesButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        checkUpdatesButton.AutoSize = true;
        checkUpdatesButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        checkUpdatesButton.BackColor = Color.FromArgb(239, 246, 249);
        checkUpdatesButton.Cursor = Cursors.Hand;
        checkUpdatesButton.FlatAppearance.BorderSize = 0;
        checkUpdatesButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 228, 236);
        checkUpdatesButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 236, 242);
        checkUpdatesButton.FlatStyle = FlatStyle.Flat;
        checkUpdatesButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        checkUpdatesButton.ForeColor = Color.FromArgb(33, 37, 41);
        checkUpdatesButton.Location = new Point(0, 0);
        checkUpdatesButton.Margin = new Padding(0, 0, 12, 0);
        checkUpdatesButton.MinimumSize = new Size(140, 36);
        checkUpdatesButton.Name = "checkUpdatesButton";
        checkUpdatesButton.Padding = new Padding(10, 6, 10, 6);
        checkUpdatesButton.Size = new Size(164, 36);
        checkUpdatesButton.Text = "Проверить";
        checkUpdatesButton.UseVisualStyleBackColor = false;
        checkUpdatesButton.Click += OnCheckUpdatesClicked;
        // 
        // installUpdateButton
        // 
        installUpdateButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        installUpdateButton.AutoSize = true;
        installUpdateButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        installUpdateButton.BackColor = Color.FromArgb(34, 158, 189);
        installUpdateButton.Cursor = Cursors.Hand;
        installUpdateButton.FlatAppearance.BorderSize = 0;
        installUpdateButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 138, 166);
        installUpdateButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 182, 214);
        installUpdateButton.FlatStyle = FlatStyle.Flat;
        installUpdateButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        installUpdateButton.ForeColor = Color.White;
        installUpdateButton.Location = new Point(176, 0);
        installUpdateButton.Margin = new Padding(0);
        installUpdateButton.MinimumSize = new Size(140, 36);
        installUpdateButton.Name = "installUpdateButton";
        installUpdateButton.Padding = new Padding(10, 6, 10, 6);
        installUpdateButton.Size = new Size(86, 36);
        installUpdateButton.Text = "Установить";
        installUpdateButton.UseVisualStyleBackColor = false;
        installUpdateButton.Click += OnInstallUpdateClicked;
        // 
        // updateIntervalLabel
        // 
        updateIntervalLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        updateIntervalLabel.AutoSize = true;
        updateIntervalLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        updateIntervalLabel.ForeColor = Color.FromArgb(120, 128, 145);
        updateIntervalLabel.Location = new Point(0, 95);
        updateIntervalLabel.Margin = new Padding(0, 12, 16, 0);
        updateIntervalLabel.Name = "updateIntervalLabel";
        updateIntervalLabel.Size = new Size(162, 15);
        updateIntervalLabel.Text = "Интервал проверки (мин):";
        // 
        // updateIntervalTextBox
        // 
        updateIntervalTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        updateIntervalTextBox.BorderStyle = BorderStyle.FixedSingle;
        updateIntervalTextBox.Location = new Point(178, 99);
        updateIntervalTextBox.Margin = new Padding(0, 4, 0, 0);
        updateIntervalTextBox.MinimumSize = new Size(180, 0);
        updateIntervalTextBox.Name = "updateIntervalTextBox";
        updateIntervalTextBox.Size = new Size(180, 23);
        updateIntervalTextBox.TextAlign = HorizontalAlignment.Center;
        updateIntervalTextBox.Validating += OnNumericTextBoxValidating;
        updateIntervalTextBox.Validated += OnUpdateIntervalValidated;
        // 
        // manifestUrlLabel
        // 
        manifestUrlLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        manifestUrlLabel.AutoSize = true;
        manifestUrlLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        manifestUrlLabel.ForeColor = Color.FromArgb(120, 128, 145);
        manifestUrlLabel.Location = new Point(0, 134);
        manifestUrlLabel.Margin = new Padding(0, 12, 16, 0);
        manifestUrlLabel.Name = "manifestUrlLabel";
        manifestUrlLabel.Size = new Size(106, 15);
        manifestUrlLabel.Text = "URL манифеста:";
        // 
        // manifestUrlTextBox
        // 
        manifestUrlTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        manifestUrlTextBox.BorderStyle = BorderStyle.FixedSingle;
        manifestUrlTextBox.Location = new Point(178, 138);
        manifestUrlTextBox.Margin = new Padding(0, 4, 0, 0);
        manifestUrlTextBox.MinimumSize = new Size(180, 0);
        manifestUrlTextBox.Name = "manifestUrlTextBox";
        manifestUrlTextBox.Size = new Size(214, 23);
        manifestUrlTextBox.Validated += OnManifestUrlValidated;
        // 
        // openLogsButton
        // 
        openLogsButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        openLogsButton.AutoSize = true;
        openLogsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        openLogsButton.BackColor = Color.FromArgb(239, 246, 249);
        openLogsButton.Cursor = Cursors.Hand;
        openLogsButton.FlatAppearance.BorderSize = 0;
        openLogsButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 228, 236);
        openLogsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 236, 242);
        openLogsButton.FlatStyle = FlatStyle.Flat;
        openLogsButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        openLogsButton.ForeColor = Color.FromArgb(33, 37, 41);
        openLogsButton.Location = new Point(0, 177);
        openLogsButton.Margin = new Padding(0, 14, 0, 0);
        openLogsButton.MinimumSize = new Size(140, 36);
        openLogsButton.Name = "openLogsButton";
        openLogsButton.Padding = new Padding(10, 6, 10, 6);
        openLogsButton.Size = new Size(146, 36);
        openLogsButton.Text = "Открыть каталог логов";
        openLogsButton.UseVisualStyleBackColor = false;
        openLogsButton.Click += OnOpenLogsClicked;
        updatesLayout.SetColumnSpan(openLogsButton, 2);
        // 
        // logsGroup
        // 
        logsGroup.BackColor = Color.White;
        logsGroup.Controls.Add(logGridView);
        logsGroup.Dock = DockStyle.Fill;
        logsGroup.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        logsGroup.ForeColor = Color.FromArgb(33, 37, 41);
        logsGroup.Location = new Point(456, 0);
        logsGroup.Margin = new Padding(24, 0, 0, 0);
        logsGroup.Name = "logsGroup";
        logsGroup.Padding = new Padding(16, 20, 16, 16);
        logsGroup.Size = new Size(704, 514);
        logsGroup.TabStop = false;
        logsGroup.Text = "Журнал";
        // 
        // logGridView
        // 
        logGridView.AllowUserToAddRows = false;
        logGridView.AllowUserToDeleteRows = false;
        logGridView.AllowUserToResizeRows = false;
        logGridView.AutoGenerateColumns = false;
        logGridView.BackgroundColor = Color.White;
        logGridView.BorderStyle = BorderStyle.None;
        logGridView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        logGridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        logGridView.ColumnHeadersHeight = 36;
        logGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        logGridView.Columns.AddRange(new DataGridViewColumn[] { timestampColumn, levelColumn, messageColumn });
        logGridView.DataSource = logsBindingSource;
        logGridView.Dock = DockStyle.Fill;
        logGridView.EnableHeadersVisualStyles = false;
        logGridView.GridColor = Color.FromArgb(230, 236, 244);
        logGridView.Location = new Point(16, 38);
        logGridView.Margin = new Padding(0);
        logGridView.MultiSelect = false;
        logGridView.Name = "logGridView";
        logGridView.ReadOnly = true;
        logGridView.RowHeadersVisible = false;
        logGridView.RowTemplate.Height = 28;
        logGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        logGridView.ShowCellErrors = false;
        logGridView.ShowEditingIcon = false;
        logGridView.ShowRowErrors = false;
        logGridView.Size = new Size(672, 460);
        logGridView.TabIndex = 0;
        logGridView.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(33, 37, 41),
            SelectionBackColor = Color.FromArgb(220, 244, 247),
            SelectionForeColor = Color.FromArgb(33, 37, 41),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            WrapMode = DataGridViewTriState.False
        };
        logGridView.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(26, 38, 55),
            ForeColor = Color.White,
            SelectionBackColor = Color.FromArgb(26, 38, 55),
            SelectionForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };
        logGridView.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(245, 249, 252),
            ForeColor = Color.FromArgb(33, 37, 41),
            SelectionBackColor = Color.FromArgb(210, 238, 242),
            SelectionForeColor = Color.FromArgb(33, 37, 41)
        };
        logGridView.RowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(33, 37, 41)
        };
        // 
        // timestampColumn
        // 
        timestampColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        timestampColumn.DataPropertyName = nameof(LogEntry.Timestamp);
        timestampColumn.DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" };
        timestampColumn.HeaderText = "Время";
        timestampColumn.MinimumWidth = 180;
        timestampColumn.Name = "timestampColumn";
        timestampColumn.ReadOnly = true;
        timestampColumn.Width = 180;
        // 
        // levelColumn
        // 
        levelColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        levelColumn.DataPropertyName = nameof(LogEntry.Level);
        levelColumn.HeaderText = "Уровень";
        levelColumn.MinimumWidth = 80;
        levelColumn.Name = "levelColumn";
        levelColumn.ReadOnly = true;
        levelColumn.Width = 80;
        // 
        // messageColumn
        // 
        messageColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        messageColumn.DataPropertyName = nameof(LogEntry.Message);
        messageColumn.HeaderText = "Сообщение";
        messageColumn.MinimumWidth = 100;
        messageColumn.Name = "messageColumn";
        messageColumn.ReadOnly = true;
        // 
        // logsBindingSource
        //
        // 
        // mainStatusStrip
        // 
        mainStatusStrip.BackColor = Color.FromArgb(26, 38, 55);
        mainStatusStrip.Dock = DockStyle.Fill;
        mainStatusStrip.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        mainStatusStrip.ForeColor = Color.White;
        mainStatusStrip.GripStyle = ToolStripGripStyle.Hidden;
        mainStatusStrip.ImageScalingSize = new Size(16, 16);
        mainStatusStrip.Items.AddRange(new ToolStripItem[] { appNameStatusLabel, versionStatusLabel });
        mainStatusStrip.Location = new Point(0, 656);
        mainStatusStrip.Name = "mainStatusStrip";
        mainStatusStrip.Padding = new Padding(12, 0, 12, 0);
        mainStatusStrip.RenderMode = ToolStripRenderMode.System;
        mainStatusStrip.Size = new Size(1160, 24);
        mainStatusStrip.SizingGrip = false;
        // 
        // appNameStatusLabel
        // 
        appNameStatusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        appNameStatusLabel.ForeColor = Color.White;
        appNameStatusLabel.Name = "appNameStatusLabel";
        appNameStatusLabel.Size = new Size(1021, 19);
        appNameStatusLabel.Spring = true;
        appNameStatusLabel.Text = "Microlux ERG-Connect";
        // 
        // versionStatusLabel
        // 
        versionStatusLabel.ForeColor = Color.FromArgb(189, 206, 223);
        versionStatusLabel.Name = "versionStatusLabel";
        versionStatusLabel.Size = new Size(56, 19);
        versionStatusLabel.Text = "Версия:";
        // 
        // trayMenu
        // 
        trayMenu.ImageScalingSize = new Size(24, 24);
        trayMenu.Items.AddRange(new ToolStripItem[] { trayOpenMenuItem, trayExitMenuItem });
        trayMenu.Name = "trayMenu";
        trayMenu.Size = new Size(124, 48);
        // 
        // trayOpenMenuItem
        // 
        trayOpenMenuItem.Name = "trayOpenMenuItem";
        trayOpenMenuItem.Size = new Size(123, 22);
        trayOpenMenuItem.Text = "Открыть";
        trayOpenMenuItem.Click += (_, _) => RestoreFromTray();
        // 
        // trayExitMenuItem
        // 
        trayExitMenuItem.Name = "trayExitMenuItem";
        trayExitMenuItem.Size = new Size(123, 22);
        trayExitMenuItem.Text = "Выход";
        trayExitMenuItem.Click += (_, _) => ExitApplication();
        // 
        // trayIcon
        // 
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Icon = AppBranding.CreateTrayIcon();
        trayIcon.Text = "Microlux ERG-Connect";
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(245, 248, 252);
        ClientSize = new Size(1200, 720);
        Controls.Add(mainLayout);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = AppBranding.CreateWindowIcon();
        MinimumSize = new Size(960, 620);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Microlux ERG-Connect";
        FormClosing += OnFormClosing;
        Load += OnFormLoaded;
        Resize += OnFormResized;
        ((ISupportInitialize)headerIconPictureBox).EndInit();
        ((ISupportInitialize)logGridView).EndInit();
        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        headerPanel.ResumeLayout(false);
        headerLayout.ResumeLayout(false);
        headerLayout.PerformLayout();
        headerTextLayout.ResumeLayout(false);
        headerTextLayout.PerformLayout();
        connectionGroup.ResumeLayout(false);
        connectionGroup.PerformLayout();
        connectionLayout.ResumeLayout(false);
        connectionLayout.PerformLayout();
        settingsGroup.ResumeLayout(false);
        settingsGroup.PerformLayout();
        settingsLayout.ResumeLayout(false);
        settingsLayout.PerformLayout();
        settingsButtonsPanel.ResumeLayout(false);
        settingsButtonsPanel.PerformLayout();
        updatesGroup.ResumeLayout(false);
        updatesGroup.PerformLayout();
        updatesLayout.ResumeLayout(false);
        updatesLayout.PerformLayout();
        updatesButtonsPanel.ResumeLayout(false);
        updatesButtonsPanel.PerformLayout();
        logsGroup.ResumeLayout(false);
        mainStatusStrip.ResumeLayout(false);
        mainStatusStrip.PerformLayout();
        trayMenu.ResumeLayout(false);
        ResumeLayout(false);
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
