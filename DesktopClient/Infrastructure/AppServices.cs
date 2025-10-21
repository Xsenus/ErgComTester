using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Net;
using System.Xml.Linq;
using AutoUpdaterDotNET;
using MicroluxErgConnect.Models;
using MicroluxErgConnect.Services;
using MicroluxErgConnect.ViewModels;

namespace MicroluxErgConnect.Infrastructure;

public static class AppServices
{
    private static bool _initialized;
    private static bool _autoUpdaterRequestedExit;

    public static SettingsService Settings { get; private set; } = null!;
    public static LogService Log { get; private set; } = null!;
    public static DeviceMonitorService DeviceMonitor { get; private set; } = null!;
    public static UpdateService Update { get; private set; } = null!;
    public static ReportGenerationService Reports { get; private set; } = null!;
    public static TelegramNotificationService Telegram { get; private set; } = null!;
    public static MainViewModel MainViewModel { get; private set; } = null!;

    public static void Initialize()
    {
        if (_initialized) return;

        Settings = new SettingsService();
        Task.Run(() => Settings.LoadAsync()).GetAwaiter().GetResult();

        Log = new LogService(Settings);
        Log.Section("Microlux ERG-Connect Desktop");
        var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
        Log.Info($"Версия приложения: {version}");
        Log.Info($".NET: {RuntimeInformation.FrameworkDescription}");
        Log.Info($"ОС: {Environment.OSVersion} | {RuntimeInformation.OSDescription}");
        Log.Info($"Пользователь: {Environment.UserDomainName}\\{Environment.UserName} | x64={Environment.Is64BitProcess}");
        Log.Info($"Рабочая директория: {AppContext.BaseDirectory}");
        Log.Info($"Файл настроек: {Settings.SettingsPath} ({(File.Exists(Settings.SettingsPath) ? "существует" : "будет создан")})");
        Log.Info($"Файл журнала: {Log.SessionLogPath}");
        DumpSettings();
        Settings.SettingsChanged += (_, __) =>
        {
            Log.Info("Настройки обновлены и сохранены.");
            DumpSettings();
        };

        Telegram = new TelegramNotificationService(Settings, Log);
        Reports = new ReportGenerationService(Settings, Log, Telegram);
        DeviceMonitor = new DeviceMonitorService(Settings, Log, Reports);
        Update = new UpdateService(Settings, Log);
        MainViewModel = new MainViewModel(Settings, DeviceMonitor, Update, Reports, Log);

        var autoUpdaterInfo = RunAutoUpdaterIfConfigured();
        if (autoUpdaterInfo.Enabled)
        {
            Telegram.NotifyAutoUpdaterSummary(
                autoUpdaterInfo.Manifest?.Version,
                autoUpdaterInfo.Manifest?.PackageUrl,
                autoUpdaterInfo.Manifest?.Mandatory,
                autoUpdaterInfo.Manifest?.MandatoryMode,
                autoUpdaterInfo.Error,
                autoUpdaterInfo.ExitRequested);
        }

        DeviceMonitor.DeviceConnected += (_, info) => Telegram.NotifyDeviceConnected(info);
        DeviceMonitor.DeviceDisconnected += (_, __) => Telegram.NotifyDeviceDisconnected(DeviceMonitor.CurrentStatus.Message);

        DeviceMonitor.Start();
        Update.Start();

        Telegram.NotifyApplicationStarted(version.ToString());

        _initialized = true;
    }

    public static void Dispose()
    {
        if (!_initialized) return;
        Update.Dispose();
        DeviceMonitor.Dispose();
        Reports.Dispose();
        Log.Info("Приложение Microlux ERG-Connect завершается.");
        Telegram.NotifyApplicationStopping(Log.SessionLogPath);
        Telegram.Dispose();
        Log.Dispose();
        _initialized = false;
    }

    private static AutoUpdaterRunInfo RunAutoUpdaterIfConfigured()
    {
        var url = Settings.Current.UpdateManifestUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            Log.Info("AutoUpdater.NET: URL манифеста не задан, проверка пропущена.");
            return new AutoUpdaterRunInfo(false, null, "URL манифеста не задан", false);
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !string.Equals(uri.Scheme, Uri.UriSchemeFtp, StringComparison.OrdinalIgnoreCase))
        {
            Log.Info($"AutoUpdater.NET: URL '{url}' не поддерживается (ожидается ftp), используется встроенный сервис обновлений.");
            return new AutoUpdaterRunInfo(false, null, "URL не поддерживает AutoUpdater.NET", false);
        }

        Log.Info($"AutoUpdater.NET: подготовка проверки обновлений ({url}).");

        var manifest = TryReadAutoUpdaterManifest(url);
        if (manifest != null)
        {
            var package = string.IsNullOrWhiteSpace(manifest.PackageUrl) ? "<не указан>" : manifest.PackageUrl;
            var mandatory = manifest.Mandatory ? $"да (режим {manifest.MandatoryMode ?? "?"})" : "нет";
            Log.Info($"AutoUpdater.NET: манифест версия {manifest.Version}, обязательное обновление: {mandatory}, пакет: {package}.");
        }
        else
        {
            Log.Warn("AutoUpdater.NET: не удалось прочитать манифест перед запуском.");
        }

