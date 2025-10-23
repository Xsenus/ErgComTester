using System;

namespace ErgData;

public static class RenderingSupport
{
    static RenderingSupport()
    {
        PdfSupported = true;
        GraphRenderingSupported = true;

        if (OperatingSystem.IsWindows() && !OperatingSystem.IsWindowsVersionAtLeast(6, 2))
        {
            DisablePdf("Генерация PDF-отчетов недоступна на Windows 7. Требуется Windows 8 или новее.");
            DisableGraphRendering("Построение графиков недоступно на Windows 7. Требуется Windows 8 или новее.");
        }
    }

    public static bool PdfSupported { get; private set; }

    public static string? PdfIssue { get; private set; }

    public static bool GraphRenderingSupported { get; private set; }

    public static string? GraphIssue { get; private set; }

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
