using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ErgData;
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
    private readonly SynchronizationContext _syncContext;

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
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        if (SynchronizationContext.Current == null)
        {
            _log.Warn("UI-синхронизация недоступна в текущем контексте. Используется резервный SynchronizationContext.");
        }

        CheckUpdatesCommand = new RelayCommand(async () => await _update.CheckForUpdatesAsync(true));
        InstallUpdateCommand = new RelayCommand(() => _update.ApplyUpdate(), () => !string.IsNullOrWhiteSpace(_update.CurrentState.DownloadedFile));
        OpenReportsCommand = new RelayCommand(OpenReportsFolder);
        OpenLogsCommand = new RelayCommand(OpenLogsFolder);
        ForceRescanCommand = new RelayCommand(async () => await ForceRescanAsync());

        _monitor.StatusChanged += (_, status) => _syncContext.Post(_ => HandleStatusUpdate(status), null);
        _monitor.DeviceConnected += (_, info) => _syncContext.Post(_ => OnDeviceConnected(info), null);
        _monitor.DeviceDisconnected += (_, __) => _syncContext.Post(_ => OnDeviceDisconnected(), null);
        _reports.SyncStateChanged += (_, state) => _syncContext.Post(_ => SyncStatus = state, null);
        _reports.ReportGenerated += (_, path) => _syncContext.Post(_ => AddLog(new LogEntry(DateTime.Now, "REPORT", path)), null);
        _update.StateChanged += (_, state) => _syncContext.Post(_ =>
        {
            UpdateStatusText = state.StatusMessage;
            InstallUpdateCommand.RaiseCanExecuteChanged();
        }, null);
        _log.LogAdded += (_, entry) => _syncContext.Post(_ => AddLog(entry), null);

        try
        {
            if (File.Exists(_log.SessionLogPath))
            {
                using var stream = new FileStream(_log.SessionLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var lastLines = new Queue<string>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line is null)
                    {
                        continue;
                    }

                    lastLines.Enqueue(line);
                    if (lastLines.Count > 200)
                    {
                        lastLines.Dequeue();
                    }
                }

                foreach (var line in lastLines)
                {
                    AddLog(new LogEntry(DateTime.Now, "HIST", line));
                }
            }
        }
        catch (Exception ex)
        {
            var message = $"Не удалось прочитать лог: {ex.Message}";
            AddLog(new LogEntry(DateTime.Now, "WARN", message));
            _log.Warn($"{message}. Подробности: {ex}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void HandleStatusUpdate(DeviceStatus status)
    {
        UpdateStatus(status);
        _log.Debug($"Статус устройства обновлен: connected={(status.IsConnected ? "да" : "нет")}, порт={status.CurrentPort ?? "<не указан>"}, сообщение='{status.Message}'");
    }

    private void OnDeviceConnected(DeviceConnectionInfo info)
    {
        IsDeviceConnected = true;
        CurrentPort = info.PortName;
        DeviceName = info.DeviceInfo.DeviceName;
        ReportName = info.DeviceInfo.ReportName;
        SoftwareVersion = info.DeviceInfo.SoftwareRev;
        StatusText = $"Устройство обнаружено: {info.DeviceInfo.DeviceName} ({info.PortName})";
        _log.Info($"Устройство подключено: {info.DeviceInfo.DeviceName} ({info.PortName}), ПО={info.DeviceInfo.SoftwareRev ?? "<неизвестно>"}, отчет={info.DeviceInfo.ReportName ?? "<нет данных>"}");
    }

    private void OnDeviceDisconnected()
    {
        IsDeviceConnected = false;
        StatusText = "Устройство отключено";
        _log.Info("Устройство отключено.");
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

    private async Task ForceRescanAsync()
    {
        try
        {
            _log.Info("Запрошен сброс запомненного COM-порта пользователем.");
            await _settings.UpdateAsync(s => s.PreferredPort = null);
            _log.Info("Запомненный COM-порт успешно сброшен. Повторный поиск устройства начнется автоматически.");
        }
        catch (Exception ex)
        {
            _log.Error($"Ошибка при сбросе COM-порта: {ex}");
            AddLog(new LogEntry(DateTime.Now, "ERROR", $"Ошибка при сбросе COM-порта: {ex.Message}"));
        }
    }

    private void OpenReportsFolder() => OpenFolderSafely(_settings.Current.ReportsDirectory, "отчеты");

    private void OpenLogsFolder() => OpenFolderSafely(_settings.Current.LogsDirectory, "логи");

    private void OpenFolderSafely(string path, string purpose)
    {
        try
        {
            Directory.CreateDirectory(path);
            _log.Info($"Открытие каталога ({purpose}): {path}");
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _log.Error($"Не удалось открыть каталог ({purpose}) '{path}': {ex}");
            AddLog(new LogEntry(DateTime.Now, "ERROR", $"Ошибка открытия каталога {purpose}: {ex.Message}"));
        }
    }

    private void AddLog(LogEntry entry)
    {
        Logs.Add(entry);
        while (Logs.Count > 1000)
        {
            Logs.RemoveAt(0);
        }
    }

    public Task<ManualConversionResult> ConvertRawFileAsync(string filePath)
        => _reports.ConvertPatientFileAsync(filePath);

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
        set => UpdateIntSetting(nameof(DeviceScanIntervalSeconds), value, 2, 60, s => s.DeviceScanIntervalSeconds, (s, v) => s.DeviceScanIntervalSeconds = v);
    }

    public int UpdateCheckIntervalMinutes
    {
        get => _settings.Current.UpdateCheckIntervalMinutes;
        set => UpdateIntSetting(nameof(UpdateCheckIntervalMinutes), value, 5, 24 * 60, s => s.UpdateCheckIntervalMinutes, (s, v) => s.UpdateCheckIntervalMinutes = v);
    }

    public int BackgroundSyncIntervalMinutes
    {
        get => _settings.Current.BackgroundSyncIntervalMinutes;
        set => UpdateIntSetting(nameof(BackgroundSyncIntervalMinutes), value, 5, 24 * 60, s => s.BackgroundSyncIntervalMinutes, (s, v) => s.BackgroundSyncIntervalMinutes = v);
    }

    public int DeviceReconnectDelaySeconds
    {
        get => _settings.Current.DeviceReconnectDelaySeconds;
        set => UpdateIntSetting(nameof(DeviceReconnectDelaySeconds), value, 5, 300, s => s.DeviceReconnectDelaySeconds, (s, v) => s.DeviceReconnectDelaySeconds = v);
    }

    public string UpdateManifestUrl
    {
        get => _settings.Current.UpdateManifestUrl;
        set => UpdateManifestSetting(value);
    }

    public ReportTemplate ReportTemplate
    {
        get => _settings.Current.ReportTemplate;
        set
        {
            if (!Enum.IsDefined(typeof(ReportTemplate), value))
            {
                _log.Warn($"Попытка установить неизвестный шаблон отчета: {value}.");
                return;
            }

            if (value == _settings.Current.ReportTemplate)
            {
                _log.Debug($"Шаблон отчета не изменился: {value}.");
                return;
            }

            _ = ApplySettingAsync(nameof(ReportTemplate), value, (s, v) => s.ReportTemplate = v);
        }
    }

    public ReportRenderingMode ReportRenderingMode
    {
        get => _settings.Current.ReportRenderingMode;
        set
        {
            if (!Enum.IsDefined(typeof(ReportRenderingMode), value))
            {
                _log.Warn($"Попытка установить неизвестный режим генерации отчетов: {value}.");
                return;
            }

            if (value == _settings.Current.ReportRenderingMode)
            {
                _log.Debug($"Режим генерации отчетов не изменился: {value}.");
                return;
            }

            _reports.ApplyRenderingMode(value);
            _ = ApplySettingAsync(nameof(ReportRenderingMode), value, (s, v) => s.ReportRenderingMode = v);
        }
    }

    public string ReportHeader
    {
        get => _settings.Current.ReportHeader ?? string.Empty;
        set => UpdateReportHeader(value);
    }

    private void UpdateIntSetting(string propertyName, int value, int min, int max, Func<AppSettings, int> accessor, Action<AppSettings, int> setter)
    {
        var normalized = Math.Clamp(value, min, max);
        if (normalized != value)
        {
            _log.Warn($"Значение {propertyName} скорректировано с {value} до {normalized} (допустимый диапазон {min}-{max}).");
        }

        if (normalized == accessor(_settings.Current))
        {
            _log.Debug($"Параметр {propertyName} не изменился (значение {normalized}).");
            return;
        }

        _ = ApplySettingAsync(propertyName, normalized, setter);
    }

    private void UpdateManifestSetting(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
        {
            _log.Warn("Попытка установить пустой URL манифеста обновлений проигнорирована.");
            return;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Scheme) || string.IsNullOrEmpty(uri.Host))
        {
            _log.Warn($"Недопустимый URL манифеста обновлений: '{trimmed}'.");
            return;
        }

        var normalized = uri.ToString();
        if (string.Equals(normalized, _settings.Current.UpdateManifestUrl, StringComparison.Ordinal))
        {
            _log.Debug($"URL манифеста обновлений не изменился: {normalized}");
            return;
        }

        _ = ApplySettingAsync(nameof(UpdateManifestUrl), normalized, (s, v) => s.UpdateManifestUrl = v);
    }

    private void UpdateReportHeader(string? value)
    {
        var normalized = NormalizeReportHeader(value);
        if (string.Equals(normalized, _settings.Current.ReportHeader, StringComparison.Ordinal))
        {
            _log.Debug("Шапка отчета не изменилась.");
            return;
        }

        _ = ApplySettingAsync(nameof(ReportHeader), normalized, (s, v) => s.ReportHeader = v);
    }

    private static string NormalizeReportHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        var builder = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            builder.Add(line.TrimEnd());
        }

        while (builder.Count > 0 && string.IsNullOrWhiteSpace(builder[^1]))
        {
            builder.RemoveAt(builder.Count - 1);
        }

        return string.Join("\n", builder);
    }

    private async Task ApplySettingAsync<T>(string propertyName, T value, Action<AppSettings, T> setter)
    {
        try
        {
            await _settings.UpdateAsync(settings => setter(settings, value));
            _log.Info($"Параметр {propertyName} обновлён: {value}.");
            OnPropertyChanged(propertyName);
        }
        catch (Exception ex)
        {
            _log.Error($"Ошибка при обновлении параметра {propertyName}: {ex}");
            AddLog(new LogEntry(DateTime.Now, "ERROR", $"Не удалось обновить параметр {propertyName}: {ex.Message}"));
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
