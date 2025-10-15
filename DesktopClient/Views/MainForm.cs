using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using MicroluxErgConnect.Infrastructure;
using MicroluxErgConnect.Models;
using MicroluxErgConnect.ViewModels;

namespace MicroluxErgConnect.Views;

public partial class MainForm : Form
{
    private readonly MainViewModel _viewModel;
    private readonly BindingList<LogEntry> _logEntries = new();
    private bool _isExitRequested;
    private readonly EventHandler _checkUpdatesCanExecuteHandler;
    private readonly EventHandler _installUpdateCanExecuteHandler;

    public MainForm()
    {
        _viewModel = AppServices.MainViewModel;
        InitializeComponent();
        logsBindingSource.DataSource = _logEntries;
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        _viewModel.Logs.CollectionChanged += LogsOnCollectionChanged;
        _checkUpdatesCanExecuteHandler = (_, _) => UpdateCommandState();
        _installUpdateCanExecuteHandler = (_, _) => UpdateCommandState();
        _viewModel.CheckUpdatesCommand.CanExecuteChanged += _checkUpdatesCanExecuteHandler;
        _viewModel.InstallUpdateCommand.CanExecuteChanged += _installUpdateCanExecuteHandler;
        UpdateAll();
        UpdateCommandState();

        var version = typeof(MainForm).Assembly.GetName().Version?.ToString() ?? "1.0.0.0";
        versionStatusLabel.Text = $"Версия: {version}";
        AppServices.Log.Info("Главное окно инициализировано.");
    }

    private void OnFormLoaded(object? sender, EventArgs e)
    {
        PopulateLogs();
        ApplySettingsToInputs();

        var settings = AppServices.Settings.Current;
        if (settings.StartMinimized)
        {
            AppServices.Log.Info("Старт в свернутом состоянии по настройкам пользователя.");
            WindowState = FormWindowState.Minimized;
            if (settings.MinimizeToTray)
            {
                Hide();
            }
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_isExitRequested && AppServices.Settings.Current.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            AppServices.Log.Info("Окно скрыто в трей вместо завершения работы.");
        }
    }

