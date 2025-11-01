using System;

namespace MicroluxErgConnect.Utils;

internal static class ReportHeaderFormatter
{
    public const int LineCount = 4;

    public static string Normalize(string? value)
    {
        var lines = Split(value);
        return string.Join('\n', lines);
    }

    public static string[] Split(string? value)
    {
        var normalized = NormalizeLineEndings(value);
        var parts = normalized.Split(new[] { '\n' }, LineCount, StringSplitOptions.None);
        return EnsureLineCount(parts);
    }

    public static string[] EnsureLineCount(string[]? lines)
    {
        var result = new string[LineCount];
        Array.Fill(result, string.Empty);

        if (lines is null || lines.Length == 0)
        {
            return result;
        }

        var limit = Math.Min(lines.Length, LineCount);
        for (var i = 0; i < limit; i++)
        {
            result[i] = lines[i] ?? string.Empty;
        }

        return result;
    }

    public static string JoinForEditor(string[]? lines)
    {
        var normalized = EnsureLineCount(lines);
        return string.Join(Environment.NewLine, normalized);
    }

    private static string NormalizeLineEndings(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }
}
