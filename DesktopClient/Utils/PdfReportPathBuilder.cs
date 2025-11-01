using System;
using System.IO;
using ErgData;

namespace MicroluxErgConnect.Utils;

internal static class PdfReportPathBuilder
{
    public static string BuildFileName(ErgPatient patient, DateTime? fallback = null)
    {
        if (patient is null)
            throw new ArgumentNullException(nameof(patient));

        var timestamp = ErgDateParser.TryParseTestDateTime(patient.TestDateTime, out var parsed)
            ? parsed
            : (fallback ?? DateTime.Now);

        return $"{patient.PatientId}_{timestamp:yyMMddHHmm}.pdf";
    }

    public static string BuildFilePath(ErgPatient patient, string directory, DateTime? fallback = null)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Каталог для PDF не задан.", nameof(directory));

        var fullDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory));
        Directory.CreateDirectory(fullDirectory);
        var fileName = BuildFileName(patient, fallback);
        return Path.Combine(fullDirectory, fileName);
    }
}
