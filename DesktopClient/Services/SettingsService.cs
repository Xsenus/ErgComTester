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
        if (File.Exists(SettingsPath))
        {
            await using var fs = File.OpenRead(SettingsPath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(fs, JsonOptions);
            if (loaded != null) _settings = loaded;
        }
        else
        {
            _settings = new AppSettings();
            await SaveAsync();
        }

        _settings.Telegram ??= new TelegramSettings();

        Directory.CreateDirectory(_settings.LogsDirectory);
        Directory.CreateDirectory(_settings.ReportsDirectory);
    }

    public async Task SaveAsync()
    {
        await _mutex.WaitAsync();
        try
        {
            Directory.CreateDirectory(BaseDirectory);
            await using var fs = File.Open(SettingsPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(fs, _settings, JsonOptions);
        }
        finally
        {
            _mutex.Release();
        }
        SettingsChanged?.Invoke(this, _settings);
    }

    public async Task UpdateAsync(Action<AppSettings> update)
    {
        await _mutex.WaitAsync();
        try
        {
            update(_settings);
        }
        finally
        {
            _mutex.Release();
        }
        await SaveAsync();
    }
}
