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
    private Panel folderInnerPanel;
    private TextBox folderTextBox;
    private Button browseButton;
    private Label headerLabel;
    private Panel headerContainer;
    private Panel headerInnerPanel;
    private RichTextBox headerTextBox;
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
        folderInnerPanel = new Panel();
        folderTextBox = new TextBox();
        browseButton = new Button();
        headerLabel = new Label();
        headerContainer = new Panel();
        headerInnerPanel = new Panel();
        headerTextBox = new RichTextBox();
        headerHintLabel = new Label();
        buttonsPanel = new FlowLayoutPanel();
        saveButton = new Button();
        cancelButton = new Button();
        layout.SuspendLayout();
        folderContainer.SuspendLayout();
        folderInnerPanel.SuspendLayout();
        headerContainer.SuspendLayout();
        headerInnerPanel.SuspendLayout();
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
        layout.Controls.Add(folderContainer, 1, 0);
        layout.Controls.Add(browseButton, 2, 0);
        layout.Controls.Add(headerLabel, 0, 1);
        layout.Controls.Add(headerContainer, 1, 1);
        layout.Controls.Add(headerHintLabel, 0, 2);
        layout.Controls.Add(buttonsPanel, 0, 3);
        layout.Dock = DockStyle.Fill;
        layout.Location = new Point(0, 0);
        layout.Margin = new Padding(0);
        layout.Name = "layout";
        layout.Padding = new Padding(20, 20, 20, 12);
        layout.RowCount = 4;
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle());
        layout.Size = new Size(560, 340);
        layout.TabIndex = 0;
        //
        // folderLabel
        //
        folderLabel.AutoSize = true;
        folderLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        folderLabel.ForeColor = Color.FromArgb(33, 37, 41);
        folderLabel.Margin = new Padding(0, 0, 12, 0);
        folderLabel.Name = "folderLabel";
        folderLabel.Size = new Size(191, 15);
        folderLabel.TabIndex = 0;
        folderLabel.Text = "Папка для сохранения отчетов";
        //
        // folderContainer
        //
        folderContainer.BackColor = Color.FromArgb(214, 219, 226);
        folderContainer.Controls.Add(folderInnerPanel);
        folderContainer.Dock = DockStyle.Fill;
        folderContainer.Location = new Point(223, 23);
        folderContainer.Margin = new Padding(0, 3, 8, 3);
        folderContainer.Name = "folderContainer";
        folderContainer.Padding = new Padding(1);
        folderContainer.Size = new Size(309, 32);
        folderContainer.TabIndex = 1;
        //
        // folderInnerPanel
        //
        folderInnerPanel.BackColor = Color.White;
        folderInnerPanel.Controls.Add(folderTextBox);
        folderInnerPanel.Dock = DockStyle.Fill;
        folderInnerPanel.Location = new Point(1, 1);
        folderInnerPanel.Margin = new Padding(0);
        folderInnerPanel.Name = "folderInnerPanel";
        folderInnerPanel.Padding = new Padding(10, 6, 10, 6);
        folderInnerPanel.Size = new Size(307, 30);
        folderInnerPanel.TabIndex = 0;
        //
        // folderTextBox
        //
        folderTextBox.BorderStyle = BorderStyle.None;
        folderTextBox.Dock = DockStyle.Fill;
        folderTextBox.Font = new Font("Segoe UI", 9F);
        folderTextBox.ForeColor = Color.FromArgb(33, 37, 41);
        folderTextBox.Location = new Point(10, 6);
        folderTextBox.Margin = new Padding(0);
        folderTextBox.Name = "folderTextBox";
        folderTextBox.Size = new Size(287, 16);
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
        browseButton.Location = new Point(540, 20);
        browseButton.Margin = new Padding(0, 3, 0, 3);
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
        headerLabel.Location = new Point(0, 68);
        headerLabel.Margin = new Padding(0, 12, 12, 0);
        headerLabel.Name = "headerLabel";
        headerLabel.Size = new Size(120, 15);
        headerLabel.TabIndex = 3;
        headerLabel.Text = "Реквизиты клиники";
        //
        // headerContainer
        //
        layout.SetColumnSpan(headerContainer, 2);
        headerContainer.BackColor = Color.FromArgb(214, 219, 226);
        headerContainer.Controls.Add(headerInnerPanel);
        headerContainer.Dock = DockStyle.Fill;
        headerContainer.Location = new Point(223, 68);
        headerContainer.Margin = new Padding(0, 12, 0, 3);
        headerContainer.Name = "headerContainer";
        headerContainer.Padding = new Padding(1);
        headerContainer.Size = new Size(404, 189);
        headerContainer.TabIndex = 4;
        //
        // headerInnerPanel
        //
        headerInnerPanel.BackColor = Color.White;
        headerInnerPanel.Controls.Add(headerTextBox);
        headerInnerPanel.Dock = DockStyle.Fill;
        headerInnerPanel.Location = new Point(1, 1);
        headerInnerPanel.Margin = new Padding(0);
        headerInnerPanel.Name = "headerInnerPanel";
        headerInnerPanel.Padding = new Padding(12, 10, 12, 10);
        headerInnerPanel.Size = new Size(402, 187);
        headerInnerPanel.TabIndex = 0;
        //
        // headerTextBox
        //
        headerTextBox.BorderStyle = BorderStyle.None;
        headerTextBox.DetectUrls = false;
        headerTextBox.Dock = DockStyle.Fill;
        headerTextBox.Font = new Font("Segoe UI", 10F);
        headerTextBox.ForeColor = Color.FromArgb(33, 37, 41);
        headerTextBox.Location = new Point(12, 10);
        headerTextBox.Margin = new Padding(0);
        headerTextBox.MinimumSize = new Size(200, 120);
        headerTextBox.Name = "headerTextBox";
        headerTextBox.ScrollBars = RichTextBoxScrollBars.None;
        headerTextBox.ShortcutsEnabled = true;
        headerTextBox.Size = new Size(378, 167);
        headerTextBox.TabIndex = 0;
        headerTextBox.Text = "";
        headerTextBox.Enter += OnHeaderEnter;
        headerTextBox.Leave += OnHeaderLeave;
        headerTextBox.TextChanged += OnHeaderTextChanged;
        //
        // headerHintLabel
        //
        headerHintLabel.AutoSize = true;
        layout.SetColumnSpan(headerHintLabel, 3);
        headerHintLabel.Font = new Font("Segoe UI", 8F);
        headerHintLabel.ForeColor = Color.FromArgb(120, 128, 145);
        headerHintLabel.Location = new Point(20, 260);
        headerHintLabel.Margin = new Padding(0, 6, 0, 8);
        headerHintLabel.Name = "headerHintLabel";
        headerHintLabel.Size = new Size(362, 13);
        headerHintLabel.TabIndex = 5;
        headerHintLabel.Text = "Введите до четырёх строк — каждая появится в отчёте по центру.";
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
        buttonsPanel.Location = new Point(20, 289);
        buttonsPanel.Margin = new Padding(0, 12, 0, 0);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Size = new Size(520, 37);
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
        ClientSize = new Size(560, 340);
        Controls.Add(layout);
        Font = new Font("Segoe UI", 9F);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Margin = new Padding(4, 3, 4, 3);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(560, 340);
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Настройки";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        folderContainer.ResumeLayout(false);
        folderInnerPanel.ResumeLayout(false);
        folderInnerPanel.PerformLayout();
        headerContainer.ResumeLayout(false);
        headerInnerPanel.ResumeLayout(false);
        buttonsPanel.ResumeLayout(false);
        buttonsPanel.PerformLayout();
        ResumeLayout(false);
    }
}
