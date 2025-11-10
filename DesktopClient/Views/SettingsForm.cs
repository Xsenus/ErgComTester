using System.Drawing;
using MicroluxErgConnect.Infrastructure;
using MicroluxErgConnect.Utils;
using MicroluxErgConnect.ViewModels;

namespace MicroluxErgConnect.Views
{
    public partial class SettingsForm : Form
    {
        private readonly MainViewModel _viewModel;
        private readonly Color _headerTextColor = Color.FromArgb(33, 37, 41);
        private readonly Color _headerPlaceholderColor = Color.FromArgb(120, 128, 145);
        private readonly string[] _headerPlaceholderLines =
        {
            "Наименование клиники",
            "Адрес",
            "Телефон",
            "Email и прочая информация"
        };
        private bool _headerPlaceholderVisible;
        private bool _suppressHeaderTextChanged;

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
                var (success, error) = await _viewModel.TryUpdateReportsDirectoryAsync(folderTextBox.Text.Trim());
                if (!success)
                {
                    var message = string.IsNullOrWhiteSpace(error) ? "Не удалось обновить каталог отчетов." : error;
                    MessageBox.Show(this, message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var normalizedHeader = ReportHeaderFormatter.Normalize(GetHeaderEditorText());
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
            var combined = ReportHeaderFormatter.JoinForEditor(lines);
            if (string.IsNullOrWhiteSpace(combined))
            {
                ShowHeaderPlaceholder();
            }
            else
            {
                SetHeaderTextInternal(combined, _headerTextColor);
                _headerPlaceholderVisible = false;
            }
        }

        private void ShowHeaderPlaceholder()
        {
            _headerPlaceholderVisible = true;
            SetHeaderTextInternal(string.Join(Environment.NewLine, _headerPlaceholderLines), _headerPlaceholderColor);
        }

        private void HideHeaderPlaceholder()
        {
            if (!_headerPlaceholderVisible)
            {
                return;
            }

            _headerPlaceholderVisible = false;
            SetHeaderTextInternal(string.Empty, _headerTextColor);
        }

        private void EnsureHeaderPlaceholder()
        {
            if (string.IsNullOrWhiteSpace(headerTextBox.Text))
            {
                ShowHeaderPlaceholder();
            }
        }

        private void SetHeaderTextInternal(string text, Color color)
        {
            _suppressHeaderTextChanged = true;
            try
            {
                headerTextBox.ForeColor = color;
                headerTextBox.Text = text;
            }
            finally
            {
                _suppressHeaderTextChanged = false;
            }

            AlignHeaderText();
            headerTextBox.SelectionStart = headerTextBox.TextLength;
            headerTextBox.SelectionLength = 0;
        }

        private void AlignHeaderText()
        {
            if (headerTextBox.IsDisposed)
            {
                return;
            }

            var selectionStart = headerTextBox.SelectionStart;
            var selectionLength = headerTextBox.SelectionLength;
            headerTextBox.SelectAll();
            headerTextBox.SelectionAlignment = HorizontalAlignment.Center;
            headerTextBox.Select(selectionStart, selectionLength);
        }

        private string GetHeaderEditorText()
            => _headerPlaceholderVisible ? string.Empty : headerTextBox.Text;

        private void OnHeaderEnter(object? sender, EventArgs e)
        {
            if (_headerPlaceholderVisible)
            {
                HideHeaderPlaceholder();
            }
        }

        private void OnHeaderLeave(object? sender, EventArgs e)
        {
            EnsureHeaderPlaceholder();
        }

        private void OnHeaderTextChanged(object? sender, EventArgs e)
        {
            if (_suppressHeaderTextChanged || _headerPlaceholderVisible)
            {
                return;
            }

            AlignHeaderText();
        }
    }
}