using System.Text;

namespace ErgComTester;

internal sealed class Logger : IDisposable
{
    private readonly object _lock = new();
    private readonly StreamWriter _sw;
    private readonly bool _verbose;

    public Logger(string path, bool verbose)
    {
        _verbose = verbose;
        _sw = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read))
        { AutoFlush = true, NewLine = "\n" };
    }

    private void WriteLine(string level, string message, ConsoleColor? color = null)
    {
        var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"{ts} [{level}] {message}";
        lock (_lock)
        {
            var prev = Console.ForegroundColor;
            if (color.HasValue) Console.ForegroundColor = color.Value;
            Console.WriteLine(line);
            Console.ForegroundColor = prev;
            _sw.WriteLine(line);
        }
    }

    public void Debug(string m) => WriteLine("DEBUG", m, ConsoleColor.DarkGray);

    public void Dispose() => _sw.Dispose();
    public void Error(string m) => WriteLine("ERROR", m, ConsoleColor.Red);

    public void HexDump(string caption, byte[] data, int width = 16)
    {
        Info($"{caption}: {data.Length} bytes");
        int offset = 0;
        while (offset < data.Length)
        {
            int count = Math.Min(width, data.Length - offset);
            Span<byte> slice = data.AsSpan(offset, count);
            var hex = BitConverter.ToString(slice.ToArray()).Replace('-', ' ');
            var ascii = new StringBuilder();
            foreach (var b in slice) ascii.Append(b >= 32 && b <= 126 ? (char)b : '.');
            Info($"{offset:0000}: {hex,-48}  |{ascii}|");
            offset += count;
        }
    }

    public void Info(string m) => WriteLine("INFO", m);

    public void Section(string title)
    {
        var sep = new string('-', Math.Max(20, title.Length + 4));
        Info(sep);
        Info($":: {title} ::");
        Info(sep);
    }
    public void Warn(string m) => WriteLine("WARN", m, ConsoleColor.Yellow);
}
