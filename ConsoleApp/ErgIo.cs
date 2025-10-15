using System.IO.Ports;

namespace ErgComTester;

internal static class ErgIo
{
    public static byte[] ReadChunk(SerialPort sp, Logger log, int minExpectedSize, int quietMs, int maxWindowMs)
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
                if (n > 0) { ms.Write(buf, 0, n); lastDataTs = Environment.TickCount; }
            }
            else
            {
                if (ms.Length >= minExpectedSize && Environment.TickCount - lastDataTs >= quietMs) break;
                Thread.Sleep(5);
            }
        }
        return ms.ToArray();
    }

    public static void Write(SerialPort sp, byte[] data, Logger log, string caption)
    {
        try { sp.Write(data, 0, data.Length); log.HexDump($"TX {caption}", data); }
        catch (Exception ex) { log.Error($"Write failed: {ex.Message}"); throw; }
    }
}
