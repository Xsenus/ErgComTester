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

    private static string GetDefaultPdfDirectory()
        => Path.Combine(AppContext.BaseDirectory, "out");

    public async Task LoadAsync()
    {
        Directory.CreateDirectory(BaseDirectory);

        var saveRequired = false;
        var settingsLoadedFromFile = false;

        if (File.Exists(SettingsPath))
        {
            await using var fs = File.OpenRead(SettingsPath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(fs, JsonOptions).ConfigureAwait(false);
            if (loaded != null)
            {
                _settings = loaded;
                settingsLoadedFromFile = true;
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

        if (string.IsNullOrWhiteSpace(_settings.PdfReportsDirectory))
        {
            _settings.PdfReportsDirectory = settingsLoadedFromFile
                ? _settings.ReportsDirectory
                : GetDefaultPdfDirectory();
            saveRequired = true;
        }
        else
        {
            try
            {
                var normalized = Path.GetFullPath(_settings.PdfReportsDirectory);
                if (!string.Equals(normalized, _settings.PdfReportsDirectory, StringComparison.Ordinal))
                {
                    _settings.PdfReportsDirectory = normalized;
                    saveRequired = true;
                }
            }
            catch
            {
                _settings.PdfReportsDirectory = GetDefaultPdfDirectory();
                saveRequired = true;
            }
        }

        var workingDirectory = Path.Combine(BaseDirectory, "Sessions");
        if (!string.Equals(_settings.ReportsDirectory, workingDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _settings.ReportsDirectory = workingDirectory;
            saveRequired = true;
        }

        Directory.CreateDirectory(_settings.LogsDirectory);
        Directory.CreateDirectory(_settings.ReportsDirectory);
        Directory.CreateDirectory(_settings.PdfReportsDirectory);

        if (saveRequired)
        {
            await SaveAsync().ConfigureAwait(false);
        }
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        // òàéì-àóò íà âñÿêèé ñëó÷àé, ÷òîáû íèêîãäà íå çàâèñàòü
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        // ïðîáóåì çàõâàòèòü ñåìàôîð ñ òàéì-àóòîì
        await _mutex.WaitAsync(cts.Token).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(BaseDirectory);

            // ðåàëüíûé àñèíõðîííûé ôàéëîâûé ïîòîê, ÷òîáû íå áëîêèðîâàòü ïóë
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
            catch { /* íå äà¸ì ñîáûòèþ óðîíèòü ñîõðàíåíèå */ }
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