        try
        {
            ConfigureAutoUpdater();
            AutoUpdater.ApplicationExitEvent += OnAutoUpdaterExitRequested;
            AutoUpdater.Start(url);
            Log.Info("AutoUpdater.NET: проверка завершена.");
            return new AutoUpdaterRunInfo(true, manifest, null, _autoUpdaterRequestedExit);
        }
        catch (Exception ex)
        {
            Log.Warn($"AutoUpdater.NET: ошибка запуска проверки: {ex.Message}");
            return new AutoUpdaterRunInfo(true, manifest, ex.Message, _autoUpdaterRequestedExit);
        }
        finally
        {
            AutoUpdater.ApplicationExitEvent -= OnAutoUpdaterExitRequested;
        }
    }

    private static void ConfigureAutoUpdater()
    {
        _autoUpdaterRequestedExit = false;
        var downloadDirectory = Path.Combine(Settings.BaseDirectory, "AutoUpdater");
        Directory.CreateDirectory(downloadDirectory);
        AutoUpdater.AppTitle = "Microlux ERG-Connect";
        AutoUpdater.Synchronous = true;
        AutoUpdater.ReportErrors = false;
        AutoUpdater.ShowSkipButton = false;
        AutoUpdater.ShowRemindLaterButton = false;
        AutoUpdater.DownloadPath = downloadDirectory;
    }

    private static AutoUpdaterManifestInfo? TryReadAutoUpdaterManifest(string url)
    {
        try
        {
#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;

            using var response = (FtpWebResponse)request.GetResponse();
            using var stream = response.GetResponseStream();
#pragma warning restore SYSLIB0014
            if (stream == null)
            {
                return null;
            }

            var document = XDocument.Load(stream);
            var item = document.Element("item") ?? document.Root;
            if (item == null)
            {
                return null;
            }

            var versionText = item.Element("version")?.Value?.Trim();
            if (!Version.TryParse(versionText, out var version))
            {
                return null;
            }

            var packageUrl = item.Element("url")?.Value?.Trim();
            var mandatoryElement = item.Element("mandatory");
            bool mandatory = false;
            string? mode = null;
            if (mandatoryElement != null)
            {
                bool.TryParse(mandatoryElement.Value, out mandatory);
                mode = mandatoryElement.Attribute("mode")?.Value;
            }

            return new AutoUpdaterManifestInfo(version, packageUrl, mandatory, mode);
        }
        catch (Exception ex)
        {
            Log.Warn($"AutoUpdater.NET: не удалось прочитать манифест '{url}': {ex.Message}");
            return null;
        }
    }

    private static void OnAutoUpdaterExitRequested()
    {
        _autoUpdaterRequestedExit = true;
        Log.Info("AutoUpdater.NET запросил завершение приложения для установки обновления.");
    }

    private sealed record AutoUpdaterRunInfo(bool Enabled, AutoUpdaterManifestInfo? Manifest, string? Error, bool ExitRequested);

    private sealed record AutoUpdaterManifestInfo(Version Version, string? PackageUrl, bool Mandatory, string? MandatoryMode);

    private static void DumpSettings()
    {
        var s = Settings.Current;
        Log.Info($"Настройки устройства: preferredPort={s.PreferredPort ?? "<не задан>"}, scanInterval={s.DeviceScanInterval.TotalSeconds}s, reconnectDelay={s.DeviceReconnectDelay.TotalSeconds}s");
        Log.Info($"Синхронизация пациентов: interval={s.BackgroundSyncInterval.TotalMinutes} мин.");
        Log.Info($"Обновления: interval={s.UpdateCheckInterval.TotalMinutes} мин., авто-загрузка={(s.AutoDownloadUpdates ? "да" : "нет")}, manifest={s.UpdateManifestUrl}");
        Log.Info($"Каталоги: отчеты={s.ReportsDirectory}, логи={s.LogsDirectory}");
        var serial = s.Serial;
        Log.Info($"COM-порт: baud={serial.BaudRate}, readTimeout={serial.ReadTimeoutMs}мс, writeTimeout={serial.WriteTimeoutMs}мс, quiet={serial.QuietTimeMs}мс, window={serial.MaxReadWindowMs}мс");
        Log.Info($"COM-параметры: DTR={(serial.DtrEnable ? "on" : "off")}, RTS={(serial.RtsEnable ? "on" : "off")}, toggle={(serial.ToggleLinesOnOpen ? "on" : "off")}, retries={serial.RetryCount}, minCI={serial.MinCommonInfoSize}, minPatient={serial.MinPatientBlockSize}");
        Log.Info($"Дополнительно: RTC sync={(serial.EnableRtcSynchronization ? "вкл" : "выкл")}, получать пациентов={(serial.RequestPatientData ? "да" : "нет")}, ZIP={(serial.EnableZipPackaging ? "вкл" : "выкл")}");
        Log.Info($"Telegram: {s.Telegram?.DescribeSafety() ?? "<не настроен>"}");
    }
}
