using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MicroluxErgConnect.Views;

partial class SettingsForm
{
    private IContainer components = null!;
    private TableLayoutPanel layout;
    private Label folderLabel;
    private Panel folderContainer;
    private TextBox folderTextBox;
    private Button browseButton;
    private Label headerLabel;
    private Panel headerContainer;
    private RichTextBox headerRichTextBox;
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
        folderContainer = new Panel();
        folderTextBox = new TextBox();
        browseButton = new Button();
        headerLabel = new Label();
        headerContainer = new Panel();
        headerRichTextBox = new RichTextBox();
        headerHintLabel = new Label();
        buttonsPanel = new FlowLayoutPanel();
        saveButton = new Button();
        cancelButton = new Button();
        layout.SuspendLayout();
        folderContainer.SuspendLayout();
        headerContainer.SuspendLayout();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        //
        // layout
        //
        layout.ColumnCount = 2;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle());
        layout.Controls.Add(folderLabel, 0, 0);
        layout.SetColumnSpan(folderLabel, 2);
        layout.Controls.Add(folderContainer, 0, 1);
        layout.Controls.Add(browseButton, 1, 1);
        layout.Controls.Add(headerLabel, 0, 2);
        layout.SetColumnSpan(headerLabel, 2);
        layout.Controls.Add(headerContainer, 0, 3);
        layout.SetColumnSpan(headerContainer, 2);
        layout.Controls.Add(headerHintLabel, 0, 4);
        layout.SetColumnSpan(headerHintLabel, 2);
        layout.Controls.Add(buttonsPanel, 0, 5);
        layout.SetColumnSpan(buttonsPanel, 2);
        layout.Dock = DockStyle.Fill;
        layout.Location = new Point(0, 0);
        layout.Margin = new Padding(0);
        layout.Name = "layout";
        layout.Padding = new Padding(0, 0, 0, 8);
        layout.RowCount = 6;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.Size = new Size(560, 312);
        layout.TabIndex = 0;
        //
        // folderLabel
        //
        folderLabel.AutoSize = true;
        folderLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        folderLabel.ForeColor = Color.FromArgb(33, 37, 41);
        folderLabel.Location = new Point(0, 0);
        folderLabel.Margin = new Padding(0, 0, 0, 6);
        folderLabel.Name = "folderLabel";
        folderLabel.Size = new Size(198, 15);
        folderLabel.TabIndex = 0;
        folderLabel.Text = "Папка для сохранения отчетов";
        //
        // folderContainer
        //
        folderContainer.BackColor = Color.White;
        folderContainer.Controls.Add(folderTextBox);
        folderContainer.Dock = DockStyle.Fill;
        folderContainer.Location = new Point(0, 21);
        folderContainer.Margin = new Padding(0, 0, 12, 0);
        folderContainer.MinimumSize = new Size(200, 40);
        folderContainer.Name = "folderContainer";
        folderContainer.Padding = new Padding(10, 8, 10, 8);
        folderContainer.Size = new Size(436, 44);
        folderContainer.TabIndex = 1;
        folderContainer.Paint += OnBorderContainerPaint;
        //
        // folderTextBox
        //
        folderTextBox.BorderStyle = BorderStyle.None;
        folderTextBox.Dock = DockStyle.Fill;
        folderTextBox.Location = new Point(10, 8);
        folderTextBox.Margin = new Padding(0);
        folderTextBox.Name = "folderTextBox";
        folderTextBox.Size = new Size(416, 16);
        folderTextBox.TabIndex = 0;
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
        browseButton.Location = new Point(448, 21);
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
        headerLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        headerLabel.ForeColor = Color.FromArgb(33, 37, 41);
        headerLabel.Location = new Point(0, 77);
        headerLabel.Margin = new Padding(0, 12, 0, 6);
        headerLabel.Name = "headerLabel";
        headerLabel.Size = new Size(127, 15);
        headerLabel.TabIndex = 3;
        headerLabel.Text = "Реквизиты клиники";
        //
        // headerContainer
        //
        headerContainer.BackColor = Color.White;
        headerContainer.Controls.Add(headerRichTextBox);
        headerContainer.Dock = DockStyle.Fill;
        headerContainer.Location = new Point(0, 98);
        headerContainer.Margin = new Padding(0);
        headerContainer.MinimumSize = new Size(200, 160);
        headerContainer.Name = "headerContainer";
        headerContainer.Padding = new Padding(10);
        headerContainer.Size = new Size(535, 173);
        headerContainer.TabIndex = 4;
        headerContainer.Paint += OnBorderContainerPaint;
        //
        // headerRichTextBox
        //
        headerRichTextBox.BorderStyle = BorderStyle.None;
        headerRichTextBox.DetectUrls = false;
        headerRichTextBox.Dock = DockStyle.Fill;
        headerRichTextBox.Font = new Font("Segoe UI", 10F);
        headerRichTextBox.Location = new Point(10, 10);
        headerRichTextBox.Margin = new Padding(0);
        headerRichTextBox.MinimumSize = new Size(200, 120);
        headerRichTextBox.Name = "headerRichTextBox";
        headerRichTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
        headerRichTextBox.Size = new Size(515, 153);
        headerRichTextBox.TabIndex = 0;
        headerRichTextBox.Text = "";
        headerRichTextBox.Enter += OnHeaderEnter;
        headerRichTextBox.Leave += OnHeaderLeave;
        headerRichTextBox.TextChanged += OnHeaderTextChanged;
        //
        // headerHintLabel
        //
        headerHintLabel.AutoSize = true;
        headerHintLabel.Margin = new Padding(0, 8, 0, 12);
        headerHintLabel.Font = new Font("Segoe UI", 8F);
        headerHintLabel.ForeColor = Color.FromArgb(120, 128, 145);
        headerHintLabel.Location = new Point(0, 274);
        headerHintLabel.Name = "headerHintLabel";
        headerHintLabel.Size = new Size(329, 13);
        headerHintLabel.TabIndex = 5;
        headerHintLabel.Text = "Каждая строка будет выведена отдельной строкой в отчете.";
        //
        // buttonsPanel
        //
        buttonsPanel.AutoSize = true;
        buttonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);
        buttonsPanel.Dock = DockStyle.Fill;
        buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonsPanel.Location = new Point(0, 299);
        buttonsPanel.Margin = new Padding(0);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Size = new Size(535, 37);
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
        cancelButton.Location = new Point(373, 0);
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
        ClientSize = new Size(560, 320);
        Controls.Add(layout);
        Font = new Font("Segoe UI", 9F);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Margin = new Padding(4, 3, 4, 3);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(560, 320);
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Настройки";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        folderContainer.ResumeLayout(false);
        folderContainer.PerformLayout();
        headerContainer.ResumeLayout(false);
        headerContainer.PerformLayout();
        buttonsPanel.ResumeLayout(false);
        buttonsPanel.PerformLayout();
        ResumeLayout(false);
    }
}
