using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MicroluxErgConnect.Models;
using MicroluxErgConnect.Services;
using MicroluxErgConnect.ViewModels;

namespace MicroluxErgConnect.Infrastructure;

public static class AppServices
{
    private static bool _initialized;

    public static SettingsService Settings { get; private set; } = null!;
    public static LogService Log { get; private set; } = null!;
    public static DeviceMonitorService DeviceMonitor { get; private set; } = null!;
    public static UpdateService Update { get; private set; } = null!;
    public static ReportGenerationService Reports { get; private set; } = null!;
    public static MainViewModel MainViewModel { get; private set; } = null!;

    public static void Initialize()
    {
        if (_initialized) return;

        Settings = new SettingsService();
        Task.Run(() => Settings.LoadAsync()).GetAwaiter().GetResult();

        Log = new LogService(Settings);
        Log.Section("Microlux ERG-Connect Desktop");
        Log.Info($"Версия приложения: {Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0)}");
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

        Reports = new ReportGenerationService(Settings, Log);
        DeviceMonitor = new DeviceMonitorService(Settings, Log, Reports);
        Update = new UpdateService(Settings, Log);
        MainViewModel = new MainViewModel(Settings, DeviceMonitor, Update, Reports, Log);

        DeviceMonitor.Start();
        Update.Start();

        _initialized = true;
    }

    public static void Dispose()
    {
        if (!_initialized) return;
        Update.Dispose();
        DeviceMonitor.Dispose();
        Reports.Dispose();
        Log.Dispose();
        _initialized = false;
    }

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
    }
}
