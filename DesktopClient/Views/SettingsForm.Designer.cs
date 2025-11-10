using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MicroluxErgConnect.Views;

partial class SettingsForm
{
    private IContainer components = null!;
    private TableLayoutPanel layout;
    private Label folderLabel;
    private TextBox folderTextBox;
    private Button browseButton;
    private Label headerLabel;
    private TextBox headerTextBox;
    private Label headerHintLabel;
    private FlowLayoutPanel buttonsPanel;
    private Button saveButton;
    private Button cancelButton;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        ComponentResourceManager resources = new ComponentResourceManager(typeof(SettingsForm));
        layout = new TableLayoutPanel();
        folderLabel = new Label();
        folderTextBox = new TextBox();
        browseButton = new Button();
        headerLabel = new Label();
        headerTextBox = new TextBox();
        headerHintLabel = new Label();
        buttonsPanel = new FlowLayoutPanel();
        saveButton = new Button();
        cancelButton = new Button();
        layout.SuspendLayout();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // layout
        // 
        layout.ColumnCount = 3;
        layout.ColumnStyles.Add(new ColumnStyle());
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle());
        layout.Controls.Add(folderLabel, 0, 0);
        layout.Controls.Add(folderTextBox, 1, 0);
        layout.Controls.Add(browseButton, 2, 0);
        layout.Controls.Add(headerLabel, 0, 1);
        layout.Controls.Add(headerTextBox, 1, 1);
        layout.Controls.Add(headerHintLabel, 0, 2);
        layout.Controls.Add(buttonsPanel, 0, 3);
        layout.Dock = DockStyle.Fill;
        layout.Location = new Point(0, 0);
        layout.Margin = new Padding(0);
        layout.Name = "layout";
        layout.Padding = new Padding(0, 0, 0, 8);
        layout.RowCount = 4;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.Size = new Size(584, 281);
        layout.TabIndex = 0;
        // 
        // folderLabel
        // 
        folderLabel.AutoSize = true;
        folderLabel.Font = new Font("Segoe UI", 9F);
        folderLabel.ForeColor = Color.FromArgb(120, 128, 145);
        folderLabel.Location = new Point(0, 0);
        folderLabel.Margin = new Padding(0, 0, 12, 0);
        folderLabel.Name = "folderLabel";
        folderLabel.Size = new Size(172, 15);
        folderLabel.TabIndex = 0;
        folderLabel.Text = "Папка для сохранения отчетов:";
        // 
        // folderTextBox
        // 
        folderTextBox.Dock = DockStyle.Fill;
        folderTextBox.Location = new Point(155, 3);
        folderTextBox.Margin = new Padding(0, 3, 8, 3);
        folderTextBox.Name = "folderTextBox";
        folderTextBox.Size = new Size(334, 23);
        folderTextBox.TabIndex = 1;
        // 
        // browseButton
        // 
        browseButton.AutoSize = true;
        browseButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        browseButton.BackColor = Color.FromArgb(239, 246, 249);
        browseButton.Cursor = Cursors.Hand;
        browseButton.FlatAppearance.BorderSize = 0;
        browseButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 228, 236);
        browseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 236, 242);
        browseButton.FlatStyle = FlatStyle.Flat;
        browseButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        browseButton.ForeColor = Color.FromArgb(33, 37, 41);
        browseButton.Location = new Point(497, 0);
        browseButton.Margin = new Padding(0);
        browseButton.MinimumSize = new Size(80, 30);
        browseButton.Name = "browseButton";
        browseButton.Padding = new Padding(10, 5, 10, 5);
        browseButton.Size = new Size(87, 35);
        browseButton.TabIndex = 2;
        browseButton.Text = "Выбрать";
        browseButton.UseVisualStyleBackColor = false;
        browseButton.Click += OnBrowseClicked;
        // 
        // headerLabel
        // 
        headerLabel.AutoSize = true;
        headerLabel.Font = new Font("Segoe UI", 9F);
        headerLabel.ForeColor = Color.FromArgb(120, 128, 145);
        headerLabel.Location = new Point(0, 47);
        headerLabel.Margin = new Padding(0, 12, 12, 0);
        headerLabel.Name = "headerLabel";
        headerLabel.Size = new Size(143, 15);
        headerLabel.TabIndex = 3;
        headerLabel.Text = "Шапка отчета (4 строки):";
        // 
        // headerTextBox
        // 
        headerTextBox.AcceptsReturn = true;
        headerTextBox.BorderStyle = BorderStyle.FixedSingle;
        layout.SetColumnSpan(headerTextBox, 2);
        headerTextBox.Dock = DockStyle.Fill;
        headerTextBox.Location = new Point(155, 47);
        headerTextBox.Margin = new Padding(0, 12, 0, 3);
        headerTextBox.MinimumSize = new Size(200, 100);
        headerTextBox.Multiline = true;
        headerTextBox.Name = "headerTextBox";
        headerTextBox.ScrollBars = ScrollBars.Vertical;
        headerTextBox.Size = new Size(429, 163);
        headerTextBox.TabIndex = 4;
        // 
        // headerHintLabel
        // 
        headerHintLabel.AutoSize = true;
        layout.SetColumnSpan(headerHintLabel, 3);
        headerHintLabel.Font = new Font("Segoe UI", 8F);
        headerHintLabel.ForeColor = Color.FromArgb(120, 128, 145);
        headerHintLabel.Location = new Point(0, 215);
        headerHintLabel.Margin = new Padding(0, 2, 0, 8);
        headerHintLabel.Name = "headerHintLabel";
        headerHintLabel.Size = new Size(329, 13);
        headerHintLabel.TabIndex = 5;
        headerHintLabel.Text = "Каждая строка будет выведена отдельной строкой в отчете.";
        // 
        // buttonsPanel
        // 
        buttonsPanel.AutoSize = true;
        buttonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layout.SetColumnSpan(buttonsPanel, 3);
        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);
        buttonsPanel.Dock = DockStyle.Fill;
        buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonsPanel.Location = new Point(0, 236);
        buttonsPanel.Margin = new Padding(0);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Size = new Size(584, 37);
        buttonsPanel.TabIndex = 6;
        // 
        // saveButton
        // 
        saveButton.AutoSize = true;
        saveButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        saveButton.BackColor = Color.FromArgb(34, 158, 189);
        saveButton.Cursor = Cursors.Hand;
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 138, 166);
        saveButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 182, 214);
        saveButton.FlatStyle = FlatStyle.Flat;
        saveButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        saveButton.ForeColor = Color.White;
        saveButton.Location = new Point(474, 0);
        saveButton.Margin = new Padding(0);
        saveButton.MinimumSize = new Size(110, 34);
        saveButton.Name = "saveButton";
        saveButton.Padding = new Padding(10, 6, 10, 6);
        saveButton.Size = new Size(110, 37);
        saveButton.TabIndex = 0;
        saveButton.Text = "Сохранить";
        saveButton.UseVisualStyleBackColor = false;
        saveButton.Click += OnSaveClicked;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        cancelButton.BackColor = Color.FromArgb(239, 246, 249);
        cancelButton.Cursor = Cursors.Hand;
        cancelButton.FlatAppearance.BorderSize = 0;
        cancelButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 228, 236);
        cancelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 236, 242);
        cancelButton.FlatStyle = FlatStyle.Flat;
        cancelButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        cancelButton.ForeColor = Color.FromArgb(33, 37, 41);
        cancelButton.Location = new Point(372, 0);
        cancelButton.Margin = new Padding(0, 0, 12, 0);
        cancelButton.MinimumSize = new Size(90, 34);
        cancelButton.Name = "cancelButton";
        cancelButton.Padding = new Padding(10, 6, 10, 6);
        cancelButton.Size = new Size(90, 37);
        cancelButton.TabIndex = 1;
        cancelButton.Text = "Отмена";
        cancelButton.UseVisualStyleBackColor = false;
        cancelButton.Click += OnCancelClicked;
        // 
        // SettingsForm
        // 
        AcceptButton = saveButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(584, 281);
        Controls.Add(layout);
        Font = new Font("Segoe UI", 9F);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Margin = new Padding(4, 3, 4, 3);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(600, 320);
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Настройки";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        buttonsPanel.ResumeLayout(false);
        buttonsPanel.PerformLayout();
        ResumeLayout(false);
    }
}
