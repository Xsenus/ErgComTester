using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using MicroluxErgConnect.Infrastructure;
using MicroluxErgConnect.Services;
using MicroluxErgConnect.Utils;

namespace MicroluxErgConnect.Views;

public partial class SettingsForm : Form
{
    private readonly SettingsService _settings;

    public SettingsForm()
    {
        InitializeComponent();
        _settings = AppServices.Settings;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settings.Current;
        pdfPathTextBox.Text = settings.PdfReportsDirectory;
        var lines = ReportHeaderFormatter.Split(settings.ReportHeader);
        headerLine1TextBox.Text = lines[0];
        headerLine2TextBox.Text = lines[1];
        headerLine3TextBox.Text = lines[2];
        headerLine4TextBox.Text = lines[3];
    }

    private void OnBrowseClick(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку для сохранения PDF-отчетов",
            SelectedPath = GetInitialDirectory()
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            pdfPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private string GetInitialDirectory()
    {
        var candidate = pdfPathTextBox.Text;
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            try
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Игнорируем ошибки проверки каталога, используем путь по умолчанию.
            }
        }

        return _settings.Current.PdfReportsDirectory;
    }

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        var pdfDirectory = pdfPathTextBox.Text.Trim();
        if (string.IsNullOrEmpty(pdfDirectory))
        {
            MessageBox.Show(this, "Укажите папку для PDF-отчетов.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            pdfPathTextBox.Focus();
            return;
        }

        try
        {
            pdfDirectory = Path.GetFullPath(pdfDirectory);
            Directory.CreateDirectory(pdfDirectory);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось подготовить папку: {ex.Message}", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var headerLines = new[]
        {
            headerLine1TextBox.Text,
            headerLine2TextBox.Text,
            headerLine3TextBox.Text,
            headerLine4TextBox.Text
        };
        var headerValue = ReportHeaderFormatter.Normalize(string.Join("\n", headerLines));

        try
        {
            await _settings.UpdateAsync(settings =>
            {
                settings.PdfReportsDirectory = pdfDirectory;
                settings.ReportHeader = headerValue;
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось сохранить настройки: {ex.Message}", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
