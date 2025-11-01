using System;
using System.Globalization;
using ErgData;

namespace MicroluxErgConnect.Utils;

internal static class ReportFileNaming
{
    private static readonly CultureInfo[] Cultures =
    {
        CultureInfo.GetCultureInfo("ru-RU"),
        CultureInfo.InvariantCulture
    };

    private static readonly string[] DateFormats =
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

    public static ReportFileNameResult CreatePdfFileName(ErgPatient patient, DateTime fallback)
    {
        if (patient == null)
        {
            throw new ArgumentNullException(nameof(patient));
        }

        var timestamp = fallback;
        var usedFallback = true;
        if (TryParseDate(patient.TestDateTime, out var parsed))
        {
            timestamp = parsed;
            usedFallback = false;
        }

        var id = patient.PatientId.ToString(CultureInfo.InvariantCulture);
        var fileName = $"{id}_{timestamp:yyMMddHHmm}.pdf";
        return new ReportFileNameResult(fileName, timestamp, usedFallback);
    }

    private static bool TryParseDate(string? value, out DateTime result)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            result = default;
            return false;
        }

        foreach (var culture in Cultures)
        {
            if (DateTime.TryParse(trimmed, culture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var parsed))
            {
                result = parsed;
                return true;
            }
        }

        foreach (var culture in Cultures)
        {
            foreach (var format in DateFormats)
            {
                if (DateTime.TryParseExact(trimmed, format, culture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var parsedExact))
                {
                    result = parsedExact;
                    return true;
                }
            }
        }

        result = default;
        return false;
    }
}

internal readonly record struct ReportFileNameResult(string FileName, DateTime Timestamp, bool UsedFallback);
