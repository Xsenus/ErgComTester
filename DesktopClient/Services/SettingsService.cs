using System;
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

        Directory.CreateDirectory(_settings.LogsDirectory);
        Directory.CreateDirectory(_settings.ReportsDirectory);

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

            // реальный асинхронный файловый поток, чтобы не блокировать пул
            await using var fs = new FileStream(
                SettingsPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                options: FileOptions.Asynchronous);

            await JsonSerializer.SerializeAsync(fs, _settings, JsonOptions, cts.Token).ConfigureAwait(false);
            await fs.FlushAsync(cts.Token).ConfigureAwait(false);
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
