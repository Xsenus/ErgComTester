using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using MicroluxErgConnect.Infrastructure;
using MicroluxErgConnect.Models;
using MicroluxErgConnect.Services;

namespace MicroluxErgConnect.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;
    private readonly DeviceMonitorService _monitor;
    private readonly UpdateService _update;
    private readonly ReportGenerationService _reports;
    private readonly LogService _log;
    private readonly Dispatcher _dispatcher;

    private string _statusText = "Готово";
    private string _syncStatus = string.Empty;
    private string _updateStatus = "Обновление не проверялось";
    private string? _currentPort;
    private string? _deviceName;
    private string? _reportName;
    private string? _softwareVersion;
    private bool _isDeviceConnected;

    public ObservableCollection<LogEntry> Logs { get; } = new();

    public RelayCommand CheckUpdatesCommand { get; }
    public RelayCommand InstallUpdateCommand { get; }
    public RelayCommand OpenReportsCommand { get; }
    public RelayCommand OpenLogsCommand { get; }
    public RelayCommand ForceRescanCommand { get; }

    public MainViewModel(SettingsService settings, DeviceMonitorService monitor, UpdateService update, ReportGenerationService reports, LogService log)
    {
        _settings = settings;
        _monitor = monitor;
        _update = update;
        _reports = reports;
        _log = log;
        _dispatcher = Dispatcher.CurrentDispatcher;

        CheckUpdatesCommand = new RelayCommand(async () => await _update.CheckForUpdatesAsync(true));
        InstallUpdateCommand = new RelayCommand(() => _update.ApplyUpdate(), () => !string.IsNullOrWhiteSpace(_update.CurrentState.DownloadedFile));
        OpenReportsCommand = new RelayCommand(OpenReportsFolder);
        OpenLogsCommand = new RelayCommand(OpenLogsFolder);
        ForceRescanCommand = new RelayCommand(ForceRescan);

        _monitor.StatusChanged += (_, status) => _dispatcher.Invoke(() => UpdateStatus(status));
        _monitor.DeviceConnected += (_, info) => _dispatcher.Invoke(() => OnDeviceConnected(info));
        _monitor.DeviceDisconnected += (_, __) => _dispatcher.Invoke(OnDeviceDisconnected);
        _reports.SyncStateChanged += (_, state) => _dispatcher.Invoke(() => SyncStatus = state);
        _reports.ReportGenerated += (_, path) => _dispatcher.Invoke(() => AddLog(new LogEntry(DateTime.Now, "REPORT", path)));
        _update.StateChanged += (_, state) => _dispatcher.Invoke(() =>
        {
            UpdateStatusText = state.StatusMessage;
            InstallUpdateCommand.RaiseCanExecuteChanged();
        });
        _log.LogAdded += (_, entry) => _dispatcher.Invoke(() => AddLog(entry));

        try
        {
            if (File.Exists(_log.SessionLogPath))
            {
                foreach (var line in File.ReadLines(_log.SessionLogPath).Take(200))
                {
                    AddLog(new LogEntry(DateTime.Now, "HIST", line));
                }
            }
        }
        catch (Exception ex)
        {
            AddLog(new LogEntry(DateTime.Now, "WARN", $"Не удалось прочитать лог: {ex.Message}"));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnDeviceConnected(DeviceConnectionInfo info)
    {
        IsDeviceConnected = true;
        CurrentPort = info.PortName;
        DeviceName = info.DeviceInfo.DeviceName;
        ReportName = info.DeviceInfo.ReportName;
        SoftwareVersion = info.DeviceInfo.SoftwareRev;
        StatusText = $"Устройство обнаружено: {info.DeviceInfo.DeviceName} ({info.PortName})";
    }

    private void OnDeviceDisconnected()
    {
        IsDeviceConnected = false;
        StatusText = "Устройство отключено";
    }

    private void UpdateStatus(DeviceStatus status)
    {
        StatusText = status.Message;
        CurrentPort = status.CurrentPort;
        DeviceName = status.DeviceInfo?.DeviceName;
        ReportName = status.DeviceInfo?.ReportName;
        SoftwareVersion = status.DeviceInfo?.SoftwareRev;
        IsDeviceConnected = status.IsConnected;
    }

    private void ForceRescan()
    {
        _ = _settings.UpdateAsync(s => s.PreferredPort = null);
        _log.Info("Сброшен запомненный COM-порт. Повторный поиск устройства начнется автоматически.");
    }

    private void OpenReportsFolder()
    {
        Directory.CreateDirectory(_settings.Current.ReportsDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _settings.Current.ReportsDirectory,
            UseShellExecute = true
        });
    }

    private void OpenLogsFolder()
    {
        Directory.CreateDirectory(_settings.Current.LogsDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _settings.Current.LogsDirectory,
            UseShellExecute = true
        });
    }

    private void AddLog(LogEntry entry)
    {
        Logs.Add(entry);
        while (Logs.Count > 1000)
        {
            Logs.RemoveAt(0);
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public string SyncStatus
    {
        get => _syncStatus;
        set => SetField(ref _syncStatus, value);
    }

    public string UpdateStatusText
    {
        get => _updateStatus;
        set => SetField(ref _updateStatus, value);
    }

    public string? CurrentPort
    {
        get => _currentPort;
        set => SetField(ref _currentPort, value);
    }

    public string? DeviceName
    {
        get => _deviceName;
        set => SetField(ref _deviceName, value);
    }

    public string? ReportName
    {
        get => _reportName;
        set => SetField(ref _reportName, value);
    }

    public string? SoftwareVersion
    {
        get => _softwareVersion;
        set => SetField(ref _softwareVersion, value);
    }

    public bool IsDeviceConnected
    {
        get => _isDeviceConnected;
        set => SetField(ref _isDeviceConnected, value);
    }

    public int DeviceScanIntervalSeconds
    {
        get => _settings.Current.DeviceScanIntervalSeconds;
        set
        {
            if (value != _settings.Current.DeviceScanIntervalSeconds)
            {
                _ = _settings.UpdateAsync(s => s.DeviceScanIntervalSeconds = value);
                OnPropertyChanged();
            }
        }
    }

    public int UpdateCheckIntervalMinutes
    {
        get => _settings.Current.UpdateCheckIntervalMinutes;
        set
        {
            if (value != _settings.Current.UpdateCheckIntervalMinutes)
            {
                _ = _settings.UpdateAsync(s => s.UpdateCheckIntervalMinutes = value);
                OnPropertyChanged();
            }
        }
    }

    public int BackgroundSyncIntervalMinutes
    {
        get => _settings.Current.BackgroundSyncIntervalMinutes;
        set
        {
            if (value != _settings.Current.BackgroundSyncIntervalMinutes)
            {
                _ = _settings.UpdateAsync(s => s.BackgroundSyncIntervalMinutes = value);
                OnPropertyChanged();
            }
        }
    }

    public int DeviceReconnectDelaySeconds
    {
        get => _settings.Current.DeviceReconnectDelaySeconds;
        set
        {
            if (value != _settings.Current.DeviceReconnectDelaySeconds)
            {
                _ = _settings.UpdateAsync(s => s.DeviceReconnectDelaySeconds = value);
                OnPropertyChanged();
            }
        }
    }

    public string UpdateManifestUrl
    {
        get => _settings.Current.UpdateManifestUrl;
        set
        {
            if (!string.Equals(value, _settings.Current.UpdateManifestUrl, StringComparison.Ordinal))
            {
                _ = _settings.UpdateAsync(s => s.UpdateManifestUrl = value);
                OnPropertyChanged();
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
