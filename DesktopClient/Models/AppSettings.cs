using System;
using System.IO;
using System.Text.Json.Serialization;
using ErgData;

namespace MicroluxErgConnect.Models;

public class AppSettings
{
    public const string LegacyManifestUrl = "https://updates.microlux.ru/manifest.json";
    public const string DefaultManifestUrl = "ftp://90.189.149.59/other/ErgComTester/ErgComTester.Update.xml";

    public string? PreferredPort { get; set; }
    public int DeviceScanIntervalSeconds { get; set; } = 5;
    public int DeviceReconnectDelaySeconds { get; set; } = 15;
    public int UpdateCheckIntervalMinutes { get; set; } = 60;
    public bool AutoDownloadUpdates { get; set; } = true;
    public string UpdateManifestUrl { get; set; } = DefaultManifestUrl;
    public SerialCommunicationOptions Serial { get; set; } = SerialCommunicationOptions.CreateDefault();
    public int BackgroundSyncIntervalMinutes { get; set; } = 30;
    public string ReportsDirectory { get; set; } = ResolveDefaultReportsDirectory();
    public string LogsDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Microlux ERG Connect", "Logs");
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public TelegramSettings Telegram { get; set; } = TelegramSettings.CreateDefault();
    public bool GenerateWordReports { get; set; } = false;
    public bool WriteLogsToFile { get; set; } = false;
    public bool KeepRawPatientFiles { get; set; } = false;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReportTemplate ReportTemplate { get; set; } = ReportTemplate.Client;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReportRenderingMode ReportRenderingMode { get; set; } = ReportRenderingMode.Automatic;
    public string ReportHeader { get; set; } = string.Empty;
    public GraphRenderOptionsDto? GraphOptions { get; set; }
    public TimeSpan DeviceScanInterval => TimeSpan.FromSeconds(Math.Clamp(DeviceScanIntervalSeconds, 2, 60));
    public TimeSpan DeviceReconnectDelay => TimeSpan.FromSeconds(Math.Clamp(DeviceReconnectDelaySeconds, 5, 300));
    public TimeSpan UpdateCheckInterval => TimeSpan.FromMinutes(Math.Clamp(UpdateCheckIntervalMinutes, 5, 24 * 60));
    public TimeSpan BackgroundSyncInterval => TimeSpan.FromMinutes(Math.Clamp(BackgroundSyncIntervalMinutes, 5, 24 * 60));

    private static string ResolveDefaultReportsDirectory()
    {
        var commonDocuments = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
        if (!string.IsNullOrWhiteSpace(commonDocuments))
        {
            try
            {
                var publicRoot = Path.GetFullPath(Path.Combine(commonDocuments, ".."));
                return Path.Combine(publicRoot, "Microlux ERG-Reports");
            }
            catch
            {
                // Игнорируем ошибки и используем запасной путь ниже.
            }
        }

        var fallbackBase = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(fallbackBase))
        {
            fallbackBase = Environment.CurrentDirectory;
        }

        return Path.Combine(fallbackBase, "Microlux ERG-Reports");
    }
}

public sealed class GraphRenderOptionsDto
{
    public float MajorTickLenMm { get; set; }
    public float MinorTickLenMm { get; set; }
    public float AxisThicknessPx { get; set; }
    public float TickThicknessPx { get; set; }
    public float CurveThicknessPx { get; set; }
    public float ExtremumThicknessPx { get; set; }
    public float? HorizontalMarkerThicknessPx { get; set; }
    public float GridThicknessPx { get; set; }
    public float DottedDashLengthPx { get; set; }
    public float DottedGapLengthPx { get; set; }
    public float LabelFontPt { get; set; }
    public float UnitsFontPt { get; set; }
    public float MarginLeft { get; set; }
    public float MarginRight { get; set; }
    public float MarginTop { get; set; }
    public float MarginBottom { get; set; }
    public float AxisGapHorizontal { get; set; }
    public float AxisGapVertical { get; set; }
    public float XDigitsOffsetPx { get; set; }
    public float XUnitsGapPx { get; set; }
    public float MinLabelGapXPx { get; set; }
    public float MinLabelGapYPx { get; set; }
    public float YDigitsLeftPadPx { get; set; }
    public float YUnitsGapFromNumbersPx { get; set; }
    public float YUnitsFallbackFromAxisPx { get; set; }

