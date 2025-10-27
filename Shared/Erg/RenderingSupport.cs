using System;

namespace ErgData;

public static class RenderingSupport
{
    static RenderingSupport() => Reload();

    public static bool PdfSupported { get; private set; }

    public static string? PdfIssue { get; private set; }

    public static bool GraphRenderingSupported { get; private set; }

    public static string? GraphIssue { get; private set; }

    public static bool UseLegacyPdfGeneration { get; private set; }

    public static bool UseLegacyGraphRendering { get; private set; }

    public static string? LegacyRenderingNotice { get; private set; }

    public static void Reload()
    {
        PdfSupported = true;
        PdfIssue = null;
        GraphRenderingSupported = true;
        GraphIssue = null;
        UseLegacyPdfGeneration = false;
        UseLegacyGraphRendering = false;
        LegacyRenderingNotice = null;

        var legacyOverride = Environment.GetEnvironmentVariable("ERG_FORCE_LEGACY_RENDERING");
        if (!string.IsNullOrWhiteSpace(legacyOverride) && !legacyOverride.Equals("0", StringComparison.OrdinalIgnoreCase))
        {
            UseLegacyPdfGeneration = true;
            UseLegacyGraphRendering = true;
            LegacyRenderingNotice =
                "Принудительно включен совместимый режим построения графиков и генерации PDF.";
            return;
        }

        if (OperatingSystem.IsWindows() && !OperatingSystem.IsWindowsVersionAtLeast(6, 2))
        {
            UseLegacyPdfGeneration = true;
            UseLegacyGraphRendering = true;
            LegacyRenderingNotice =
                "Обнаружена Windows 7. Включен совместимый режим построения графиков и генерации PDF-отчетов.";
        }
    }

    public static void DisablePdf(string reason)
    {
        PdfSupported = false;
        PdfIssue = string.IsNullOrWhiteSpace(reason)
            ? "Генерация PDF отключена."
            : reason;
    }

    public static void DisableGraphRendering(string reason)
    {
        GraphRenderingSupported = false;
        GraphIssue = string.IsNullOrWhiteSpace(reason)
            ? "Построение графиков отключено."
            : reason;
    }
}
