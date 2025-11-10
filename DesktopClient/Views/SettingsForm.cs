using MicroluxErgConnect.Infrastructure;
using MicroluxErgConnect.Utils;
using MicroluxErgConnect.ViewModels;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MicroluxErgConnect.Views
{
    public partial class SettingsForm : Form
    {
        private readonly MainViewModel _viewModel;
        private static readonly Color BorderColor = Color.FromArgb(206, 212, 218);
        private static readonly Color PlaceholderColor = Color.FromArgb(148, 155, 170);
        private readonly string[] _defaultHeaderLines =
        {
            "Наименование клиники",
            "Адрес",
            "Телефон",
            "Email и прочая информация"
        };
        private bool _isHeaderPlaceholderActive;

        public SettingsForm()
        {
            InitializeComponent();
            _viewModel = AppServices.MainViewModel;
        }

        private async Task ApplySettingsAsync()
        {
            ToggleControls(false);
            try
            {
                var (success, error) = await _viewModel.TryUpdateReportsDirectoryAsync(folderTextBox.Text);
                if (!success)
                {
                    var message = string.IsNullOrWhiteSpace(error) ? "Не удалось обновить каталог отчетов." : error;
                    MessageBox.Show(this, message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var normalizedHeader = ReportHeaderFormatter.Normalize(GetHeaderTextForSave());
                if (!string.Equals(normalizedHeader, _viewModel.ReportHeader, StringComparison.Ordinal))
                {
                    AppServices.Log.Info("Шапка отчета обновлена через окно настроек.");
                    _viewModel.ReportHeader = normalizedHeader;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                ToggleControls(true);
            }
        }

        private void OnBrowseClicked(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = ResolveInitialFolder(),
                Description = "Выберите папку для сохранения PDF-отчетов"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                folderTextBox.Text = dialog.SelectedPath;
            }
        }

        private void OnCancelClicked(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            await ApplySettingsAsync();
        }

        private void ToggleControls(bool enabled)
        {
            browseButton.Enabled = enabled;
            saveButton.Enabled = enabled;
            cancelButton.Enabled = enabled;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            folderTextBox.Text = _viewModel.ReportsDirectory;
            ApplyHeaderFromSettings();
        }

        public string? ResolveInitialFolder()
        {
            var current = folderTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(current) && Directory.Exists(current))
            {
                return current;
            }

            if (!string.IsNullOrEmpty(_viewModel.ReportsDirectory) && Directory.Exists(_viewModel.ReportsDirectory))
            {
                return _viewModel.ReportsDirectory;
            }

            return AppContext.BaseDirectory;
        }

        private void ApplyHeaderFromSettings()
        {
            var lines = ReportHeaderFormatter.Split(_viewModel.ReportHeader);
            var hasContent = lines.Any(l => !string.IsNullOrWhiteSpace(l));
            if (hasContent)
            {
                _isHeaderPlaceholderActive = false;
                headerRichTextBox.ForeColor = SystemColors.WindowText;
                headerRichTextBox.Text = ReportHeaderFormatter.JoinForEditor(lines);
                CenterHeaderText();
            }
            else
            {
                ShowHeaderPlaceholder();
            }
        }

        private void ShowHeaderPlaceholder()
        {
            _isHeaderPlaceholderActive = true;
            headerRichTextBox.ForeColor = PlaceholderColor;
            headerRichTextBox.Text = string.Join(Environment.NewLine, _defaultHeaderLines);
            CenterHeaderText();
        }

        private void CenterHeaderText()
        {
            if (headerRichTextBox.IsDisposed)
            {
                return;
            }

            var selectionStart = headerRichTextBox.SelectionStart;
            var selectionLength = headerRichTextBox.SelectionLength;
            headerRichTextBox.SelectAll();
            headerRichTextBox.SelectionAlignment = HorizontalAlignment.Center;
            headerRichTextBox.Select(selectionStart, selectionLength);
        }

        private void OnHeaderEnter(object? sender, EventArgs e)
        {
            if (!_isHeaderPlaceholderActive)
            {
                return;
            }

            _isHeaderPlaceholderActive = false;
            headerRichTextBox.Clear();
            headerRichTextBox.ForeColor = SystemColors.WindowText;
            CenterHeaderText();
        }

        private void OnHeaderLeave(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(headerRichTextBox.Text))
            {
                ShowHeaderPlaceholder();
            }
            else
            {
                CenterHeaderText();
            }
        }

        private void OnHeaderTextChanged(object? sender, EventArgs e)
        {
            if (_isHeaderPlaceholderActive)
            {
                return;
            }

            CenterHeaderText();
        }

        private void OnBorderContainerPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel panel)
            {
                return;
            }

            var rect = panel.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawRectangle(pen, rect);
        }

        private string GetHeaderTextForSave()
        {
            if (_isHeaderPlaceholderActive)
            {
                return string.Empty;
            }

            return headerRichTextBox.Text;
        }
    }
}