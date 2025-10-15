using System;

namespace MicroluxErgConnect.Models;

public record DeviceConnectionInfo(
    string PortName,
    CommonInfo DeviceInfo,
    DateTime DetectedAt,
    byte[] RawCommonInfo);

public record DeviceStatus(
    bool IsConnected,
    string? CurrentPort,
    CommonInfo? DeviceInfo,
    DateTime LastUpdated,
    string Message);
