using System.Drawing;
using System.Windows.Forms;

namespace MicroluxErgConnect.Views;

partial class SettingsForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null!;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        mainLayout = new TableLayoutPanel();
        contentLayout = new TableLayoutPanel();
        pdfPathLabel = new Label();
        pdfPathPanel = new FlowLayoutPanel();
        pdfPathTextBox = new TextBox();
        browseButton = new Button();
        headerGroup = new GroupBox();
        headerLayout = new TableLayoutPanel();
        headerLine1Label = new Label();
        headerLine1TextBox = new TextBox();
        headerLine2Label = new Label();
        headerLine2TextBox = new TextBox();
        headerLine3Label = new Label();
        headerLine3TextBox = new TextBox();
        headerLine4Label = new Label();
        headerLine4TextBox = new TextBox();
        buttonsPanel = new FlowLayoutPanel();
        saveButton = new Button();
        cancelButton = new Button();
        mainLayout.SuspendLayout();
        contentLayout.SuspendLayout();
        pdfPathPanel.SuspendLayout();
        headerGroup.SuspendLayout();
        headerLayout.SuspendLayout();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // mainLayout
        // 
        mainLayout.ColumnCount = 1;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.Controls.Add(contentLayout, 0, 0);
        mainLayout.Controls.Add(buttonsPanel, 0, 1);
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Location = new Point(0, 0);
        mainLayout.Name = "mainLayout";
        mainLayout.RowCount = 2;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle());
        mainLayout.Size = new Size(540, 360);
        mainLayout.TabIndex = 0;
        // 
        // contentLayout
        // 
        contentLayout.AutoSize = true;
        contentLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        contentLayout.ColumnCount = 1;
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        contentLayout.Controls.Add(pdfPathLabel, 0, 0);
        contentLayout.Controls.Add(pdfPathPanel, 0, 1);
        contentLayout.Controls.Add(headerGroup, 0, 2);
        contentLayout.Dock = DockStyle.Fill;
        contentLayout.Location = new Point(12, 12);
        contentLayout.Margin = new Padding(12, 12, 12, 0);
        contentLayout.Name = "contentLayout";
        contentLayout.RowCount = 3;
        contentLayout.RowStyles.Add(new RowStyle());
        contentLayout.RowStyles.Add(new RowStyle());
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        contentLayout.Size = new Size(516, 303);
        contentLayout.TabIndex = 0;
        // 
        // pdfPathLabel
        // 
        pdfPathLabel.AutoSize = true;
        pdfPathLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        pdfPathLabel.Location = new Point(0, 0);
        pdfPathLabel.Margin = new Padding(0, 0, 0, 6);
        pdfPathLabel.Name = "pdfPathLabel";
        pdfPathLabel.Size = new Size(173, 15);
        pdfPathLabel.TabIndex = 0;
        pdfPathLabel.Text = "Папка для PDF-отчетов:";
        // 
        // pdfPathPanel
        // 
        pdfPathPanel.AutoSize = true;
        pdfPathPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        pdfPathPanel.Controls.Add(pdfPathTextBox);
        pdfPathPanel.Controls.Add(browseButton);
        pdfPathPanel.Dock = DockStyle.Fill;
        pdfPathPanel.Location = new Point(0, 21);
        pdfPathPanel.Margin = new Padding(0, 0, 0, 12);
        pdfPathPanel.Name = "pdfPathPanel";
        pdfPathPanel.Size = new Size(516, 35);
        pdfPathPanel.TabIndex = 1;
        pdfPathPanel.WrapContents = false;
        // 
        // pdfPathTextBox
        // 
        pdfPathTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        pdfPathTextBox.BorderStyle = BorderStyle.FixedSingle;
        pdfPathTextBox.Location = new Point(0, 5);
        pdfPathTextBox.Margin = new Padding(0, 0, 8, 0);
        pdfPathTextBox.MinimumSize = new Size(320, 27);
        pdfPathTextBox.Name = "pdfPathTextBox";
        pdfPathTextBox.Size = new Size(380, 27);
        pdfPathTextBox.TabIndex = 0;
        // 
        // browseButton
        // 
        browseButton.AutoSize = true;
        browseButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        browseButton.Location = new Point(388, 0);
        browseButton.Margin = new Padding(8, 0, 0, 0);
        browseButton.Name = "browseButton";
        browseButton.Padding = new Padding(8, 4, 8, 4);
        browseButton.Size = new Size(79, 35);
        browseButton.TabIndex = 1;
        browseButton.Text = "Обзор...";
        browseButton.UseVisualStyleBackColor = true;
        browseButton.Click += OnBrowseClick;
        // 
        // headerGroup
        // 
        headerGroup.AutoSize = true;
        headerGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        headerGroup.Controls.Add(headerLayout);
        headerGroup.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        headerGroup.Location = new Point(0, 68);
        headerGroup.Margin = new Padding(0);
        headerGroup.Name = "headerGroup";
        headerGroup.Padding = new Padding(12, 10, 12, 12);
        headerGroup.Size = new Size(516, 235);
        headerGroup.TabIndex = 2;
        headerGroup.TabStop = false;
        headerGroup.Text = "Шапка отчета";
        // 
        // headerLayout
        // 
        headerLayout.AutoSize = true;
        headerLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        headerLayout.ColumnCount = 2;
        headerLayout.ColumnStyles.Add(new ColumnStyle());
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.Controls.Add(headerLine1Label, 0, 0);
        headerLayout.Controls.Add(headerLine1TextBox, 1, 0);
        headerLayout.Controls.Add(headerLine2Label, 0, 1);
        headerLayout.Controls.Add(headerLine2TextBox, 1, 1);
        headerLayout.Controls.Add(headerLine3Label, 0, 2);
        headerLayout.Controls.Add(headerLine3TextBox, 1, 2);
        headerLayout.Controls.Add(headerLine4Label, 0, 3);
        headerLayout.Controls.Add(headerLine4TextBox, 1, 3);
        headerLayout.Dock = DockStyle.Fill;
        headerLayout.Location = new Point(12, 26);
        headerLayout.Name = "headerLayout";
        headerLayout.RowCount = 4;
        headerLayout.RowStyles.Add(new RowStyle());
        headerLayout.RowStyles.Add(new RowStyle());
        headerLayout.RowStyles.Add(new RowStyle());
        headerLayout.RowStyles.Add(new RowStyle());
        headerLayout.Size = new Size(492, 197);
        headerLayout.TabIndex = 0;
        // 
        // headerLine1Label
        // 
        headerLine1Label.AutoSize = true;
        headerLine1Label.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        headerLine1Label.ForeColor = Color.FromArgb(80, 80, 80);
        headerLine1Label.Location = new Point(0, 3);
        headerLine1Label.Margin = new Padding(0, 3, 8, 6);
        headerLine1Label.Name = "headerLine1Label";
        headerLine1Label.Size = new Size(65, 15);
        headerLine1Label.TabIndex = 0;
        headerLine1Label.Text = "Строка 1:";
        // 
        // headerLine1TextBox
        // 
        headerLine1TextBox.BorderStyle = BorderStyle.FixedSingle;
        headerLine1TextBox.Dock = DockStyle.Fill;
        headerLine1TextBox.Location = new Point(73, 0);
        headerLine1TextBox.Margin = new Padding(0, 0, 0, 8);
        headerLine1TextBox.MaxLength = 200;
        headerLine1TextBox.Name = "headerLine1TextBox";
        headerLine1TextBox.Size = new Size(419, 27);
        headerLine1TextBox.TabIndex = 1;
        // 
        // headerLine2Label
        // 
        headerLine2Label.AutoSize = true;
        headerLine2Label.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        headerLine2Label.ForeColor = Color.FromArgb(80, 80, 80);
        headerLine2Label.Location = new Point(0, 38);
        headerLine2Label.Margin = new Padding(0, 3, 8, 6);
        headerLine2Label.Name = "headerLine2Label";
        headerLine2Label.Size = new Size(65, 15);
        headerLine2Label.TabIndex = 2;
        headerLine2Label.Text = "Строка 2:";
        // 
        // headerLine2TextBox
        // 
        headerLine2TextBox.BorderStyle = BorderStyle.FixedSingle;
        headerLine2TextBox.Dock = DockStyle.Fill;
        headerLine2TextBox.Location = new Point(73, 35);
        headerLine2TextBox.Margin = new Padding(0, 0, 0, 8);
        headerLine2TextBox.MaxLength = 200;
        headerLine2TextBox.Name = "headerLine2TextBox";
        headerLine2TextBox.Size = new Size(419, 27);
        headerLine2TextBox.TabIndex = 3;
        // 
        // headerLine3Label
        // 
        headerLine3Label.AutoSize = true;
        headerLine3Label.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        headerLine3Label.ForeColor = Color.FromArgb(80, 80, 80);
        headerLine3Label.Location = new Point(0, 73);
        headerLine3Label.Margin = new Padding(0, 3, 8, 6);
        headerLine3Label.Name = "headerLine3Label";
        headerLine3Label.Size = new Size(65, 15);
        headerLine3Label.TabIndex = 4;
        headerLine3Label.Text = "Строка 3:";
        // 
        // headerLine3TextBox
        // 
        headerLine3TextBox.BorderStyle = BorderStyle.FixedSingle;
        headerLine3TextBox.Dock = DockStyle.Fill;
        headerLine3TextBox.Location = new Point(73, 70);
        headerLine3TextBox.Margin = new Padding(0, 0, 0, 8);
        headerLine3TextBox.MaxLength = 200;
        headerLine3TextBox.Name = "headerLine3TextBox";
        headerLine3TextBox.Size = new Size(419, 27);
        headerLine3TextBox.TabIndex = 5;
        // 
        // headerLine4Label
        // 
        headerLine4Label.AutoSize = true;
        headerLine4Label.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        headerLine4Label.ForeColor = Color.FromArgb(80, 80, 80);
        headerLine4Label.Location = new Point(0, 108);
        headerLine4Label.Margin = new Padding(0, 3, 8, 6);
        headerLine4Label.Name = "headerLine4Label";
        headerLine4Label.Size = new Size(65, 15);
        headerLine4Label.TabIndex = 6;
        headerLine4Label.Text = "Строка 4:";
        // 
        // headerLine4TextBox
        // 
        headerLine4TextBox.BorderStyle = BorderStyle.FixedSingle;
        headerLine4TextBox.Dock = DockStyle.Fill;
        headerLine4TextBox.Location = new Point(73, 105);
        headerLine4TextBox.Margin = new Padding(0, 0, 0, 8);
        headerLine4TextBox.MaxLength = 200;
        headerLine4TextBox.Name = "headerLine4TextBox";
        headerLine4TextBox.Size = new Size(419, 27);
        headerLine4TextBox.TabIndex = 7;
        // 
        // buttonsPanel
        // 
        buttonsPanel.AutoSize = true;
        buttonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);
        buttonsPanel.Dock = DockStyle.Fill;
        buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonsPanel.Location = new Point(12, 327);
        buttonsPanel.Margin = new Padding(12, 12, 12, 12);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Size = new Size(516, 21);
        buttonsPanel.TabIndex = 1;
        // 
        // saveButton
        // 
        saveButton.AutoSize = true;
        saveButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        saveButton.Location = new Point(433, 0);
        saveButton.Margin = new Padding(0);
        saveButton.Name = "saveButton";
        saveButton.Padding = new Padding(12, 4, 12, 4);
        saveButton.Size = new Size(83, 29);
        saveButton.TabIndex = 1;
        saveButton.Text = "Сохранить";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += OnSaveClick;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(332, 0);
        cancelButton.Margin = new Padding(8, 0, 0, 0);
        cancelButton.Name = "cancelButton";
        cancelButton.Padding = new Padding(12, 4, 12, 4);
        cancelButton.Size = new Size(93, 29);
        cancelButton.TabIndex = 0;
        cancelButton.Text = "Отмена";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += OnCancelClick;
        // 
        // SettingsForm
        // 
        AcceptButton = saveButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(540, 360);
        Controls.Add(mainLayout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        Padding = new Padding(0);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Настройки";
        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        contentLayout.ResumeLayout(false);
        contentLayout.PerformLayout();
        pdfPathPanel.ResumeLayout(false);
        pdfPathPanel.PerformLayout();
        headerGroup.ResumeLayout(false);
        headerGroup.PerformLayout();
        headerLayout.ResumeLayout(false);
        headerLayout.PerformLayout();
        buttonsPanel.ResumeLayout(false);
        buttonsPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel mainLayout;
    private TableLayoutPanel contentLayout;
    private Label pdfPathLabel;
    private FlowLayoutPanel pdfPathPanel;
    private TextBox pdfPathTextBox;
    private Button browseButton;
    private GroupBox headerGroup;
    private TableLayoutPanel headerLayout;
    private Label headerLine1Label;
    private TextBox headerLine1TextBox;
    private Label headerLine2Label;
    private TextBox headerLine2TextBox;
    private Label headerLine3Label;
    private TextBox headerLine3TextBox;
    private Label headerLine4Label;
    private TextBox headerLine4TextBox;
    private FlowLayoutPanel buttonsPanel;
    private Button saveButton;
    private Button cancelButton;
}
