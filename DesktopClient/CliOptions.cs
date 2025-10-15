using System;

namespace MicroluxErgConnect;

public sealed class SerialCommunicationOptions
{
    public int BaudRate { get; set; } = 115200;
    public bool DtrEnable { get; set; } = true;
    public bool RtsEnable { get; set; } = true;
    public bool ToggleLinesOnOpen { get; set; } = true;
    public int QuietTimeMs { get; set; } = 120;
    public int MaxReadWindowMs { get; set; } = 1500;
    public int ReadTimeoutMs { get; set; } = 400;
    public int WriteTimeoutMs { get; set; } = 400;
    public int RetryCount { get; set; } = 5;
    public int AttemptDelayMs { get; set; } = 150;
    public int MinCommonInfoSize { get; set; } = 136;
    public int MinPatientBlockSize { get; set; } = 64;
    public bool EnableRtcSynchronization { get; set; } = true;
    public bool RequestPatientData { get; set; } = true;
    public bool EnableZipPackaging { get; set; } = false;

    public SerialCommunicationOptions Clone() => (SerialCommunicationOptions)MemberwiseClone();

    public static SerialCommunicationOptions CreateDefault() => new();

    public TimeSpan QuietTime => TimeSpan.FromMilliseconds(Math.Max(QuietTimeMs, 20));
    public TimeSpan MaxReadWindow => TimeSpan.FromMilliseconds(Math.Max(MaxReadWindowMs, QuietTimeMs));
}
