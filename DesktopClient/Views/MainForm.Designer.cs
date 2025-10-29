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
    private Label reportTemplateLabel;
    private ComboBox reportTemplateComboBox;
    private Label renderingModeLabel;
    private ComboBox renderingModeComboBox;
    private Label reportHeaderLabel;
    private TextBox reportHeaderTextBox;
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
        ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
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
        reportTemplateLabel = new Label();
        reportTemplateComboBox = new ComboBox();
        renderingModeLabel = new Label();
        renderingModeComboBox = new ComboBox();
        reportHeaderLabel = new Label();
        reportHeaderTextBox = new TextBox();
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
        logsBindingSource = new BindingSource(components);
        mainStatusStrip = new StatusStrip();
        appNameStatusLabel = new ToolStripStatusLabel();
        versionStatusLabel = new ToolStripStatusLabel();
        trayIcon = new NotifyIcon(components);
        trayMenu = new ContextMenuStrip(components);
        trayOpenMenuItem = new ToolStripMenuItem();
        trayExitMenuItem = new ToolStripMenuItem();
        mainLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        headerLayout.SuspendLayout();
        ((ISupportInitialize)headerIconPictureBox).BeginInit();
        headerTextLayout.SuspendLayout();
        headerBadgesPanel.SuspendLayout();
        contentLayout.SuspendLayout();
        detailsContainer.SuspendLayout();
        detailsLayout.SuspendLayout();
        connectionGroup.SuspendLayout();
        connectionLayout.SuspendLayout();
        settingsGroup.SuspendLayout();
        settingsLayout.SuspendLayout();
        settingsButtonsPanel.SuspendLayout();
        updatesGroup.SuspendLayout();
        updatesLayout.SuspendLayout();
        updatesButtonsPanel.SuspendLayout();
        logsGroup.SuspendLayout();
        ((ISupportInitialize)logGridView).BeginInit();
        ((ISupportInitialize)logsBindingSource).BeginInit();
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
        mainLayout.Margin = new Padding(3, 4, 3, 4);
        mainLayout.Name = "mainLayout";
        mainLayout.Padding = new Padding(23, 27, 23, 16);
        mainLayout.RowCount = 3;
        mainLayout.RowStyles.Add(new RowStyle());
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
        mainLayout.Size = new Size(1371, 960);
        mainLayout.TabIndex = 1;
        // 
        // headerPanel
        // 
        headerPanel.BackColor = Color.FromArgb(26, 38, 55);
        headerPanel.Controls.Add(headerLayout);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Location = new Point(23, 27);
        headerPanel.Margin = new Padding(0, 0, 0, 21);
        headerPanel.Name = "headerPanel";
        headerPanel.Padding = new Padding(27, 24, 27, 24);
        headerPanel.Size = new Size(1325, 165);
        headerPanel.TabIndex = 0;
        // 
        // headerLayout
        // 
        headerLayout.ColumnCount = 2;
        headerLayout.ColumnStyles.Add(new ColumnStyle());
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.Controls.Add(headerIconPictureBox, 0, 0);
        headerLayout.Controls.Add(headerTextLayout, 1, 0);
        headerLayout.Dock = DockStyle.Fill;
        headerLayout.Location = new Point(27, 24);
        headerLayout.Margin = new Padding(0);
        headerLayout.Name = "headerLayout";
        headerLayout.RowCount = 1;
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        headerLayout.Size = new Size(1271, 117);
        headerLayout.TabIndex = 0;
        // 
        // headerIconPictureBox
        // 
        headerIconPictureBox.Image = (Image)resources.GetObject("headerIconPictureBox.Image");
        headerIconPictureBox.Location = new Point(0, 0);
        headerIconPictureBox.Margin = new Padding(0, 0, 27, 0);
        headerIconPictureBox.Name = "headerIconPictureBox";
        headerIconPictureBox.Size = new Size(101, 117);
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
        headerTextLayout.Location = new Point(128, 0);
        headerTextLayout.Margin = new Padding(0);
        headerTextLayout.Name = "headerTextLayout";
        headerTextLayout.RowCount = 3;
        headerTextLayout.RowStyles.Add(new RowStyle());
        headerTextLayout.RowStyles.Add(new RowStyle());
        headerTextLayout.RowStyles.Add(new RowStyle());
        headerTextLayout.Size = new Size(1143, 117);
        headerTextLayout.TabIndex = 1;
        // 
        // headerTitleLabel
        // 
        headerTitleLabel.AutoSize = true;
        headerTitleLabel.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
        headerTitleLabel.ForeColor = Color.White;
        headerTitleLabel.Location = new Point(0, 0);
        headerTitleLabel.Margin = new Padding(0);
        headerTitleLabel.Name = "headerTitleLabel";
        headerTitleLabel.Size = new Size(368, 46);
        headerTitleLabel.TabIndex = 0;
        headerTitleLabel.Text = "Microlux ERG-Connect";
        // 
        // headerSubtitleLabel
        // 
        headerSubtitleLabel.AutoSize = true;
        headerSubtitleLabel.Font = new Font("Segoe UI", 11F);
        headerSubtitleLabel.ForeColor = Color.FromArgb(189, 206, 223);
        headerSubtitleLabel.Location = new Point(0, 54);
        headerSubtitleLabel.Margin = new Padding(0, 8, 0, 16);
        headerSubtitleLabel.Name = "headerSubtitleLabel";
        headerSubtitleLabel.Size = new Size(486, 25);
        headerSubtitleLabel.TabIndex = 1;
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
        headerBadgesPanel.Location = new Point(0, 108);
        headerBadgesPanel.Margin = new Padding(0, 13, 0, 0);
        headerBadgesPanel.Name = "headerBadgesPanel";
        headerBadgesPanel.Size = new Size(1143, 47);
        headerBadgesPanel.TabIndex = 2;
        // 
        // headerStatusLabel
        // 
        headerStatusLabel.AutoSize = true;
        headerStatusLabel.BackColor = Color.FromArgb(48, 149, 177);
        headerStatusLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        headerStatusLabel.ForeColor = Color.White;
        headerStatusLabel.Location = new Point(0, 0);
        headerStatusLabel.Margin = new Padding(0, 0, 14, 11);
        headerStatusLabel.Name = "headerStatusLabel";
        headerStatusLabel.Padding = new Padding(14, 8, 14, 8);
        headerStatusLabel.Size = new Size(95, 36);
        headerStatusLabel.TabIndex = 0;
        headerStatusLabel.Text = "Статус: -";
        // 
        // headerDeviceLabel
        // 
        headerDeviceLabel.AutoSize = true;
        headerDeviceLabel.BackColor = Color.FromArgb(60, 87, 119);
        headerDeviceLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        headerDeviceLabel.ForeColor = Color.White;
        headerDeviceLabel.Location = new Point(109, 0);
        headerDeviceLabel.Margin = new Padding(0, 0, 14, 11);
        headerDeviceLabel.Name = "headerDeviceLabel";
        headerDeviceLabel.Padding = new Padding(14, 8, 14, 8);
        headerDeviceLabel.Size = new Size(129, 36);
        headerDeviceLabel.TabIndex = 1;
        headerDeviceLabel.Text = "Устройство: -";
        // 
        // headerPortLabel
        // 
        headerPortLabel.AutoSize = true;
        headerPortLabel.BackColor = Color.FromArgb(74, 102, 135);
        headerPortLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        headerPortLabel.ForeColor = Color.White;
        headerPortLabel.Location = new Point(252, 0);
        headerPortLabel.Margin = new Padding(0, 0, 14, 11);
        headerPortLabel.Name = "headerPortLabel";
        headerPortLabel.Padding = new Padding(14, 8, 14, 8);
        headerPortLabel.Size = new Size(86, 36);
        headerPortLabel.TabIndex = 2;
        headerPortLabel.Text = "Порт: -";
        // 
        // headerSyncLabel
        // 
        headerSyncLabel.AutoSize = true;
        headerSyncLabel.BackColor = Color.FromArgb(48, 149, 177);
        headerSyncLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        headerSyncLabel.ForeColor = Color.White;
        headerSyncLabel.Location = new Point(352, 0);
        headerSyncLabel.Margin = new Padding(0, 0, 14, 11);
        headerSyncLabel.Name = "headerSyncLabel";
        headerSyncLabel.Padding = new Padding(14, 8, 14, 8);
        headerSyncLabel.Size = new Size(163, 36);
        headerSyncLabel.TabIndex = 3;
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
        contentLayout.Location = new Point(23, 213);
        contentLayout.Margin = new Padding(0, 0, 0, 21);
        contentLayout.Name = "contentLayout";
        contentLayout.RowCount = 1;
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        contentLayout.Size = new Size(1325, 675);
        contentLayout.TabIndex = 1;
        // 
        // detailsContainer
        // 
        detailsContainer.AutoScroll = true;
        detailsContainer.AutoScrollMargin = new Size(0, 16);
        detailsContainer.Controls.Add(detailsLayout);
        detailsContainer.Dock = DockStyle.Fill;
        detailsContainer.Location = new Point(0, 0);
        detailsContainer.Margin = new Padding(0);
        detailsContainer.MinimumSize = new Size(366, 0);
        detailsContainer.Name = "detailsContainer";
        detailsContainer.Size = new Size(503, 675);
        detailsContainer.TabIndex = 0;
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
        detailsLayout.Location = new Point(0, 0);
        detailsLayout.Margin = new Padding(0);
        detailsLayout.Name = "detailsLayout";
        detailsLayout.RowCount = 3;
        detailsLayout.RowStyles.Add(new RowStyle());
        detailsLayout.RowStyles.Add(new RowStyle());
        detailsLayout.RowStyles.Add(new RowStyle());
        detailsLayout.Size = new Size(482, 1349);
        detailsLayout.TabIndex = 0;
        // 
        // connectionGroup
        // 
        connectionGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        connectionGroup.AutoSize = true;
        connectionGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        connectionGroup.BackColor = Color.White;
        connectionGroup.Controls.Add(connectionLayout);
        connectionGroup.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        connectionGroup.ForeColor = Color.FromArgb(33, 37, 41);
        connectionGroup.Location = new Point(0, 0);
        connectionGroup.Margin = new Padding(0, 0, 0, 21);
        connectionGroup.Name = "connectionGroup";
        connectionGroup.Padding = new Padding(18, 27, 18, 21);
        connectionGroup.Size = new Size(482, 425);
        connectionGroup.TabIndex = 0;
        connectionGroup.TabStop = false;
        connectionGroup.Text = "Подключение";
        // 
        // connectionLayout
        // 
        connectionLayout.AutoSize = true;
        connectionLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        connectionLayout.BackColor = Color.White;
        connectionLayout.ColumnCount = 2;
        connectionLayout.ColumnStyles.Add(new ColumnStyle());
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
        connectionLayout.Location = new Point(18, 50);
        connectionLayout.Margin = new Padding(0);
        connectionLayout.Name = "connectionLayout";
        connectionLayout.Padding = new Padding(0, 5, 0, 11);
        connectionLayout.RowCount = 7;
        connectionLayout.RowStyles.Add(new RowStyle());
        connectionLayout.RowStyles.Add(new RowStyle());
        connectionLayout.RowStyles.Add(new RowStyle());
        connectionLayout.RowStyles.Add(new RowStyle());
        connectionLayout.RowStyles.Add(new RowStyle());
        connectionLayout.RowStyles.Add(new RowStyle());
        connectionLayout.RowStyles.Add(new RowStyle());
        connectionLayout.Size = new Size(446, 354);
        connectionLayout.TabIndex = 0;
        // 
        // statusCaptionLabel
        // 
        statusCaptionLabel.AutoSize = true;
        statusCaptionLabel.Font = new Font("Segoe UI", 9F);
        statusCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        statusCaptionLabel.Location = new Point(0, 5);
        statusCaptionLabel.Margin = new Padding(0, 0, 18, 0);
        statusCaptionLabel.Name = "statusCaptionLabel";
        statusCaptionLabel.Size = new Size(55, 20);
        statusCaptionLabel.TabIndex = 0;
        statusCaptionLabel.Text = "Статус:";
        // 
        // statusValueLabel
        // 
        statusValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        statusValueLabel.AutoSize = true;
        statusValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        statusValueLabel.ForeColor = Color.FromArgb(34, 158, 189);
        statusValueLabel.Location = new Point(95, 13);
        statusValueLabel.Margin = new Padding(9, 8, 0, 5);
        statusValueLabel.MinimumSize = new Size(0, 32);
        statusValueLabel.Name = "statusValueLabel";
        statusValueLabel.Size = new Size(351, 32);
        statusValueLabel.TabIndex = 1;
        // 
        // portCaptionLabel
        // 
        portCaptionLabel.AutoSize = true;
        portCaptionLabel.Font = new Font("Segoe UI", 9F);
        portCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        portCaptionLabel.Location = new Point(0, 50);
        portCaptionLabel.Margin = new Padding(0, 0, 18, 0);
        portCaptionLabel.Name = "portCaptionLabel";
        portCaptionLabel.Size = new Size(47, 20);
        portCaptionLabel.TabIndex = 2;
        portCaptionLabel.Text = "Порт:";
        // 
        // portValueLabel
        // 
        portValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        portValueLabel.AutoSize = true;
        portValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        portValueLabel.ForeColor = Color.FromArgb(33, 37, 41);
        portValueLabel.Location = new Point(95, 58);
        portValueLabel.Margin = new Padding(9, 8, 0, 5);
        portValueLabel.MinimumSize = new Size(0, 32);
        portValueLabel.Name = "portValueLabel";
        portValueLabel.Size = new Size(351, 32);
        portValueLabel.TabIndex = 3;
        // 
        // deviceCaptionLabel
        // 
        deviceCaptionLabel.AutoSize = true;
        deviceCaptionLabel.Font = new Font("Segoe UI", 9F);
        deviceCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        deviceCaptionLabel.Location = new Point(0, 95);
        deviceCaptionLabel.Margin = new Padding(0, 0, 18, 0);
        deviceCaptionLabel.Name = "deviceCaptionLabel";
        deviceCaptionLabel.Size = new Size(68, 20);
        deviceCaptionLabel.TabIndex = 4;
        deviceCaptionLabel.Text = "Прибор:";
        // 
        // deviceValueLabel
        // 
        deviceValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        deviceValueLabel.AutoSize = true;
        deviceValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        deviceValueLabel.ForeColor = Color.FromArgb(33, 37, 41);
        deviceValueLabel.Location = new Point(95, 103);
        deviceValueLabel.Margin = new Padding(9, 8, 0, 5);
        deviceValueLabel.MinimumSize = new Size(0, 32);
        deviceValueLabel.Name = "deviceValueLabel";
        deviceValueLabel.Size = new Size(351, 32);
        deviceValueLabel.TabIndex = 5;
        // 
        // softwareCaptionLabel
        // 
        softwareCaptionLabel.AutoSize = true;
        softwareCaptionLabel.Font = new Font("Segoe UI", 9F);
        softwareCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        softwareCaptionLabel.Location = new Point(0, 140);
        softwareCaptionLabel.Margin = new Padding(0, 0, 18, 0);
        softwareCaptionLabel.Name = "softwareCaptionLabel";
        softwareCaptionLabel.Size = new Size(34, 20);
        softwareCaptionLabel.TabIndex = 6;
        softwareCaptionLabel.Text = "ПО:";
        // 
        // softwareValueLabel
        // 
        softwareValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        softwareValueLabel.AutoSize = true;
        softwareValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        softwareValueLabel.ForeColor = Color.FromArgb(33, 37, 41);
        softwareValueLabel.Location = new Point(95, 148);
        softwareValueLabel.Margin = new Padding(9, 8, 0, 5);
        softwareValueLabel.MinimumSize = new Size(0, 32);
        softwareValueLabel.Name = "softwareValueLabel";
        softwareValueLabel.Size = new Size(351, 32);
        softwareValueLabel.TabIndex = 7;
        // 
        // reportCaptionLabel
        // 
        reportCaptionLabel.AutoSize = true;
        reportCaptionLabel.Font = new Font("Segoe UI", 9F);
        reportCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        reportCaptionLabel.Location = new Point(0, 185);
        reportCaptionLabel.Margin = new Padding(0, 0, 18, 0);
        reportCaptionLabel.Name = "reportCaptionLabel";
        reportCaptionLabel.Size = new Size(51, 20);
        reportCaptionLabel.TabIndex = 8;
        reportCaptionLabel.Text = "Отчет:";
        // 
        // reportValueLabel
        // 
        reportValueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        reportValueLabel.AutoSize = true;
        reportValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        reportValueLabel.ForeColor = Color.FromArgb(33, 37, 41);
        reportValueLabel.Location = new Point(95, 193);
        reportValueLabel.Margin = new Padding(9, 8, 0, 5);
        reportValueLabel.MinimumSize = new Size(0, 32);
        reportValueLabel.Name = "reportValueLabel";
        reportValueLabel.Size = new Size(351, 32);
        reportValueLabel.TabIndex = 9;
        // 
        // syncStatusLabel
        // 
        syncStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        syncStatusLabel.AutoSize = true;
        connectionLayout.SetColumnSpan(syncStatusLabel, 2);
        syncStatusLabel.Font = new Font("Segoe UI", 9F);
        syncStatusLabel.ForeColor = Color.FromArgb(54, 127, 151);
        syncStatusLabel.Location = new Point(0, 249);
        syncStatusLabel.Margin = new Padding(0, 19, 0, 5);
        syncStatusLabel.Name = "syncStatusLabel";
        syncStatusLabel.Size = new Size(446, 20);
        syncStatusLabel.TabIndex = 10;
        // 
        // resetPortButton
        // 
        resetPortButton.AutoSize = true;
        resetPortButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        resetPortButton.BackColor = Color.FromArgb(34, 158, 189);
        connectionLayout.SetColumnSpan(resetPortButton, 2);
        resetPortButton.Cursor = Cursors.Hand;
        resetPortButton.FlatAppearance.BorderSize = 0;
        resetPortButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 138, 166);
        resetPortButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 182, 214);
        resetPortButton.FlatStyle = FlatStyle.Flat;
        resetPortButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        resetPortButton.ForeColor = Color.White;
        resetPortButton.Location = new Point(0, 295);
        resetPortButton.Margin = new Padding(0, 21, 0, 0);
        resetPortButton.MinimumSize = new Size(160, 48);
        resetPortButton.Name = "resetPortButton";
        resetPortButton.Padding = new Padding(11, 8, 11, 8);
        resetPortButton.Size = new Size(160, 48);
        resetPortButton.TabIndex = 11;
        resetPortButton.Text = "Сбросить порт";
        resetPortButton.UseVisualStyleBackColor = false;
        resetPortButton.Click += OnResetPortClicked;
        // 
        // settingsGroup
        // 
        settingsGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        settingsGroup.AutoSize = true;
        settingsGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        settingsGroup.BackColor = Color.White;
        settingsGroup.Controls.Add(settingsLayout);
        settingsGroup.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        settingsGroup.ForeColor = Color.FromArgb(33, 37, 41);
        settingsGroup.Location = new Point(0, 446);
        settingsGroup.Margin = new Padding(0, 0, 0, 21);
        settingsGroup.Name = "settingsGroup";
        settingsGroup.Padding = new Padding(18, 27, 18, 21);
        settingsGroup.Size = new Size(482, 522);
        settingsGroup.TabIndex = 1;
        settingsGroup.TabStop = false;
        settingsGroup.Text = "Настройки опроса";
        // 
        // settingsLayout
        // 
        settingsLayout.AutoSize = true;
        settingsLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        settingsLayout.BackColor = Color.White;
        settingsLayout.ColumnCount = 2;
        settingsLayout.ColumnStyles.Add(new ColumnStyle());
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        settingsLayout.Controls.Add(scanIntervalLabel, 0, 0);
        settingsLayout.Controls.Add(scanIntervalTextBox, 1, 0);
        settingsLayout.Controls.Add(reconnectDelayLabel, 0, 1);
        settingsLayout.Controls.Add(reconnectDelayTextBox, 1, 1);
        settingsLayout.Controls.Add(backgroundSyncLabel, 0, 2);
        settingsLayout.Controls.Add(backgroundSyncTextBox, 1, 2);
        settingsLayout.Controls.Add(reportTemplateLabel, 0, 3);
        settingsLayout.Controls.Add(reportTemplateComboBox, 1, 3);
        settingsLayout.Controls.Add(renderingModeLabel, 0, 4);
        settingsLayout.Controls.Add(renderingModeComboBox, 1, 4);
        settingsLayout.Controls.Add(reportHeaderLabel, 0, 5);
        settingsLayout.Controls.Add(reportHeaderTextBox, 1, 5);
        settingsLayout.Controls.Add(settingsButtonsPanel, 0, 6);
        settingsLayout.Dock = DockStyle.Fill;
        settingsLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        settingsLayout.Location = new Point(18, 50);
        settingsLayout.Margin = new Padding(0);
        settingsLayout.Name = "settingsLayout";
        settingsLayout.Padding = new Padding(0, 5, 0, 11);
        settingsLayout.RowCount = 7;
        settingsLayout.RowStyles.Add(new RowStyle());
        settingsLayout.RowStyles.Add(new RowStyle());
        settingsLayout.RowStyles.Add(new RowStyle());
        settingsLayout.RowStyles.Add(new RowStyle());
        settingsLayout.RowStyles.Add(new RowStyle());
        settingsLayout.RowStyles.Add(new RowStyle());
        settingsLayout.RowStyles.Add(new RowStyle());
        settingsLayout.Size = new Size(446, 451);
        settingsLayout.TabIndex = 0;
        // 
        // scanIntervalLabel
        // 
        scanIntervalLabel.AutoSize = true;
        scanIntervalLabel.Font = new Font("Segoe UI", 9F);
        scanIntervalLabel.ForeColor = Color.FromArgb(120, 128, 145);
        scanIntervalLabel.Location = new Point(0, 5);
        scanIntervalLabel.Margin = new Padding(0, 0, 18, 0);
        scanIntervalLabel.Name = "scanIntervalLabel";
        scanIntervalLabel.Size = new Size(153, 20);
        scanIntervalLabel.TabIndex = 0;
        scanIntervalLabel.Text = "Интервал поиска (с):";
        // 
        // scanIntervalTextBox
        // 
        scanIntervalTextBox.BorderStyle = BorderStyle.FixedSingle;
        scanIntervalTextBox.Dock = DockStyle.Fill;
        scanIntervalTextBox.Location = new Point(246, 10);
        scanIntervalTextBox.Margin = new Padding(5);
        scanIntervalTextBox.Name = "scanIntervalTextBox";
        scanIntervalTextBox.Size = new Size(195, 30);
        scanIntervalTextBox.TabIndex = 1;
        scanIntervalTextBox.TextAlign = HorizontalAlignment.Center;
        scanIntervalTextBox.Validating += OnNumericTextBoxValidating;
        scanIntervalTextBox.Validated += OnScanIntervalValidated;
        // 
        // reconnectDelayLabel
        // 
        reconnectDelayLabel.AutoSize = true;
        reconnectDelayLabel.Font = new Font("Segoe UI", 9F);
        reconnectDelayLabel.ForeColor = Color.FromArgb(120, 128, 145);
        reconnectDelayLabel.Location = new Point(0, 58);
        reconnectDelayLabel.Margin = new Padding(0, 13, 18, 0);
        reconnectDelayLabel.Name = "reconnectDelayLabel";
        reconnectDelayLabel.Size = new Size(206, 20);
        reconnectDelayLabel.TabIndex = 2;
        reconnectDelayLabel.Text = "Задержка перепроверки (с):";
        // 
        // reconnectDelayTextBox
        // 
        reconnectDelayTextBox.BorderStyle = BorderStyle.FixedSingle;
        reconnectDelayTextBox.Dock = DockStyle.Fill;
        reconnectDelayTextBox.Location = new Point(246, 50);
        reconnectDelayTextBox.Margin = new Padding(5);
        reconnectDelayTextBox.Name = "reconnectDelayTextBox";
        reconnectDelayTextBox.Size = new Size(195, 30);
        reconnectDelayTextBox.TabIndex = 3;
        reconnectDelayTextBox.TextAlign = HorizontalAlignment.Center;
        reconnectDelayTextBox.Validating += OnNumericTextBoxValidating;
        reconnectDelayTextBox.Validated += OnReconnectDelayValidated;
        // 
        // backgroundSyncLabel
        // 
        backgroundSyncLabel.AutoSize = true;
        backgroundSyncLabel.Font = new Font("Segoe UI", 9F);
        backgroundSyncLabel.ForeColor = Color.FromArgb(120, 128, 145);
        backgroundSyncLabel.Location = new Point(0, 98);
        backgroundSyncLabel.Margin = new Padding(0, 13, 18, 0);
        backgroundSyncLabel.Name = "backgroundSyncLabel";
        backgroundSyncLabel.Size = new Size(223, 20);
        backgroundSyncLabel.TabIndex = 4;
        backgroundSyncLabel.Text = "Период синхронизации (мин):";
        // 
        // backgroundSyncTextBox
        // 
        backgroundSyncTextBox.BorderStyle = BorderStyle.FixedSingle;
        backgroundSyncTextBox.Dock = DockStyle.Fill;
        backgroundSyncTextBox.Location = new Point(246, 90);
        backgroundSyncTextBox.Margin = new Padding(5);
        backgroundSyncTextBox.Name = "backgroundSyncTextBox";
        backgroundSyncTextBox.Size = new Size(195, 30);
        backgroundSyncTextBox.TabIndex = 5;
        backgroundSyncTextBox.TextAlign = HorizontalAlignment.Center;
        backgroundSyncTextBox.Validating += OnNumericTextBoxValidating;
        backgroundSyncTextBox.Validated += OnBackgroundSyncValidated;
        // 
        // reportTemplateLabel
        // 
        reportTemplateLabel.AutoSize = true;
        reportTemplateLabel.Font = new Font("Segoe UI", 9F);
        reportTemplateLabel.ForeColor = Color.FromArgb(120, 128, 145);
        reportTemplateLabel.Location = new Point(0, 138);
        reportTemplateLabel.Margin = new Padding(0, 13, 18, 0);
        reportTemplateLabel.Name = "reportTemplateLabel";
        reportTemplateLabel.Size = new Size(127, 20);
        reportTemplateLabel.TabIndex = 6;
        reportTemplateLabel.Text = "Шаблон отчетов:";
        // 
        // reportTemplateComboBox
        // 
        reportTemplateComboBox.Dock = DockStyle.Fill;
        reportTemplateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        reportTemplateComboBox.FormattingEnabled = true;
        reportTemplateComboBox.Location = new Point(246, 130);
        reportTemplateComboBox.Margin = new Padding(5);
        reportTemplateComboBox.MinimumSize = new Size(137, 0);
        reportTemplateComboBox.Name = "reportTemplateComboBox";
        reportTemplateComboBox.Size = new Size(195, 31);
        reportTemplateComboBox.TabIndex = 7;
        // 
        // renderingModeLabel
        // 
        renderingModeLabel.AutoSize = true;
        renderingModeLabel.Font = new Font("Segoe UI", 9F);
        renderingModeLabel.ForeColor = Color.FromArgb(120, 128, 145);
        renderingModeLabel.Location = new Point(0, 179);
        renderingModeLabel.Margin = new Padding(0, 13, 18, 0);
        renderingModeLabel.Name = "renderingModeLabel";
        renderingModeLabel.Size = new Size(168, 20);
        renderingModeLabel.TabIndex = 8;
        renderingModeLabel.Text = "Режим генерации PDF:";
        // 
        // renderingModeComboBox
        // 
        renderingModeComboBox.Dock = DockStyle.Fill;
        renderingModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        renderingModeComboBox.FormattingEnabled = true;
        renderingModeComboBox.Location = new Point(246, 171);
        renderingModeComboBox.Margin = new Padding(5);
        renderingModeComboBox.MinimumSize = new Size(137, 0);
        renderingModeComboBox.Name = "renderingModeComboBox";
        renderingModeComboBox.Size = new Size(195, 31);
        renderingModeComboBox.TabIndex = 9;
        // 
        // reportHeaderLabel
        // 
        reportHeaderLabel.AutoSize = true;
        reportHeaderLabel.Font = new Font("Segoe UI", 9F);
        reportHeaderLabel.ForeColor = Color.FromArgb(120, 128, 145);
        reportHeaderLabel.Location = new Point(0, 220);
        reportHeaderLabel.Margin = new Padding(0, 13, 18, 0);
        reportHeaderLabel.Name = "reportHeaderLabel";
        reportHeaderLabel.Size = new Size(107, 20);
        reportHeaderLabel.TabIndex = 10;
        reportHeaderLabel.Text = "Шапка отчета:";
        // 
        // reportHeaderTextBox
        // 
        reportHeaderTextBox.AcceptsReturn = true;
        reportHeaderTextBox.BorderStyle = BorderStyle.FixedSingle;
        reportHeaderTextBox.Dock = DockStyle.Fill;
        reportHeaderTextBox.Location = new Point(246, 212);
        reportHeaderTextBox.Margin = new Padding(5);
        reportHeaderTextBox.MinimumSize = new Size(183, 79);
        reportHeaderTextBox.Multiline = true;
        reportHeaderTextBox.Name = "reportHeaderTextBox";
        reportHeaderTextBox.ScrollBars = ScrollBars.Vertical;
        reportHeaderTextBox.Size = new Size(195, 106);
        reportHeaderTextBox.TabIndex = 11;
        reportHeaderTextBox.Validated += OnReportHeaderValidated;
        // 
        // settingsButtonsPanel
        // 
        settingsButtonsPanel.AutoSize = true;
        settingsButtonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        settingsLayout.SetColumnSpan(settingsButtonsPanel, 2);
        settingsButtonsPanel.Controls.Add(openReportsButton);
        settingsButtonsPanel.Controls.Add(convertBinButton);
        settingsButtonsPanel.Location = new Point(0, 344);
        settingsButtonsPanel.Margin = new Padding(0, 21, 0, 0);
        settingsButtonsPanel.Name = "settingsButtonsPanel";
        settingsButtonsPanel.Size = new Size(240, 96);
        settingsButtonsPanel.TabIndex = 12;
        // 
        // openReportsButton
        // 
        openReportsButton.AutoSize = true;
        openReportsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        openReportsButton.BackColor = Color.FromArgb(239, 246, 249);
        openReportsButton.Cursor = Cursors.Hand;
        openReportsButton.FlatAppearance.BorderSize = 0;
        openReportsButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 228, 236);
        openReportsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 236, 242);
        openReportsButton.FlatStyle = FlatStyle.Flat;
        openReportsButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        openReportsButton.ForeColor = Color.FromArgb(33, 37, 41);
        openReportsButton.Location = new Point(0, 0);
        openReportsButton.Margin = new Padding(0, 0, 14, 0);
        openReportsButton.MinimumSize = new Size(160, 48);
        openReportsButton.Name = "openReportsButton";
        openReportsButton.Padding = new Padding(11, 8, 11, 8);
        openReportsButton.Size = new Size(224, 48);
        openReportsButton.TabIndex = 0;
        openReportsButton.Text = "Открыть каталог отчетов";
        openReportsButton.UseVisualStyleBackColor = false;
        openReportsButton.Click += OnOpenReportsClicked;
        // 
        // convertBinButton
        // 
        convertBinButton.AutoSize = true;
        convertBinButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        convertBinButton.BackColor = Color.FromArgb(34, 158, 189);
        convertBinButton.Cursor = Cursors.Hand;
        convertBinButton.FlatAppearance.BorderSize = 0;
        convertBinButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 138, 166);
        convertBinButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 182, 214);
        convertBinButton.FlatStyle = FlatStyle.Flat;
        convertBinButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        convertBinButton.ForeColor = Color.White;
        convertBinButton.Location = new Point(0, 48);
        convertBinButton.Margin = new Padding(0);
        convertBinButton.MinimumSize = new Size(160, 48);
        convertBinButton.Name = "convertBinButton";
        convertBinButton.Padding = new Padding(11, 8, 11, 8);
        convertBinButton.Size = new Size(240, 48);
        convertBinButton.TabIndex = 1;
        convertBinButton.Text = "Конвертировать .bin в отчет";
        convertBinButton.UseVisualStyleBackColor = false;
        convertBinButton.Click += OnConvertBinClicked;
        // 
        // updatesGroup
        // 
        updatesGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        updatesGroup.AutoSize = true;
        updatesGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        updatesGroup.BackColor = Color.White;
        updatesGroup.Controls.Add(updatesLayout);
        updatesGroup.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        updatesGroup.ForeColor = Color.FromArgb(33, 37, 41);
        updatesGroup.Location = new Point(0, 989);
        updatesGroup.Margin = new Padding(0);
        updatesGroup.Name = "updatesGroup";
        updatesGroup.Padding = new Padding(18, 27, 18, 21);
        updatesGroup.Size = new Size(482, 360);
        updatesGroup.TabIndex = 2;
        updatesGroup.TabStop = false;
        updatesGroup.Text = "Обновления";
        // 
        // updatesLayout
        // 
        updatesLayout.AutoSize = true;
        updatesLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        updatesLayout.BackColor = Color.White;
        updatesLayout.ColumnCount = 2;
        updatesLayout.ColumnStyles.Add(new ColumnStyle());
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
        updatesLayout.Location = new Point(18, 50);
        updatesLayout.Margin = new Padding(0);
        updatesLayout.Name = "updatesLayout";
        updatesLayout.Padding = new Padding(0, 5, 0, 11);
        updatesLayout.RowCount = 6;
        updatesLayout.RowStyles.Add(new RowStyle());
        updatesLayout.RowStyles.Add(new RowStyle());
        updatesLayout.RowStyles.Add(new RowStyle());
        updatesLayout.RowStyles.Add(new RowStyle());
        updatesLayout.RowStyles.Add(new RowStyle());
        updatesLayout.RowStyles.Add(new RowStyle());
        updatesLayout.Size = new Size(446, 289);
        updatesLayout.TabIndex = 0;
        // 
        // updateStatusCaptionLabel
        // 
        updateStatusCaptionLabel.AutoSize = true;
        updateStatusCaptionLabel.Font = new Font("Segoe UI", 9F);
        updateStatusCaptionLabel.ForeColor = Color.FromArgb(120, 128, 145);
        updateStatusCaptionLabel.Location = new Point(0, 5);
        updateStatusCaptionLabel.Margin = new Padding(0, 0, 18, 0);
        updateStatusCaptionLabel.Name = "updateStatusCaptionLabel";
        updateStatusCaptionLabel.Size = new Size(146, 20);
        updateStatusCaptionLabel.TabIndex = 0;
        updateStatusCaptionLabel.Text = "Статус обновлений:";
        // 
        // updateStatusValueLabel
        // 
        updateStatusValueLabel.AutoEllipsis = true;
        updatesLayout.SetColumnSpan(updateStatusValueLabel, 2);
        updateStatusValueLabel.Dock = DockStyle.Fill;
        updateStatusValueLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        updateStatusValueLabel.ForeColor = Color.FromArgb(33, 37, 41);
        updateStatusValueLabel.Location = new Point(0, 33);
        updateStatusValueLabel.Margin = new Padding(0, 8, 0, 5);
        updateStatusValueLabel.MaximumSize = new Size(0, 32);
        updateStatusValueLabel.MinimumSize = new Size(0, 32);
        updateStatusValueLabel.Name = "updateStatusValueLabel";
        updateStatusValueLabel.Size = new Size(446, 32);
        updateStatusValueLabel.TabIndex = 1;
        updateStatusValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // updatesButtonsPanel
        // 
        updatesButtonsPanel.AutoSize = true;
        updatesButtonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        updatesLayout.SetColumnSpan(updatesButtonsPanel, 2);
        updatesButtonsPanel.Controls.Add(checkUpdatesButton);
        updatesButtonsPanel.Controls.Add(installUpdateButton);
        updatesButtonsPanel.Location = new Point(0, 83);
        updatesButtonsPanel.Margin = new Padding(0, 13, 0, 8);
        updatesButtonsPanel.Name = "updatesButtonsPanel";
        updatesButtonsPanel.Size = new Size(334, 48);
        updatesButtonsPanel.TabIndex = 2;
        // 
        // checkUpdatesButton
        // 
        checkUpdatesButton.AutoSize = true;
        checkUpdatesButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        checkUpdatesButton.BackColor = Color.FromArgb(239, 246, 249);
        checkUpdatesButton.Cursor = Cursors.Hand;
        checkUpdatesButton.FlatAppearance.BorderSize = 0;
        checkUpdatesButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 228, 236);
        checkUpdatesButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 236, 242);
        checkUpdatesButton.FlatStyle = FlatStyle.Flat;
        checkUpdatesButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        checkUpdatesButton.ForeColor = Color.FromArgb(33, 37, 41);
        checkUpdatesButton.Location = new Point(0, 0);
        checkUpdatesButton.Margin = new Padding(0, 0, 14, 0);
        checkUpdatesButton.MinimumSize = new Size(160, 48);
        checkUpdatesButton.Name = "checkUpdatesButton";
        checkUpdatesButton.Padding = new Padding(11, 8, 11, 8);
        checkUpdatesButton.Size = new Size(160, 48);
        checkUpdatesButton.TabIndex = 0;
        checkUpdatesButton.Text = "Проверить";
        checkUpdatesButton.UseVisualStyleBackColor = false;
        checkUpdatesButton.Click += OnCheckUpdatesClicked;
        // 
        // installUpdateButton
        // 
        installUpdateButton.AutoSize = true;
        installUpdateButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        installUpdateButton.BackColor = Color.FromArgb(34, 158, 189);
        installUpdateButton.Cursor = Cursors.Hand;
        installUpdateButton.FlatAppearance.BorderSize = 0;
        installUpdateButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 138, 166);
        installUpdateButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 182, 214);
        installUpdateButton.FlatStyle = FlatStyle.Flat;
        installUpdateButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        installUpdateButton.ForeColor = Color.White;
        installUpdateButton.Location = new Point(174, 0);
        installUpdateButton.Margin = new Padding(0);
        installUpdateButton.MinimumSize = new Size(160, 48);
        installUpdateButton.Name = "installUpdateButton";
        installUpdateButton.Padding = new Padding(11, 8, 11, 8);
        installUpdateButton.Size = new Size(160, 48);
        installUpdateButton.TabIndex = 1;
        installUpdateButton.Text = "Установить";
        installUpdateButton.UseVisualStyleBackColor = false;
        installUpdateButton.Click += OnInstallUpdateClicked;
        // 
        // updateIntervalLabel
        // 
        updateIntervalLabel.AutoSize = true;
        updateIntervalLabel.Font = new Font("Segoe UI", 9F);
        updateIntervalLabel.ForeColor = Color.FromArgb(120, 128, 145);
        updateIntervalLabel.Location = new Point(0, 155);
        updateIntervalLabel.Margin = new Padding(0, 16, 18, 0);
        updateIntervalLabel.Name = "updateIntervalLabel";
        updateIntervalLabel.Size = new Size(194, 20);
        updateIntervalLabel.TabIndex = 3;
        updateIntervalLabel.Text = "Интервал проверки (мин):";
        // 
        // updateIntervalTextBox
        // 
        updateIntervalTextBox.BorderStyle = BorderStyle.FixedSingle;
        updateIntervalTextBox.Location = new Point(212, 144);
        updateIntervalTextBox.Margin = new Padding(0, 5, 0, 0);
        updateIntervalTextBox.MinimumSize = new Size(205, 2);
        updateIntervalTextBox.Name = "updateIntervalTextBox";
        updateIntervalTextBox.Size = new Size(205, 30);
        updateIntervalTextBox.TabIndex = 4;
        updateIntervalTextBox.TextAlign = HorizontalAlignment.Center;
        updateIntervalTextBox.Validating += OnNumericTextBoxValidating;
        updateIntervalTextBox.Validated += OnUpdateIntervalValidated;
        // 
        // manifestUrlLabel
        // 
        manifestUrlLabel.AutoSize = true;
        manifestUrlLabel.Font = new Font("Segoe UI", 9F);
        manifestUrlLabel.ForeColor = Color.FromArgb(120, 128, 145);
        manifestUrlLabel.Location = new Point(0, 191);
        manifestUrlLabel.Margin = new Padding(0, 16, 18, 0);
        manifestUrlLabel.Name = "manifestUrlLabel";
        manifestUrlLabel.Size = new Size(118, 20);
        manifestUrlLabel.TabIndex = 5;
        manifestUrlLabel.Text = "URL манифеста:";
        // 
        // manifestUrlTextBox
        // 
        manifestUrlTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        manifestUrlTextBox.BorderStyle = BorderStyle.FixedSingle;
        manifestUrlTextBox.Location = new Point(212, 180);
        manifestUrlTextBox.Margin = new Padding(0, 5, 0, 0);
        manifestUrlTextBox.MinimumSize = new Size(205, 2);
        manifestUrlTextBox.Name = "manifestUrlTextBox";
        manifestUrlTextBox.Size = new Size(234, 30);
        manifestUrlTextBox.TabIndex = 6;
        manifestUrlTextBox.Validated += OnManifestUrlValidated;
        // 
        // openLogsButton
        // 
        openLogsButton.AutoSize = true;
        openLogsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        openLogsButton.BackColor = Color.FromArgb(239, 246, 249);
        updatesLayout.SetColumnSpan(openLogsButton, 2);
        openLogsButton.Cursor = Cursors.Hand;
        openLogsButton.FlatAppearance.BorderSize = 0;
        openLogsButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 228, 236);
        openLogsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 236, 242);
        openLogsButton.FlatStyle = FlatStyle.Flat;
        openLogsButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        openLogsButton.ForeColor = Color.FromArgb(33, 37, 41);
        openLogsButton.Location = new Point(0, 230);
        openLogsButton.Margin = new Padding(0, 19, 0, 0);
        openLogsButton.MinimumSize = new Size(160, 48);
        openLogsButton.Name = "openLogsButton";
        openLogsButton.Padding = new Padding(11, 8, 11, 8);
        openLogsButton.Size = new Size(208, 48);
        openLogsButton.TabIndex = 7;
        openLogsButton.Text = "Открыть каталог логов";
        openLogsButton.UseVisualStyleBackColor = false;
        openLogsButton.Click += OnOpenLogsClicked;
        // 
        // logsGroup
        // 
        logsGroup.BackColor = Color.White;
        logsGroup.Controls.Add(logGridView);
        logsGroup.Dock = DockStyle.Fill;
        logsGroup.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        logsGroup.ForeColor = Color.FromArgb(33, 37, 41);
        logsGroup.Location = new Point(530, 0);
        logsGroup.Margin = new Padding(27, 0, 0, 0);
        logsGroup.Name = "logsGroup";
        logsGroup.Padding = new Padding(18, 27, 18, 21);
        logsGroup.Size = new Size(795, 675);
        logsGroup.TabIndex = 1;
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
        logGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        logGridView.ColumnHeadersHeight = 36;
        logGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        logGridView.DataSource = logsBindingSource;
        logGridView.Dock = DockStyle.Fill;
        logGridView.EnableHeadersVisualStyles = false;
        logGridView.GridColor = Color.FromArgb(230, 236, 244);
        logGridView.Location = new Point(18, 50);
        logGridView.Margin = new Padding(0);
        logGridView.MultiSelect = false;
        logGridView.Name = "logGridView";
        logGridView.ReadOnly = true;
        logGridView.RowHeadersVisible = false;
        logGridView.RowHeadersWidth = 51;
        logGridView.RowTemplate.Height = 28;
        logGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        logGridView.ShowCellErrors = false;
        logGridView.ShowEditingIcon = false;
        logGridView.ShowRowErrors = false;
        logGridView.Size = new Size(759, 604);
        logGridView.TabIndex = 0;
        // 
        // mainStatusStrip
        // 
        mainStatusStrip.BackColor = Color.FromArgb(26, 38, 55);
        mainStatusStrip.Dock = DockStyle.Fill;
        mainStatusStrip.Font = new Font("Segoe UI", 9F);
        mainStatusStrip.ForeColor = Color.White;
        mainStatusStrip.ImageScalingSize = new Size(20, 20);
        mainStatusStrip.Items.AddRange(new ToolStripItem[] { appNameStatusLabel, versionStatusLabel });
        mainStatusStrip.Location = new Point(23, 909);
        mainStatusStrip.Name = "mainStatusStrip";
        mainStatusStrip.Padding = new Padding(14, 0, 14, 0);
        mainStatusStrip.Size = new Size(1325, 35);
        mainStatusStrip.SizingGrip = false;
        mainStatusStrip.TabIndex = 2;
        // 
        // appNameStatusLabel
        // 
        appNameStatusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        appNameStatusLabel.ForeColor = Color.White;
        appNameStatusLabel.Name = "appNameStatusLabel";
        appNameStatusLabel.Size = new Size(1235, 29);
        appNameStatusLabel.Spring = true;
        appNameStatusLabel.Text = "Microlux ERG-Connect";
        // 
        // versionStatusLabel
        // 
        versionStatusLabel.ForeColor = Color.FromArgb(189, 206, 223);
        versionStatusLabel.Name = "versionStatusLabel";
        versionStatusLabel.Size = new Size(62, 29);
        versionStatusLabel.Text = "Версия:";
        // 
        // trayIcon
        // 
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Icon = (Icon)resources.GetObject("trayIcon.Icon");
        trayIcon.Text = "Microlux ERG-Connect";
        trayIcon.Visible = true;
        trayIcon.DoubleClick += TrayIcon_DoubleClick;
        // 
        // trayMenu
        // 
        trayMenu.ImageScalingSize = new Size(24, 24);
        trayMenu.Items.AddRange(new ToolStripItem[] { trayOpenMenuItem, trayExitMenuItem });
        trayMenu.Name = "trayMenu";
        trayMenu.Size = new Size(137, 52);
        // 
        // trayOpenMenuItem
        // 
        trayOpenMenuItem.Name = "trayOpenMenuItem";
        trayOpenMenuItem.Size = new Size(136, 24);
        trayOpenMenuItem.Text = "Открыть";
        trayOpenMenuItem.Click += TrayOpenMenuItem_Click;
        // 
        // trayExitMenuItem
        // 
        trayExitMenuItem.Name = "trayExitMenuItem";
        trayExitMenuItem.Size = new Size(136, 24);
        trayExitMenuItem.Text = "Выход";
        trayExitMenuItem.Click += TrayExitMenuItem_Click;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(245, 248, 252);
        ClientSize = new Size(1371, 960);
        Controls.Add(mainLayout);
        Font = new Font("Segoe UI", 9F);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Margin = new Padding(3, 4, 3, 4);
        MinimumSize = new Size(1095, 811);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Microlux ERG-Connect";
        FormClosing += OnFormClosing;
        Load += OnFormLoaded;
        Resize += OnFormResized;
        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        headerPanel.ResumeLayout(false);
        headerLayout.ResumeLayout(false);
        ((ISupportInitialize)headerIconPictureBox).EndInit();
        headerTextLayout.ResumeLayout(false);
        headerTextLayout.PerformLayout();
        headerBadgesPanel.ResumeLayout(false);
        headerBadgesPanel.PerformLayout();
        contentLayout.ResumeLayout(false);
        detailsContainer.ResumeLayout(false);
        detailsContainer.PerformLayout();
        detailsLayout.ResumeLayout(false);
        detailsLayout.PerformLayout();
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
        ((ISupportInitialize)logGridView).EndInit();
        ((ISupportInitialize)logsBindingSource).EndInit();
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
