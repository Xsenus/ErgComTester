namespace ErgComTester;

internal enum RunMode { Auto, List, Single }

internal sealed class CliOptions
{

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();
        foreach (var a in args)
        {
            var kv = a.Split('=', 2, System.StringSplitOptions.RemoveEmptyEntries);
            string key = kv[0].Trim().ToLowerInvariant();
            string? val = kv.Length > 1 ? kv[1].Trim() : null;
            switch (key)
            {
                case "--list": o.Mode = RunMode.List; break;
                case "--single": o.Mode = RunMode.Single; break;
                case "--port": o.PortName = val; break;
                case "--baud": if (int.TryParse(val, out var b)) o.BaudRate = b; break;
                case "--retries": if (int.TryParse(val, out var r)) o.Retries = r; break;
                case "--rtctime": o.Rtc = true; break;
                case "--nofetch": o.NoFetch = true; break;
                case "--nozip": o.NoZip = true; break;
                case "--nodtr": o.DtrEnable = false; break;
                case "--norts": o.RtsEnable = false; break;
                case "--notoggle": o.ToggleLinesOnOpen = false; break;
                case "--quiet": if (int.TryParse(val, out var q)) o.QuietTimeMs = q; break;
                case "--readwin": if (int.TryParse(val, out var w)) o.MaxReadWindowMs = w; break;
                case "--min-ci": if (int.TryParse(val, out var ci)) o.MinCommonInfoSize = ci; break;
                case "--min-pb": if (int.TryParse(val, out var pb)) o.MinPatientBlockSize = pb; break;
            }
        }
        return o;
    }

    public int AttemptDelayMs { get; private set; } = 150;
    public int BaudRate { get; private set; } = 115200;
    public bool DtrEnable { get; private set; } = true;
    public int MaxReadWindowMs { get; private set; } = 1500;
    public int MinCommonInfoSize { get; private set; } = 136; // 64+64+6+1 + 1(KS)
    public int MinPatientBlockSize { get; private set; } = 64;
    public RunMode Mode { get; private set; } = RunMode.Auto;
    public bool NoFetch { get; private set; } = false;
    public bool NoZip { get; private set; } = false;
    public string? PortName { get; private set; }
    public int QuietTimeMs { get; private set; } = 120;
    public int ReadTimeoutMs { get; private set; } = 400;
    public int Retries { get; private set; } = 5;
    public bool Rtc { get; private set; } = false;
    public bool RtsEnable { get; private set; } = true;
    public bool ToggleLinesOnOpen { get; private set; } = true;
    public int WriteTimeoutMs { get; private set; } = 400;
}
