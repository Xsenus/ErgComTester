using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync()
    {
        Directory.CreateDirectory(BaseDirectory);

        var saveRequired = false;

        if (File.Exists(SettingsPath))
        {
            await using var fs = File.OpenRead(SettingsPath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(fs, JsonOptions).ConfigureAwait(false);
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
        else
        {
            _settings = new AppSettings();
            saveRequired = true;
        }

        _settings.Telegram ??= TelegramSettings.CreateDefault();

        if (string.IsNullOrWhiteSpace(_settings.Telegram.BotToken)
            && string.IsNullOrWhiteSpace(_settings.Telegram.ChatId))
        {
            _settings.Telegram = TelegramSettings.CreateDefault();
            saveRequired = true;
        }

        if (_settings.ClinicHeader == null)
        {
            _settings.ClinicHeader = string.Empty;
            saveRequired = true;
        }

        if (string.Equals(_settings.UpdateManifestUrl, AppSettings.LegacyManifestUrl, StringComparison.OrdinalIgnoreCase))
        {
            _settings.UpdateManifestUrl = AppSettings.DefaultManifestUrl;
            saveRequired = true;
        }

        Directory.CreateDirectory(_settings.LogsDirectory);
        Directory.CreateDirectory(_settings.ReportsDirectory);

        if (saveRequired)
        {
            await SaveAsync().ConfigureAwait(false);
        }
    }

    public async Task SaveAsync()
    {
        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(BaseDirectory);
            await using var fs = File.Open(SettingsPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(fs, _settings, JsonOptions).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
        SettingsChanged?.Invoke(this, _settings);
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
}
