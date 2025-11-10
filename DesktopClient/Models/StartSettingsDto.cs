using System;
using System.Globalization;
using System.Text.Json;

namespace MicroluxErgConnect.Models;

internal sealed class StartSettingsDto
{
    public int? DeviceScanIntervalSeconds { get; set; }
    public int? DeviceReconnectDelaySeconds { get; set; }
    public int? BackgroundSyncIntervalMinutes { get; set; }
    public JsonElement EnableLogs { get; set; }
    public JsonElement SaveBinFiles { get; set; }

    public bool TryGetEnableLogs(out bool value) => TryParseBoolean(EnableLogs, out value);
    public bool TryGetSaveBinFiles(out bool value) => TryParseBoolean(SaveBinFiles, out value);

    private static bool TryParseBoolean(JsonElement element, out bool value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.True:
                value = true;
                return true;
            case JsonValueKind.False:
                value = false;
                return true;
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var numeric))
                {
                    value = numeric != 0;
                    return true;
                }
                break;
            case JsonValueKind.String:
                var text = element.GetString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    break;
                }

                if (bool.TryParse(text, out var boolValue))
                {
                    value = boolValue;
                    return true;
                }

                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericString))
                {
                    value = numericString != 0;
                    return true;
                }
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                break;
        }

        value = default;
        return false;
    }
}
