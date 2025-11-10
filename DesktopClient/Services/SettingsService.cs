using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ErgData;
using MicroluxErgConnect.Models;

namespace MicroluxErgConnect.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private AppSettings _settings = new();

    public AppSettings Current => _settings;
    public string BaseDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicroluxErgConnect");
    public string SettingsPath => Path.Combine(BaseDirectory, "settings.json");
    public StartSettingsImportResult StartSettingsImport { get; private set; } = StartSettingsImportResult.NotFound;

    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync()
    {
        Directory.CreateDirectory(BaseDirectory);

        var saveRequired = false;

        if (File.Exists(SettingsPath))
        {
            var fs = File.OpenRead(SettingsPath);
            try
            {
                var loaded = await JsonSerializer
                    .DeserializeAsync<AppSettings>(fs, JsonOptions)
                    .ConfigureAwait(false);
                if (loaded != null)
                {
                    _settings = loaded;
                }
                else
                {
                    _settings = new AppSettings();
                    saveRequired = true;
                }
            }
            finally
            {
                await fs.DisposeAsync().ConfigureAwait(false);
            }
        }
        else
        {
            _settings = new AppSettings();
            saveRequired = true;
        }

        _settings.Telegram ??= TelegramSettings.CreateDefault();

        var startSettingsImport = TryImportStartSettings();
        StartSettingsImport = startSettingsImport;
        if (startSettingsImport.Applied)
        {
            saveRequired = true;
        }

        var legacyReportsDirectory = Path.Combine(AppContext.BaseDirectory ?? Environment.CurrentDirectory, "out");
        var defaultReportsDirectory = AppSettings.ResolveDefaultReportsDirectory();
        if (string.Equals(_settings.ReportsDirectory, legacyReportsDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _settings.ReportsDirectory = defaultReportsDirectory;
            saveRequired = true;
        }

        if (string.IsNullOrWhiteSpace(_settings.Telegram.BotToken)
            && string.IsNullOrWhiteSpace(_settings.Telegram.ChatId))
        {
            _settings.Telegram = TelegramSettings.CreateDefault();
            saveRequired = true;
        }

        if (string.Equals(_settings.UpdateManifestUrl, AppSettings.LegacyManifestUrl, StringComparison.OrdinalIgnoreCase))
        {
            _settings.UpdateManifestUrl = AppSettings.DefaultManifestUrl;
            saveRequired = true;
        }

        _settings.ReportHeader ??= string.Empty;
        if (!Enum.IsDefined(typeof(ReportRenderingMode), _settings.ReportRenderingMode))
        {
            _settings.ReportRenderingMode = ReportRenderingMode.Automatic;
            saveRequired = true;
        }

        if (_settings.ReportTemplate != ReportTemplate.Client)
        {
            _settings.ReportTemplate = ReportTemplate.Client;
            saveRequired = true;
        }

        if (_settings.ReportRenderingMode != ReportRenderingMode.Legacy)
        {
            _settings.ReportRenderingMode = ReportRenderingMode.Legacy;
            saveRequired = true;
        }

        if (_settings.EnableLogs)
        {
            Directory.CreateDirectory(_settings.LogsDirectory);
        }
        Directory.CreateDirectory(_settings.ReportsDirectory);

        if (_settings.Serial != null)
        {
            if (_settings.Serial.DtrEnable
                || _settings.Serial.RtsEnable
                || _settings.Serial.ToggleLinesOnOpen)
            {
                _settings.Serial.DtrEnable = false;
                _settings.Serial.RtsEnable = false;
                _settings.Serial.ToggleLinesOnOpen = false;
                saveRequired = true;
            }
        }

        if (saveRequired)
        {
            await SaveAsync().ConfigureAwait(false);
        }
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        // тайм-аут на всякий случай, чтобы никогда не зависать
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        // пробуем захватить семафор с тайм-аутом
        await _mutex.WaitAsync(cts.Token).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(BaseDirectory);

            var fs = new FileStream(
                SettingsPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                options: FileOptions.Asynchronous);
            try
            {
                await JsonSerializer.SerializeAsync(fs, _settings, JsonOptions, cts.Token).ConfigureAwait(false);
                await fs.FlushAsync(cts.Token).ConfigureAwait(false);
            }
            finally
            {
                await fs.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _mutex.Release();
        }

        _ = Task.Run(() =>
        {
            try { SettingsChanged?.Invoke(this, _settings); }
            catch { /* не даём событию уронить сохранение */ }
        });
    }

    private StartSettingsImportResult TryImportStartSettings()
    {
        var baseDir = AppContext.BaseDirectory ?? Environment.CurrentDirectory;
        var path = Path.Combine(baseDir, "start_settings.json");
        if (!File.Exists(path))
        {
            return StartSettingsImportResult.NotFound;
        }

        Dictionary<string, JsonElement>? root;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path), options);
        }
        catch (Exception ex)
        {
            return StartSettingsImportResult.Failure(path, ex.Message);
        }

        if (root is null || root.Count == 0)
        {
            return StartSettingsImportResult.Failure(path, "файл не содержит корректных данных");
        }

        var applied = new List<string>();

        bool changed = false;

        if (TryReadInt(root, "DeviceScanIntervalSeconds", out var scanInterval))
        {
            var value = Math.Clamp(scanInterval, 2, 60);
            if (_settings.DeviceScanIntervalSeconds != value)
            {
                _settings.DeviceScanIntervalSeconds = value;
                applied.Add($"DeviceScanIntervalSeconds={value}");
                changed = true;
            }
        }

        if (TryReadInt(root, "DeviceReconnectDelaySeconds", out var reconnectDelay))
        {
            var value = Math.Clamp(reconnectDelay, 5, 300);
            if (_settings.DeviceReconnectDelaySeconds != value)
            {
                _settings.DeviceReconnectDelaySeconds = value;
                applied.Add($"DeviceReconnectDelaySeconds={value}");
                changed = true;
            }
        }

        int? backgroundSyncMinutes = null;
        if (TryReadInt(root, "BackgroundSyncIntervalMinutes", out var syncMinutes))
        {
            backgroundSyncMinutes = syncMinutes;
        }
        else if (TryReadInt(root, "BackgroundSyncIntervalSeconds", out var syncSeconds))
        {
            backgroundSyncMinutes = (int)Math.Round(syncSeconds / 60d);
        }

        if (backgroundSyncMinutes.HasValue)
        {
            var value = Math.Clamp(backgroundSyncMinutes.Value, 5, 24 * 60);
            if (_settings.BackgroundSyncIntervalMinutes != value)
            {
                _settings.BackgroundSyncIntervalMinutes = value;
                applied.Add($"BackgroundSyncIntervalMinutes={value}");
                changed = true;
            }
        }

        if (TryReadBool(root, "EnableLogs", out var enableLogs)
            || TryReadBool(root, "LogsEnabled", out enableLogs))
        {
            if (_settings.EnableLogs != enableLogs)
            {
                _settings.EnableLogs = enableLogs;
                applied.Add($"EnableLogs={(enableLogs ? 1 : 0)}");
                changed = true;
            }
        }

        if (TryReadBool(root, "SaveRawPatientFiles", out var saveRaw)
            || TryReadBool(root, "KeepRawBinFiles", out saveRaw)
            || TryReadBool(root, "EnableRawBin", out saveRaw))
        {
            if (_settings.SaveRawPatientFiles != saveRaw)
            {
                _settings.SaveRawPatientFiles = saveRaw;
                applied.Add($"SaveRawPatientFiles={(saveRaw ? 1 : 0)}");
                changed = true;
            }
        }

        if (!changed)
        {
            return StartSettingsImportResult.NoChanges(path);
        }

        return StartSettingsImportResult.Success(path, applied);
    }

    private static bool TryReadInt(Dictionary<string, JsonElement> root, string key, out int value)
    {
        value = default;
        if (!root.TryGetValue(key, out var element))
        {
            return false;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Number when element.TryGetInt32(out var number):
                value = number;
                return true;
            case JsonValueKind.String when int.TryParse(element.GetString(), out var parsed):
                value = parsed;
                return true;
            default:
                return false;
        }
    }

    private static bool TryReadBool(Dictionary<string, JsonElement> root, string key, out bool value)
    {
        value = default;
        if (!root.TryGetValue(key, out var element))
        {
            return false;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.True:
                value = true;
                return true;
            case JsonValueKind.False:
                value = false;
                return true;
            case JsonValueKind.Number when element.TryGetInt32(out var number):
                value = number != 0;
                return true;
            case JsonValueKind.String:
                var str = element.GetString();
                if (string.Equals(str, "1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(str, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(str, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                if (string.Equals(str, "0", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(str, "false", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(str, "no", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }

                return false;
            default:
                return false;
        }
    }


    public async Task UpdateAsync(Action<AppSettings> update)
    {
        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            update(_settings);
        }
        finally
        {
            _mutex.Release();
        }
        await SaveAsync().ConfigureAwait(false);
    }

    public readonly struct StartSettingsImportResult
    {
        private StartSettingsImportResult(bool attempted, bool applied, string? path, string? error, IReadOnlyList<string>? appliedSettings)
        {
            Attempted = attempted;
            Applied = applied;
            Path = path;
            Error = error;
            AppliedSettings = appliedSettings ?? Array.Empty<string>();
        }

        public bool Attempted { get; }
        public bool Applied { get; }
        public string? Path { get; }
        public string? Error { get; }
        public IReadOnlyList<string> AppliedSettings { get; }

        public static StartSettingsImportResult NotFound => new(false, false, null, null, null);
        public static StartSettingsImportResult NoChanges(string path) => new(true, false, path, null, Array.Empty<string>());
        public static StartSettingsImportResult Success(string path, IReadOnlyList<string> applied)
            => new(true, true, path, null, applied);
        public static StartSettingsImportResult Failure(string path, string error)
            => new(true, false, path, error, Array.Empty<string>());
    }
}
