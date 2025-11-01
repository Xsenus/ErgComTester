using System;
using System.Globalization;

namespace ErgData;

public static class ErgDateParser
{
    private static readonly string[] SupportedFormats =
    {
        "dd.MM.yyyy HH:mm",
        "dd.MM.yyyy H:mm",
        "dd/MM/yyyy HH:mm",
        "dd/MM/yyyy H:mm",
        "dd-MM-yyyy HH:mm",
        "dd-MM-yyyy H:mm",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd H:mm",
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.FFF",
        "yyyy.MM.dd HH:mm"
    };

    private static readonly CultureInfo[] SupportedCultures =
    {
        CultureInfo.GetCultureInfo("ru-RU"),
        CultureInfo.InvariantCulture
    };

    public static bool TryParseTestDateTime(string? value, out DateTime result)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            result = default;
            return false;
        }

        foreach (var culture in SupportedCultures)
        {
            if (DateTime.TryParse(trimmed, culture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out result))
            {
                return true;
            }
        }

        foreach (var format in SupportedFormats)
        {
            foreach (var culture in SupportedCultures)
            {
                if (DateTime.TryParseExact(trimmed, format, culture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out result))
                {
                    return true;
                }
            }
        }

        result = default;
        return false;
    }
}
