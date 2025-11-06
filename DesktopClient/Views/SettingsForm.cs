using MicroluxErgConnect.Infrastructure;
using MicroluxErgConnect.Utils;
using MicroluxErgConnect.ViewModels;

namespace MicroluxErgConnect.Views
{
    public partial class SettingsForm : Form
    {
        private readonly MainViewModel _viewModel;

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

                var normalizedHeader = ReportHeaderFormatter.Normalize(headerTextBox.Text);
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
            headerTextBox.Text = ReportHeaderFormatter.JoinForEditor(ReportHeaderFormatter.Split(_viewModel.ReportHeader));
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
    }
}