    private void OnFormResized(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized && AppServices.Settings.Current.MinimizeToTray)
        {
            Hide();
            AppServices.Log.Info("Окно свернуто в трей.");
        }
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        AppServices.Log.Info("Окно восстановлено из трея.");
    }

    private void ExitApplication()
    {
        _isExitRequested = true;
        AppServices.Log.Info("Пользователь запросил завершение приложения из трея.");
        trayIcon.Visible = false;
        Close();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => ViewModelOnPropertyChanged(sender, e)));
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(MainViewModel.StatusText):
                statusValueLabel.Text = _viewModel.StatusText;
                break;
            case nameof(MainViewModel.CurrentPort):
                portValueLabel.Text = _viewModel.CurrentPort ?? "-";
                break;
            case nameof(MainViewModel.DeviceName):
                deviceValueLabel.Text = _viewModel.DeviceName ?? "-";
                break;
            case nameof(MainViewModel.SoftwareVersion):
                softwareValueLabel.Text = _viewModel.SoftwareVersion ?? "-";
                break;
            case nameof(MainViewModel.ReportName):
                reportValueLabel.Text = _viewModel.ReportName ?? "-";
                break;
            case nameof(MainViewModel.SyncStatus):
                syncStatusLabel.Text = _viewModel.SyncStatus;
                break;
            case nameof(MainViewModel.UpdateStatusText):
                updateStatusValueLabel.Text = _viewModel.UpdateStatusText;
                break;
            case nameof(MainViewModel.DeviceScanIntervalSeconds):
            case nameof(MainViewModel.DeviceReconnectDelaySeconds):
            case nameof(MainViewModel.BackgroundSyncIntervalMinutes):
            case nameof(MainViewModel.UpdateCheckIntervalMinutes):
            case nameof(MainViewModel.UpdateManifestUrl):
                ApplySettingsToInputs();
                break;
        }
    }

    private void LogsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new NotifyCollectionChangedEventHandler(LogsOnCollectionChanged), sender, e);
            return;
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _logEntries.Clear();
        }

        if (e.OldItems is { Count: > 0 })
        {
            foreach (var item in e.OldItems.Cast<LogEntry>())
            {
                _logEntries.Remove(item);
            }
        }

        if (e.NewItems is { Count: > 0 })
        {
            foreach (var item in e.NewItems.Cast<LogEntry>())
            {
                _logEntries.Add(item);
            }
            ScrollLogsToEnd();
        }
    }

    private void PopulateLogs()
    {
        _logEntries.Clear();
        foreach (var entry in _viewModel.Logs)
        {
            _logEntries.Add(entry);
        }
        ScrollLogsToEnd();
    }

    private void ScrollLogsToEnd()
    {
        if (logGridView.Rows.Count > 0)
        {
            var lastIndex = logGridView.Rows.Count - 1;
            try
            {
                logGridView.FirstDisplayedScrollingRowIndex = Math.Max(0, lastIndex);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Игнорируем, когда таблица еще не готова к прокрутке.
            }
            catch (InvalidOperationException)
            {
                // Игнорируем временные ошибки прокрутки.
            }
        }
    }

    private void ApplySettingsToInputs()
    {
        scanIntervalTextBox.Text = _viewModel.DeviceScanIntervalSeconds.ToString();
        reconnectDelayTextBox.Text = _viewModel.DeviceReconnectDelaySeconds.ToString();
        backgroundSyncTextBox.Text = _viewModel.BackgroundSyncIntervalMinutes.ToString();
        updateIntervalTextBox.Text = _viewModel.UpdateCheckIntervalMinutes.ToString();
        manifestUrlTextBox.Text = _viewModel.UpdateManifestUrl;
    }

    private void UpdateAll()
    {
        statusValueLabel.Text = _viewModel.StatusText;
        portValueLabel.Text = _viewModel.CurrentPort ?? "-";
        deviceValueLabel.Text = _viewModel.DeviceName ?? "-";
        softwareValueLabel.Text = _viewModel.SoftwareVersion ?? "-";
        reportValueLabel.Text = _viewModel.ReportName ?? "-";
        syncStatusLabel.Text = _viewModel.SyncStatus;
        updateStatusValueLabel.Text = _viewModel.UpdateStatusText;
    }

    private void UpdateCommandState()
    {
        checkUpdatesButton.Enabled = _viewModel.CheckUpdatesCommand.CanExecute(null);
        installUpdateButton.Enabled = _viewModel.InstallUpdateCommand.CanExecute(null);
        ForceCommandStateLogging();
    }

    private void ForceCommandStateLogging()
    {
        AppServices.Log.Debug($"Доступность команд: проверка обновлений={(checkUpdatesButton.Enabled ? "доступна" : "недоступна")}, установка={(installUpdateButton.Enabled ? "доступна" : "недоступна")}");
    }

    private void OnResetPortClicked(object? sender, EventArgs e)
    {
        if (_viewModel.ForceRescanCommand.CanExecute(null))
        {
            AppServices.Log.Info("Пользователь инициировал сброс запомненного порта.");
            _viewModel.ForceRescanCommand.Execute(null);
        }
    }

    private void OnOpenReportsClicked(object? sender, EventArgs e)
    {
        ExecuteSafeAsync(_viewModel.OpenReportsCommand, "открытие каталога отчетов");
    }

    private void OnOpenLogsClicked(object? sender, EventArgs e)
    {
        ExecuteSafeAsync(_viewModel.OpenLogsCommand, "открытие каталога логов");
    }

    private void OnCheckUpdatesClicked(object? sender, EventArgs e)
    {
        ExecuteSafeAsync(_viewModel.CheckUpdatesCommand, "проверку обновлений");
    }

    private void OnInstallUpdateClicked(object? sender, EventArgs e)
    {
        ExecuteSafeAsync(_viewModel.InstallUpdateCommand, "установку обновления");
    }

    private void ExecuteSafeAsync(RelayCommand command, string actionDescription)
    {
        if (!command.CanExecute(null))
        {
            AppServices.Log.Warn($"Команда на {actionDescription} в данный момент недоступна.");
            return;
        }

        try
        {
            command.Execute(null);
            AppServices.Log.Info($"Команда на {actionDescription} запущена пользователем.");
        }
        catch (Exception ex)
        {
            AppServices.Log.Error($"Ошибка при выполнении команды ({actionDescription}): {ex}");
            MessageBox.Show(
                $"Ошибка при выполнении действия: {ex.Message}",
                "Microlux ERG-Connect",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OnNumericTextBoxValidating(object? sender, CancelEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        if (!int.TryParse(textBox.Text, out var value))
        {
            e.Cancel = true;
            AppServices.Log.Warn($"Введено недопустимое числовое значение '{textBox.Text}' в поле {textBox.Name}.");
            MessageBox.Show(
                "Введите целое число.",
                "Microlux ERG-Connect",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        else if (value < 0)
        {
            e.Cancel = true;
            AppServices.Log.Warn($"Введено отрицательное значение {value} в поле {textBox.Name}. Значение должно быть неотрицательным.");
            MessageBox.Show(
                "Значение должно быть неотрицательным.",
                "Microlux ERG-Connect",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OnScanIntervalValidated(object? sender, EventArgs e)
    {
        if (TryParseTextBoxValue(scanIntervalTextBox, out var value))
        {
            _viewModel.DeviceScanIntervalSeconds = value;
        }
    }

    private void OnReconnectDelayValidated(object? sender, EventArgs e)
    {
        if (TryParseTextBoxValue(reconnectDelayTextBox, out var value))
        {
            _viewModel.DeviceReconnectDelaySeconds = value;
        }
    }

    private void OnBackgroundSyncValidated(object? sender, EventArgs e)
    {
        if (TryParseTextBoxValue(backgroundSyncTextBox, out var value))
        {
            _viewModel.BackgroundSyncIntervalMinutes = value;
        }
    }

    private void OnUpdateIntervalValidated(object? sender, EventArgs e)
    {
        if (TryParseTextBoxValue(updateIntervalTextBox, out var value))
        {
            _viewModel.UpdateCheckIntervalMinutes = value;
        }
    }

    private void OnManifestUrlValidated(object? sender, EventArgs e)
    {
        var url = manifestUrlTextBox.Text.Trim();
        if (!string.Equals(url, _viewModel.UpdateManifestUrl, StringComparison.Ordinal))
        {
            AppServices.Log.Info($"Пользователь изменил URL манифеста обновлений: '{url}'.");
            _viewModel.UpdateManifestUrl = url;
        }
        manifestUrlTextBox.Text = _viewModel.UpdateManifestUrl;
    }

    private static bool TryParseTextBoxValue(TextBox textBox, out int value)
    {
        if (!int.TryParse(textBox.Text, out value))
        {
            AppServices.Log.Warn($"Не удалось преобразовать значение '{textBox.Text}' из поля {textBox.Name} к целому числу.");
            return false;
        }
        return true;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        AppServices.Log.Info("Главное окно отображено пользователю.");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        _viewModel.Logs.CollectionChanged -= LogsOnCollectionChanged;
        _viewModel.CheckUpdatesCommand.CanExecuteChanged -= _checkUpdatesCanExecuteHandler;
        _viewModel.InstallUpdateCommand.CanExecuteChanged -= _installUpdateCanExecuteHandler;
        trayIcon.Visible = false;
        base.OnFormClosed(e);
    }
}
