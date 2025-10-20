using System.Collections.Generic;
using System.IO.Ports;
using MicroluxErgConnect;

namespace ErgComTester;

internal static class ErgIo
{
    private enum ReadStrategyId { Polling, BlockingChunk, BlockingByte }

    private static readonly ReadStrategyId[] _defaultStrategyOrder =
        { ReadStrategyId.Polling, ReadStrategyId.BlockingChunk, ReadStrategyId.BlockingByte };

    private static readonly Dictionary<string, ReadStrategyId> _preferredByPort = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _lock = new();

    public static byte[] ReadChunk(SerialPort sp, ILog log, int minExpectedSize, int quietMs, int maxWindowMs)
    {
        var strategies = BuildStrategyOrder(sp.PortName);
        byte[] bestAttempt = Array.Empty<byte>();

        foreach (var strategy in strategies)
        {
            var data = ExecuteStrategy(strategy, sp, minExpectedSize, quietMs, maxWindowMs);
            if (IsSuccessful(data, minExpectedSize))
            {
                RememberPreferredStrategy(sp.PortName, strategy, log);
                return data;
            }

            if (data.Length > bestAttempt.Length) bestAttempt = data;
        }

        if (bestAttempt.Length == 0)
            log.Debug($"[{sp.PortName}] No data received after trying: {string.Join(", ", strategies)}");

        return bestAttempt;
    }

    private static IEnumerable<ReadStrategyId> BuildStrategyOrder(string portName)
    {
        lock (_lock)
        {
            if (_preferredByPort.TryGetValue(portName, out var preferred))
            {
                foreach (var s in _defaultStrategyOrder)
                {
                    if (s == preferred) yield return s;
                }
                foreach (var s in _defaultStrategyOrder)
                {
                    if (s != preferred) yield return s;
                }
                yield break;
            }
        }

        foreach (var s in _defaultStrategyOrder) yield return s;
    }

    private static void RememberPreferredStrategy(string portName, ReadStrategyId strategy, ILog log)
    {
        lock (_lock)
        {
            if (_preferredByPort.TryGetValue(portName, out var existing) && existing == strategy) return;
            _preferredByPort[portName] = strategy;
        }
        log.Debug($"[{portName}] Selected read strategy: {strategy}");
    }

    private static bool IsSuccessful(byte[] data, int minExpectedSize)
    {
        if (minExpectedSize <= 0) return data.Length > 0;
        return data.Length >= minExpectedSize;
    }

    private static byte[] ExecuteStrategy(ReadStrategyId strategy, SerialPort sp, int minExpectedSize, int quietMs, int maxWindowMs)
        => strategy switch
        {
            ReadStrategyId.Polling => ReadPolling(sp, minExpectedSize, quietMs, maxWindowMs),
            ReadStrategyId.BlockingChunk => ReadBlockingChunk(sp, minExpectedSize, quietMs, maxWindowMs),
            ReadStrategyId.BlockingByte => ReadBlockingByte(sp, minExpectedSize, quietMs, maxWindowMs),
            _ => Array.Empty<byte>()
        };

    private static byte[] ReadPolling(SerialPort sp, int minExpectedSize, int quietMs, int maxWindowMs)
    {
        var start = Environment.TickCount;
        var lastDataTs = start;
        using var ms = new System.IO.MemoryStream();
        var buf = new byte[4096];

        while (Environment.TickCount - start < maxWindowMs)
        {
            int toRead = Math.Min(buf.Length, sp.BytesToRead);
            if (toRead > 0)
            {
                int n = sp.Read(buf, 0, toRead);
                if (n > 0)
                {
                    ms.Write(buf, 0, n);
                    lastDataTs = Environment.TickCount;
                }
            }
            else
            {
                if (ms.Length >= minExpectedSize && Environment.TickCount - lastDataTs >= quietMs) break;
                Thread.Sleep(5);
            }
        }
        return ms.ToArray();
    }

    private static byte[] ReadBlockingChunk(SerialPort sp, int minExpectedSize, int quietMs, int maxWindowMs)
    {
        var start = Environment.TickCount;
        var lastDataTs = start;
        using var ms = new System.IO.MemoryStream();
        var buf = new byte[1024];
        var originalTimeout = sp.ReadTimeout;
        var timeout = quietMs > 0 ? quietMs : 50;

        try
        {
            sp.ReadTimeout = timeout;
            while (Environment.TickCount - start < maxWindowMs)
            {
                try
                {
                    int n = sp.Read(buf, 0, buf.Length);
                    if (n > 0)
                    {
                        ms.Write(buf, 0, n);
                        lastDataTs = Environment.TickCount;
                    }
                }
                catch (TimeoutException)
                {
                    if (ms.Length >= minExpectedSize && Environment.TickCount - lastDataTs >= quietMs) break;
                }
            }
        }
        finally
        {
            sp.ReadTimeout = originalTimeout;
        }

        return ms.ToArray();
    }

    private static byte[] ReadBlockingByte(SerialPort sp, int minExpectedSize, int quietMs, int maxWindowMs)
    {
        var start = Environment.TickCount;
        var lastDataTs = start;
        using var ms = new System.IO.MemoryStream();
        var buf = new byte[1];
        var originalTimeout = sp.ReadTimeout;
        var timeout = quietMs > 0 ? quietMs : 50;

        try
        {
            sp.ReadTimeout = timeout;
            while (Environment.TickCount - start < maxWindowMs)
            {
                try
                {
                    int n = sp.Read(buf, 0, 1);
                    if (n > 0)
                    {
                        ms.WriteByte(buf[0]);
                        lastDataTs = Environment.TickCount;
                    }
                }
                catch (TimeoutException)
                {
                    if (ms.Length >= minExpectedSize && Environment.TickCount - lastDataTs >= quietMs) break;
                }
            }
        }
        finally
        {
            sp.ReadTimeout = originalTimeout;
        }

        return ms.ToArray();
    }

    public static void Write(SerialPort sp, byte[] data, ILog log, string caption)
    {
        try { sp.Write(data, 0, data.Length); log.HexDump($"TX {caption}", data); }
        catch (Exception ex) { log.Error($"Write failed: {ex.Message}"); throw; }
    }
}
