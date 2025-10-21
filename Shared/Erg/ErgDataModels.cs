using System;
using System.Text.Json.Serialization;

namespace ErgData;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnimalKind : byte
{
    Cat = 0,
    Dog = 1,
    Rabbit = 2,
    Horse = 3,
    Other = 4
}

public sealed class CommonInfo
{
    public string ReportName { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string SoftwareRev { get; init; } = string.Empty;
    public int TotalNumId { get; init; }
    public byte Checksum { get; init; }
    public bool ChecksumValid { get; init; }
}

public sealed class ErgPatient
{
    public uint PatientId { get; init; }
    public AnimalKind Animal { get; init; }
    public string TestDateTime { get; init; } = string.Empty;
    public byte TotalNumTests { get; init; }
    public List<ErgTest> Tests { get; init; } = new();
    public string Description { get; init; } = string.Empty;
    public byte Checksum { get; init; }
    public bool ChecksumValid { get; init; }
}

public sealed class ErgTest
{
    public string TestName { get; init; } = string.Empty;
    public byte GraphNumPoints { get; init; }
    public byte GraphDt { get; init; }
    public byte GraphDiscrPerMkV { get; init; }
    public byte GraphFlashPosition { get; init; }
    public byte GraphXValueStep { get; init; }
    public byte GraphXLineStep { get; init; }
    public int GraphXScaleMin { get; init; }
    public int GraphXScaleMax { get; init; }
    public byte GraphYValueStep { get; init; }
    public byte GraphYLineStep { get; init; }
    public int GraphYScaleMin { get; init; }
    public int GraphYScaleMax { get; init; }
    public GraphStyle[] GraphStyles { get; init; } = Array.Empty<GraphStyle>();
    public bool AWaveExists { get; init; }
    public byte AWaveMsNormalMin { get; init; }
    public byte AWaveMsNormalMax { get; init; }
    public uint AWaveMkVNormalMin { get; init; }
    public uint AWaveMkVNormalMax { get; init; }
    public byte BWaveMsNormalMin { get; init; }
    public byte BWaveMsNormalMax { get; init; }
    public uint BWaveMkVNormalMin { get; init; }
    public uint BWaveMkVNormalMax { get; init; }
    public byte Rezerv1 { get; init; }
    public byte Rezerv2 { get; init; }
    public int Rezerv3 { get; init; }
    public EyeData RightEye { get; init; } = new();
    public EyeData LeftEye { get; init; } = new();
}

public sealed record GraphStyle
{
    public int Index { get; init; }
    public byte Red { get; init; }
    public byte Green { get; init; }
    public byte Blue { get; init; }
    public bool Dotted { get; init; }
}

public sealed record EyeData
{
    public bool IsFlat { get; init; }
    public byte QualityIndex { get; init; }
    public byte ValueCount { get; init; }
    public byte[] AWaveMs { get; init; } = Array.Empty<byte>();
    public uint[] AWaveMkV { get; init; } = Array.Empty<uint>();
    public byte[] BWaveMs { get; init; } = Array.Empty<byte>();
    public uint[] BWaveMkV { get; init; } = Array.Empty<uint>();
    public byte AWaveMarker { get; init; }
    public byte BWaveMarker { get; init; }
    public byte GraphCount { get; init; }
    public int[][] Graphs { get; init; } = Array.Empty<int[]>();
    public byte Rezerv1 { get; init; }
    public byte Rezerv2 { get; init; }
    public int Rezerv3 { get; init; }
}
