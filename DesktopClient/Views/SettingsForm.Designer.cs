using System.Drawing;
using System.Windows.Forms;

namespace MicroluxErgConnect.Views;

partial class SettingsForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

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

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        mainLayout = new TableLayoutPanel();
        pdfPathLabel = new Label();
        pdfPathPanel = new TableLayoutPanel();
        pdfPathTextBox = new TextBox();
        browseButton = new Button();
        headerLine1Label = new Label();
        headerLine2Label = new Label();
        headerLine3Label = new Label();
        headerLine4Label = new Label();
        headerLine1TextBox = new TextBox();
        headerLine2TextBox = new TextBox();
        headerLine3TextBox = new TextBox();
        headerLine4TextBox = new TextBox();
        buttonsPanel = new FlowLayoutPanel();
        okButton = new Button();
        cancelButton = new Button();
        SuspendLayout();
        // 
        // mainLayout
        // 
        mainLayout.ColumnCount = 2;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.Controls.Add(pdfPathLabel, 0, 0);
        mainLayout.Controls.Add(pdfPathPanel, 1, 0);
        mainLayout.Controls.Add(headerLine1Label, 0, 1);
        mainLayout.Controls.Add(headerLine1TextBox, 1, 1);
        mainLayout.Controls.Add(headerLine2Label, 0, 2);
        mainLayout.Controls.Add(headerLine2TextBox, 1, 2);
        mainLayout.Controls.Add(headerLine3Label, 0, 3);
        mainLayout.Controls.Add(headerLine3TextBox, 1, 3);
        mainLayout.Controls.Add(headerLine4Label, 0, 4);
        mainLayout.Controls.Add(headerLine4TextBox, 1, 4);
        mainLayout.Controls.Add(buttonsPanel, 0, 5);
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Location = new Point(12, 12);
        mainLayout.Name = "mainLayout";
        mainLayout.Padding = new Padding(0, 0, 0, 8);
        mainLayout.RowCount = 6;
        mainLayout.RowStyles.Add(new RowStyle());
        mainLayout.RowStyles.Add(new RowStyle());
        mainLayout.RowStyles.Add(new RowStyle());
        mainLayout.RowStyles.Add(new RowStyle());
        mainLayout.RowStyles.Add(new RowStyle());
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        mainLayout.Size = new Size(560, 256);
        mainLayout.TabIndex = 0;
        // 
        // pdfPathLabel
        // 
        pdfPathLabel.AutoSize = true;
        pdfPathLabel.Dock = DockStyle.Fill;
        pdfPathLabel.Location = new Point(0, 0);
        pdfPathLabel.Margin = new Padding(0, 0, 12, 12);
        pdfPathLabel.Name = "pdfPathLabel";
        pdfPathLabel.Size = new Size(141, 27);
        pdfPathLabel.TabIndex = 0;
        pdfPathLabel.Text = "Папка для PDF-отчетов:";
        pdfPathLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // pdfPathPanel
        // 
        pdfPathPanel.AutoSize = true;
        pdfPathPanel.ColumnCount = 2;
        pdfPathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pdfPathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pdfPathPanel.Controls.Add(pdfPathTextBox, 0, 0);
        pdfPathPanel.Controls.Add(browseButton, 1, 0);
        pdfPathPanel.Dock = DockStyle.Fill;
        pdfPathPanel.Location = new Point(153, 0);
        pdfPathPanel.Margin = new Padding(0, 0, 0, 12);
        pdfPathPanel.Name = "pdfPathPanel";
        pdfPathPanel.RowCount = 1;
        pdfPathPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pdfPathPanel.Size = new Size(407, 27);
        pdfPathPanel.TabIndex = 1;
        // 
        // pdfPathTextBox
        // 
        pdfPathTextBox.Dock = DockStyle.Fill;
        pdfPathTextBox.Location = new Point(3, 3);
        pdfPathTextBox.Margin = new Padding(3, 3, 6, 3);
        pdfPathTextBox.Name = "pdfPathTextBox";
        pdfPathTextBox.Size = new Size(323, 23);
        pdfPathTextBox.TabIndex = 0;
        // 
        // browseButton
        // 
        browseButton.AutoSize = true;
        browseButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        browseButton.Location = new Point(335, 0);
        browseButton.Margin = new Padding(3, 0, 0, 0);
        browseButton.MinimumSize = new Size(60, 27);
        browseButton.Name = "browseButton";
        browseButton.Padding = new Padding(6, 2, 6, 2);
        browseButton.Size = new Size(72, 27);
        browseButton.TabIndex = 1;
        browseButton.Text = "Обзор…";
        browseButton.UseVisualStyleBackColor = true;
        browseButton.Click += OnBrowseClicked;
        // 
        // headerLine1Label
        // 
        headerLine1Label.AutoSize = true;
        headerLine1Label.Dock = DockStyle.Fill;
        headerLine1Label.Location = new Point(0, 39);
        headerLine1Label.Margin = new Padding(0, 0, 12, 8);
        headerLine1Label.Name = "headerLine1Label";
        headerLine1Label.Size = new Size(141, 23);
        headerLine1Label.TabIndex = 2;
        headerLine1Label.Text = "Шапка, строка 1:";
        headerLine1Label.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // headerLine2Label
        // 
        headerLine2Label.AutoSize = true;
        headerLine2Label.Dock = DockStyle.Fill;
        headerLine2Label.Location = new Point(0, 70);
        headerLine2Label.Margin = new Padding(0, 0, 12, 8);
        headerLine2Label.Name = "headerLine2Label";
        headerLine2Label.Size = new Size(141, 23);
        headerLine2Label.TabIndex = 4;
        headerLine2Label.Text = "Шапка, строка 2:";
        headerLine2Label.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // headerLine3Label
        // 
        headerLine3Label.AutoSize = true;
        headerLine3Label.Dock = DockStyle.Fill;
        headerLine3Label.Location = new Point(0, 101);
        headerLine3Label.Margin = new Padding(0, 0, 12, 8);
        headerLine3Label.Name = "headerLine3Label";
        headerLine3Label.Size = new Size(141, 23);
        headerLine3Label.TabIndex = 6;
        headerLine3Label.Text = "Шапка, строка 3:";
        headerLine3Label.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // headerLine4Label
        // 
        headerLine4Label.AutoSize = true;
        headerLine4Label.Dock = DockStyle.Fill;
        headerLine4Label.Location = new Point(0, 132);
        headerLine4Label.Margin = new Padding(0, 0, 12, 8);
        headerLine4Label.Name = "headerLine4Label";
        headerLine4Label.Size = new Size(141, 23);
        headerLine4Label.TabIndex = 8;
        headerLine4Label.Text = "Шапка, строка 4:";
        headerLine4Label.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // headerLine1TextBox
        // 
        headerLine1TextBox.Dock = DockStyle.Fill;
        headerLine1TextBox.Location = new Point(153, 39);
        headerLine1TextBox.Margin = new Padding(0, 0, 0, 8);
        headerLine1TextBox.Name = "headerLine1TextBox";
        headerLine1TextBox.Size = new Size(407, 23);
        headerLine1TextBox.TabIndex = 3;
        // 
        // headerLine2TextBox
        // 
        headerLine2TextBox.Dock = DockStyle.Fill;
        headerLine2TextBox.Location = new Point(153, 70);
        headerLine2TextBox.Margin = new Padding(0, 0, 0, 8);
        headerLine2TextBox.Name = "headerLine2TextBox";
        headerLine2TextBox.Size = new Size(407, 23);
        headerLine2TextBox.TabIndex = 5;
        // 
        // headerLine3TextBox
        // 
        headerLine3TextBox.Dock = DockStyle.Fill;
        headerLine3TextBox.Location = new Point(153, 101);
        headerLine3TextBox.Margin = new Padding(0, 0, 0, 8);
        headerLine3TextBox.Name = "headerLine3TextBox";
        headerLine3TextBox.Size = new Size(407, 23);
        headerLine3TextBox.TabIndex = 7;
        // 
        // headerLine4TextBox
        // 
        headerLine4TextBox.Dock = DockStyle.Fill;
        headerLine4TextBox.Location = new Point(153, 132);
        headerLine4TextBox.Margin = new Padding(0, 0, 0, 8);
        headerLine4TextBox.Name = "headerLine4TextBox";
        headerLine4TextBox.Size = new Size(407, 23);
        headerLine4TextBox.TabIndex = 9;
        // 
        // buttonsPanel
        // 
        buttonsPanel.AutoSize = true;
        buttonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        mainLayout.SetColumnSpan(buttonsPanel, 2);
        buttonsPanel.Dock = DockStyle.Fill;
        buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonsPanel.Location = new Point(0, 163);
        buttonsPanel.Margin = new Padding(0, 0, 0, 0);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Padding = new Padding(0, 8, 0, 0);
        buttonsPanel.Size = new Size(560, 93);
        buttonsPanel.TabIndex = 10;
        // 
        // okButton
        // 
        okButton.AutoSize = true;
        okButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        okButton.Location = new Point(457, 11);
        okButton.Margin = new Padding(8, 3, 0, 0);
        okButton.MinimumSize = new Size(100, 32);
        okButton.Name = "okButton";
        okButton.Padding = new Padding(10, 4, 10, 4);
        okButton.Size = new Size(103, 32);
        okButton.TabIndex = 0;
        okButton.Text = "Сохранить";
        okButton.UseVisualStyleBackColor = true;
        okButton.Click += OnSaveClicked;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(346, 11);
        cancelButton.Margin = new Padding(8, 3, 0, 0);
        cancelButton.MinimumSize = new Size(100, 32);
        cancelButton.Name = "cancelButton";
        cancelButton.Padding = new Padding(10, 4, 10, 4);
        cancelButton.Size = new Size(103, 32);
        cancelButton.TabIndex = 1;
        cancelButton.Text = "Отмена";
        cancelButton.UseVisualStyleBackColor = true;
        // 
        // SettingsForm
        // 
        AcceptButton = okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(584, 281);
        Controls.Add(mainLayout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Настройки";
        buttonsPanel.Controls.Add(okButton);
        buttonsPanel.Controls.Add(cancelButton);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel mainLayout;
    private Label pdfPathLabel;
    private TableLayoutPanel pdfPathPanel;
    private TextBox pdfPathTextBox;
    private Button browseButton;
    private Label headerLine1Label;
    private Label headerLine2Label;
    private Label headerLine3Label;
    private Label headerLine4Label;
    private TextBox headerLine1TextBox;
    private TextBox headerLine2TextBox;
    private TextBox headerLine3TextBox;
    private TextBox headerLine4TextBox;
    private FlowLayoutPanel buttonsPanel;
    private Button okButton;
    private Button cancelButton;
}
