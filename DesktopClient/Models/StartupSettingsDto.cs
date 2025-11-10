using System.Text.Json.Serialization;
using MicroluxErgConnect.Utils;

namespace MicroluxErgConnect.Models;

public sealed class StartupSettingsDto
{
    public int? DeviceScanIntervalSeconds { get; set; }
    public int? DeviceReconnectDelaySeconds { get; set; }
    public int? BackgroundSyncIntervalMinutes { get; set; }
    public int? BackgroundSyncIntervalSeconds { get; set; }

    [JsonConverter(typeof(FlexibleBoolJsonConverter))]
    public bool? WriteLogsToFile { get; set; }

    [JsonConverter(typeof(FlexibleBoolJsonConverter))]
    public bool? EnableLogs { get; set; }

    [JsonConverter(typeof(FlexibleBoolJsonConverter))]
    public bool? KeepRawPatientFiles { get; set; }

    [JsonConverter(typeof(FlexibleBoolJsonConverter))]
    public bool? KeepBinFiles { get; set; }
}
