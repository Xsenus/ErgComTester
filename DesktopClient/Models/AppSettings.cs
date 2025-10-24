using System;
using System.IO;

namespace MicroluxErgConnect.Models;

public class AppSettings
{
    public const string LegacyManifestUrl = "https://updates.microlux.ru/manifest.json";
    public const string DefaultManifestUrl = "ftp://90.189.149.59/other/ErgComTester/ErgComTester.Update.xml";

    public string? PreferredPort { get; set; }
    public int DeviceScanIntervalSeconds { get; set; } = 5;
    public int DeviceReconnectDelaySeconds { get; set; } = 15;
    public int UpdateCheckIntervalMinutes { get; set; } = 60;
    public bool AutoDownloadUpdates { get; set; } = true;
    public string UpdateManifestUrl { get; set; } = DefaultManifestUrl;
    public SerialCommunicationOptions Serial { get; set; } = SerialCommunicationOptions.CreateDefault();
    public int BackgroundSyncIntervalMinutes { get; set; } = 30;
    public string ReportsDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Microlux ERG Connect", "Reports");
    public string LogsDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Microlux ERG Connect", "Logs");
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public TelegramSettings Telegram { get; set; } = TelegramSettings.CreateDefault();

    public TimeSpan DeviceScanInterval => TimeSpan.FromSeconds(Math.Clamp(DeviceScanIntervalSeconds, 2, 60));
    public TimeSpan DeviceReconnectDelay => TimeSpan.FromSeconds(Math.Clamp(DeviceReconnectDelaySeconds, 5, 300));
    public TimeSpan UpdateCheckInterval => TimeSpan.FromMinutes(Math.Clamp(UpdateCheckIntervalMinutes, 5, 24 * 60));
    public TimeSpan BackgroundSyncInterval => TimeSpan.FromMinutes(Math.Clamp(BackgroundSyncIntervalMinutes, 5, 24 * 60));
}
