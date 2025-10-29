using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MicroluxErgConnect.Models;
using MicroluxErgConnect;
using MicroluxErgConnect.Infrastructure;

namespace MicroluxErgConnect.Services;

public sealed class UpdateService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly ILog _log;
    private readonly HttpClient _httpClient = new();
    private readonly object _stateLock = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private UpdateState _state = new(false, null, null, "Проверка обновлений не выполнялась");

    public event EventHandler<UpdateState>? StateChanged;

    private bool IsAutoUpdaterMode
    {
        get
        {
            var url = _settings.Current.UpdateManifestUrl;
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && string.Equals(uri.Scheme, Uri.UriSchemeFtp, StringComparison.OrdinalIgnoreCase);
        }
    }

    public UpdateService(SettingsService settings, ILog log)
    {
        _settings = settings;
        _log = log;
    }

    public UpdateState CurrentState
    {
        get { lock (_stateLock) return _state; }
    }

    public void Start()
    {
        if (IsAutoUpdaterMode)
        {
            _log.Info("Сервис обновлений переведен в режим AutoUpdater.NET. Фоновые проверки встроенного механизма отключены.");
            UpdateStateInternal(new UpdateState(false, CurrentVersion, null, "Обновления управляются AutoUpdater.NET."));
            return;
        }

        if (_loopTask != null)
        {
            _log.Debug("Сервис обновлений уже запущен.");
            return;
        }
        _log.Info("Запуск фонового сервиса обновлений.");
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => LoopAsync(_cts.Token));
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CheckForUpdatesAsync(false, ct);
                await Task.Delay(_settings.Current.UpdateCheckInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.Warn($"Ошибка автоматической проверки обновлений: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }
        }
    }

    public async Task<UpdateState> CheckForUpdatesAsync(bool forceDownload, CancellationToken ct = default)
    {
        if (IsAutoUpdaterMode)
        {
            if (!forceDownload)
            {
                const string scheduledMessage = "Плановая проверка отключена: используется AutoUpdater.NET.";
                _log.Info($"AutoUpdater.NET: {scheduledMessage}");
                var latest = CurrentState.LatestVersion ?? CurrentVersion;
                return UpdateStateInternal(new UpdateState(CurrentState.UpdateAvailable, latest, CurrentState.DownloadedFile, scheduledMessage));
            }

            try
            {
                _log.Info("AutoUpdater.NET: запуск проверки по запросу пользователя.");
                var info = await AppServices.RunAutoUpdaterAsync();
                if (!info.Enabled)
                {
                    var message = info.Error ?? "AutoUpdater.NET: режим отключен.";
                    return UpdateStateInternal(new UpdateState(false, CurrentState.LatestVersion ?? CurrentVersion, CurrentState.DownloadedFile, message));
                }

                var latest = info.Manifest?.Version ?? CurrentState.LatestVersion ?? CurrentVersion;
                var updateAvailable = latest > CurrentVersion;
                string status;
                if (!string.IsNullOrWhiteSpace(info.Error))
                {
                    status = $"AutoUpdater.NET: ошибка проверки: {info.Error}";
                }
                else if (info.ExitRequested)
                {
                    status = "AutoUpdater.NET: обновление будет установлено после закрытия приложения.";
                }
                else if (updateAvailable)
                {
                    status = $"AutoUpdater.NET: доступна версия {latest}.";
                }
                else
                {
                    status = "AutoUpdater.NET: обновления не найдены.";
                }

                return UpdateStateInternal(new UpdateState(updateAvailable, latest, CurrentState.DownloadedFile, status));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Warn($"AutoUpdater.NET: ошибка ручной проверки: {ex.Message}");
                return UpdateStateInternal(new UpdateState(CurrentState.UpdateAvailable, CurrentState.LatestVersion, CurrentState.DownloadedFile, "AutoUpdater.NET: ошибка проверки обновлений"));
            }
        }

        try
        {
            _log.Info(forceDownload ? "Ручная проверка обновлений." : "Плановая проверка обновлений.");
            var manifest = await DownloadManifestAsync(ct);
            if (manifest == null)
            {
                return UpdateStateInternal(new UpdateState(false, null, CurrentState.DownloadedFile, "Не удалось загрузить манифест обновлений"));
            }

            if (manifest.Version <= CurrentVersion)
            {
                _log.Info("Установлена актуальная версия приложения.");
                return UpdateStateInternal(new UpdateState(false, manifest.Version, CurrentState.DownloadedFile, "Обновления не требуются"));
            }

            _log.Info($"Доступна новая версия {manifest.Version}.");
            string? downloadedFile = CurrentState.DownloadedFile;
            if (_settings.Current.AutoDownloadUpdates || forceDownload)
            {
                _log.Info("Запущена загрузка обновления.");
                downloadedFile = await DownloadPackageAsync(manifest, ct);
                if (downloadedFile != null)
                {
                    _log.Info($"Обновление скачано: {downloadedFile}");
                }
            }

            return UpdateStateInternal(new UpdateState(true, manifest.Version, downloadedFile, downloadedFile != null ? "Обновление готово к установке" : "Обновление доступно"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.Warn($"Ошибка проверки обновлений: {ex.Message}");
            return UpdateStateInternal(new UpdateState(CurrentState.UpdateAvailable, CurrentState.LatestVersion, CurrentState.DownloadedFile, "Ошибка проверки обновлений"));
        }
    }

    public void ApplyUpdate()
    {
        if (IsAutoUpdaterMode)
        {
            _log.Info("AutoUpdater.NET управляет установкой обновлений. Ручной запуск встроенного механизма недоступен.");
            return;
        }

        var file = CurrentState.DownloadedFile;
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            _log.Warn("Нет загруженного обновления для установки.");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo(file)
            {
                UseShellExecute = true
            };
            var process = Process.Start(psi);
            if (process == null)
            {
                _log.Warn("Установщик обновления не был запущен (Process.Start вернул null). Продолжаем работу приложения.");
                return;
            }

            _log.Info("Запущен установщик обновления.");
            AppServices.RequestApplicationExit("Запущен установщик обновления, требуется закрыть приложение.");
        }
        catch (Exception ex)
        {
            _log.Error($"Не удалось запустить обновление: {ex.Message}");
        }
    }

    private async Task<UpdateManifest?> DownloadManifestAsync(CancellationToken ct)
    {
        var url = _settings.Current.UpdateManifestUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            _log.Warn("URL манифеста обновлений не задан.");
            return null;
        }

        _log.Info($"Загрузка манифеста обновлений: {url}");
        using var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            _log.Warn($"Не удалось получить манифест: {response.StatusCode}");
            return null;
        }
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!json.RootElement.TryGetProperty("version", out var versionElement) || !Version.TryParse(versionElement.GetString(), out var version))
        {
            _log.Warn("Манифест не содержит корректного номера версии.");
            return null;
        }
        string downloadUrl = json.RootElement.GetProperty("downloadUrl").GetString() ?? string.Empty;
        string? notes = json.RootElement.TryGetProperty("releaseNotes", out var rn) ? rn.GetString() : null;
        DateTime? released = null;
        if (json.RootElement.TryGetProperty("releasedAt", out var ra) && DateTime.TryParse(ra.GetString(), out var dt))
        {
            released = dt;
        }
        _log.Info($"Манифест получен: версия={version}, опубликовано={(released.HasValue ? released.Value.ToString("u") : "n/a")}." );
        return new UpdateManifest(version, downloadUrl, notes, released);
    }

    private async Task<string?> DownloadPackageAsync(UpdateManifest manifest, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(manifest.DownloadUrl))
        {
            _log.Warn("Манифест не содержит ссылки на загрузку.");
            return null;
        }

        var updatesDir = Path.Combine(_settings.BaseDirectory, "updates");
        Directory.CreateDirectory(updatesDir);
        var targetPath = Path.Combine(updatesDir, $"MicroluxErgConnect_{manifest.Version}.installer");
        _log.Debug($"Скачивание обновления в {targetPath}.");

        using var response = await _httpClient.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            _log.Warn($"Не удалось загрузить обновление: {response.StatusCode}");
            return null;
        }

        await using var remoteStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await remoteStream.CopyToAsync(fileStream, ct);
        return targetPath;
    }

    private UpdateState UpdateStateInternal(UpdateState state)
    {
        lock (_stateLock)
        {
            _state = state;
        }
        StateChanged?.Invoke(this, state);
        return state;
    }

    private static Version CurrentVersion => typeof(UpdateService).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);

    public void Dispose()
    {
        if (IsAutoUpdaterMode)
        {
            _log.Info("Сервис обновлений (AutoUpdater.NET) завершает работу.");
            _httpClient.Dispose();
            return;
        }

        _log.Info("Завершение работы сервиса обновлений.");
        _cts?.Cancel();
        try
        {
            _loopTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException) { }
        _cts?.Dispose();
        _cts = null;
        _loopTask = null;
        _httpClient.Dispose();
    }
}