    public static GraphRenderOptionsDto From(ErgReportBuilder.GraphRenderOptions o) => new()
    {
        MajorTickLenMm = o.MajorTickLenMm,
        MinorTickLenMm = o.MinorTickLenMm,
        AxisThicknessPx = o.AxisThicknessPx,
        TickThicknessPx = o.TickThicknessPx,
        CurveThicknessPx = o.CurveThicknessPx,
        ExtremumThicknessPx = o.ExtremumThicknessPx,
        HorizontalMarkerThicknessPx = o.HorizontalMarkerThicknessPx,
        GridThicknessPx = o.GridThicknessPx,
        DottedDashLengthPx = o.DottedDashLengthPx,
        DottedGapLengthPx = o.DottedGapLengthPx,
        LabelFontPt = o.LabelFontPt,
        UnitsFontPt = o.UnitsFontPt,
        MarginLeft = o.MarginLeft,
        MarginRight = o.MarginRight,
        MarginTop = o.MarginTop,
        MarginBottom = o.MarginBottom,
        AxisGapHorizontal = o.AxisGapHorizontal,
        AxisGapVertical = o.AxisGapVertical,
        XDigitsOffsetPx = o.XDigitsOffsetPx,
        XUnitsGapPx = o.XUnitsGapPx,
        MinLabelGapXPx = o.MinLabelGapXPx,
        MinLabelGapYPx = o.MinLabelGapYPx,
        YDigitsLeftPadPx = o.YDigitsLeftPadPx,
        YUnitsGapFromNumbersPx = o.YUnitsGapFromNumbersPx,
        YUnitsFallbackFromAxisPx = o.YUnitsFallbackFromAxisPx
    };

    public void ApplyTo(ErgReportBuilder.GraphRenderOptions o)
    {
        o.MajorTickLenMm = MajorTickLenMm;
        o.MinorTickLenMm = MinorTickLenMm;
        o.AxisThicknessPx = AxisThicknessPx;
        o.TickThicknessPx = TickThicknessPx;
        o.CurveThicknessPx = CurveThicknessPx;
        o.ExtremumThicknessPx = ExtremumThicknessPx;
        if (HorizontalMarkerThicknessPx.HasValue)
            o.HorizontalMarkerThicknessPx = HorizontalMarkerThicknessPx.Value;
        o.GridThicknessPx = GridThicknessPx;
        if (DottedDashLengthPx > 0f)
            o.DottedDashLengthPx = ErgReportBuilder.GraphRenderOptions.NormalizeDottedLength(DottedDashLengthPx, o.DottedDashLengthPx);
        if (DottedGapLengthPx > 0f)
            o.DottedGapLengthPx = ErgReportBuilder.GraphRenderOptions.NormalizeDottedLength(DottedGapLengthPx, o.DottedGapLengthPx);
        o.LabelFontPt = LabelFontPt;
        o.UnitsFontPt = UnitsFontPt;
        o.MarginLeft = MarginLeft;
        o.MarginRight = MarginRight;
        o.MarginTop = MarginTop;
        o.MarginBottom = MarginBottom;
        o.AxisGapHorizontal = AxisGapHorizontal;
        o.AxisGapVertical = AxisGapVertical;
        o.XDigitsOffsetPx = XDigitsOffsetPx;
        o.XUnitsGapPx = XUnitsGapPx;
        o.MinLabelGapXPx = MinLabelGapXPx;
        o.MinLabelGapYPx = MinLabelGapYPx;
        o.YDigitsLeftPadPx = YDigitsLeftPadPx;
        o.YUnitsGapFromNumbersPx = YUnitsGapFromNumbersPx;
        o.YUnitsFallbackFromAxisPx = YUnitsFallbackFromAxisPx;
    }
}