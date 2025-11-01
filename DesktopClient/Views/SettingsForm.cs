using System;
using System.IO;
using System.Windows.Forms;
using MicroluxErgConnect.Utils;
using MicroluxErgConnect.ViewModels;

namespace MicroluxErgConnect.Views;

public partial class SettingsForm : Form
{
    private readonly MainViewModel _viewModel;

    public SettingsForm(MainViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        LoadInitialValues();
    }

    private void LoadInitialValues()
    {
        pdfPathTextBox.Text = _viewModel.PdfReportsDirectory;
        var lines = ReportHeaderFormatter.Split(_viewModel.ReportHeader);
        headerLine1TextBox.Text = lines.Length > 0 ? lines[0] : string.Empty;
        headerLine2TextBox.Text = lines.Length > 1 ? lines[1] : string.Empty;
        headerLine3TextBox.Text = lines.Length > 2 ? lines[2] : string.Empty;
        headerLine4TextBox.Text = lines.Length > 3 ? lines[3] : string.Empty;
    }

    private void OnBrowseClicked(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку, в которую будут сохраняться PDF-отчеты",
            SelectedPath = ResolveInitialPath(),
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        pdfPathTextBox.Text = dialog.SelectedPath;
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var pathText = pdfPathTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(pathText))
        {
            MessageBox.Show(
                this,
                "Укажите папку для сохранения PDF-отчетов.",
                "Microlux ERG-Connect",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            pdfPathTextBox.Focus();
            return;
        }

        string normalizedPath;
        try
        {
            normalizedPath = NormalizePath(pathText);
            Directory.CreateDirectory(normalizedPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Не удалось использовать указанный каталог: {ex.Message}",
                "Microlux ERG-Connect",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            pdfPathTextBox.Focus();
            return;
        }

        try
        {
            _viewModel.PdfReportsDirectory = normalizedPath;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Microlux ERG-Connect",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            pdfPathTextBox.Focus();
            return;
        }

        pdfPathTextBox.Text = normalizedPath;

        var headerLines = new[]
        {
            headerLine1TextBox.Text ?? string.Empty,
            headerLine2TextBox.Text ?? string.Empty,
            headerLine3TextBox.Text ?? string.Empty,
            headerLine4TextBox.Text ?? string.Empty
        };
        var headerValue = string.Join(Environment.NewLine, headerLines);
        _viewModel.ReportHeader = headerValue;

        DialogResult = DialogResult.OK;
        Close();
    }

    private static string NormalizePath(string value)
    {
        var expanded = Environment.ExpandEnvironmentVariables(value);
        return Path.GetFullPath(expanded);
    }

    private string ResolveInitialPath()
    {
        var text = pdfPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return _viewModel.PdfReportsDirectory;
        }

        try
        {
            return NormalizePath(text);
        }
        catch
        {
            return _viewModel.PdfReportsDirectory;
        }
    }
}
