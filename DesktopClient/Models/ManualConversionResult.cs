using ErgData;

namespace MicroluxErgConnect.Models;

public sealed record ManualConversionResult
{
    public bool Success { get; init; }
    public string RawPath { get; init; } = string.Empty;
    public string? JsonPath { get; init; }
    public string? PdfPath { get; init; }
    public string? DocxPath { get; init; }
    public string? ErrorMessage { get; init; }
    public ErgPatient? Patient { get; init; }
}
