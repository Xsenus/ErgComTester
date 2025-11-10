using System;
using System.Globalization;
using System.IO;
using System.Text;
using MicroluxErgConnect.Models;
using MicroluxErgConnect;

namespace MicroluxErgConnect.Services;

public sealed class LogService : IDisposable, ILog
{
    private readonly object _lock = new();
    private readonly StreamWriter _writer;

    public event EventHandler<LogEntry>? LogAdded;

    public string SessionLogPath { get; }
    public bool IsFileLoggingEnabled { get; }

    public LogService(SettingsService settings)
    {
        IsFileLoggingEnabled = settings.Current.WriteLogsToFile;

        var logsDirectory = settings.Current.LogsDirectory;
        if (string.IsNullOrWhiteSpace(logsDirectory))
        {
            logsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Microlux ERG Connect", "Logs");
        }

        if (IsFileLoggingEnabled)
        {
            Directory.CreateDirectory(logsDirectory);
        }

        SessionLogPath = Path.Combine(logsDirectory, $"ergconnect_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        _writer = IsFileLoggingEnabled
            ? new StreamWriter(File.Open(SessionLogPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true,
                NewLine = "\n"
            }
            : new StreamWriter(Stream.Null)
            {
                AutoFlush = false,
                NewLine = "\n"
            };
        Info("Журнал сеанса создан.");
    }

    private void Write(string level, string message)
    {
        var entry = new LogEntry(DateTime.Now, level, message);
        lock (_lock)
        {
            var line = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}", entry.Timestamp, level, message);
            _writer.WriteLine(line);
        }
        LogAdded?.Invoke(this, entry);
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);
    public void Debug(string message) => Write("DEBUG", message);

    public void Section(string title)
    {
        Write("INFO", new string('=', Math.Max(8, title.Length + 4)));
        Write("INFO", $"= {title} =");
        Write("INFO", new string('=', Math.Max(8, title.Length + 4)));
    }

    public void HexDump(string caption, byte[] data, int width = 16)
    {
        Info($"{caption}: {data.Length} байт");
        for (int offset = 0; offset < data.Length; offset += width)
        {
            var count = Math.Min(width, data.Length - offset);
            var hex = new StringBuilder();
            var ascii = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var b = data[offset + i];
                hex.AppendFormat(CultureInfo.InvariantCulture, "{0:X2} ", b);
                ascii.Append(b is >= 32 and <= 126 ? (char)b : '.');
            }
            Info(string.Format(CultureInfo.InvariantCulture, "{0:0000}: {1,-48} |{2}|", offset, hex.ToString(), ascii.ToString()));
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Dispose();
        }
    }
}
