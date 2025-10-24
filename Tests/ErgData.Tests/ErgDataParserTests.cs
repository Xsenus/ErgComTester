using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;
using ErgData;
using Xunit;

namespace ErgData.Tests;

public sealed class ErgDataParserTests
{
    static ErgDataParserTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static byte[] BuildCommonInfoFrame()
    {
        var body = new MemoryStream();
        using (var writer = new BinaryWriter(body, Encoding.ASCII, leaveOpen: true))
        {
            WriteFixedString(writer, "ERG Report", 64, Encoding.GetEncoding(1251));
            WriteFixedString(writer, "Microlux", 64, Encoding.GetEncoding(1251));
            WriteFixedString(writer, "1.3", 6, Encoding.ASCII);
            writer.Write((byte)5);
        }

        var checksum = ComputeChecksum(body.ToArray());
        var frame = body.ToArray().Concat(new byte[] { checksum }).ToArray();
        return frame;
    }

    private static byte[] BuildPatientFrame(Action<TestBuilder>? configure = null)
    {
        var builder = TestBuilder.CreateDefault();
        configure?.Invoke(builder);

        var body = new MemoryStream();
        using (var writer = new BinaryWriter(body, Encoding.ASCII, leaveOpen: true))
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, 12345);
            writer.Write(buffer);
            writer.Write((byte)AnimalKind.Dog);
            WriteFixedString(writer, "15/09/2025 12:15", 18, Encoding.GetEncoding(1251));
            writer.Write((byte)1); // total tests

            WriteTest(writer, builder);

            WriteFixedString(writer, "Описание пациента", 500, Encoding.GetEncoding(1251));
        }

        var checksum = ComputeChecksum(body.ToArray());
        var frame = body.ToArray().Concat(new byte[] { checksum }).ToArray();
        return frame;
    }

    private static void WriteTest(BinaryWriter writer, TestBuilder builder)
    {
        var enc = Encoding.GetEncoding(1251);
        WriteFixedString(writer, builder.TestName, 100, enc);
        writer.Write(builder.GraphNumPoints);
        writer.Write(builder.GraphDt);
        writer.Write(builder.GraphDiscr);
        writer.Write(builder.GraphFlashPosition);
        writer.Write(builder.GraphXValueStep);
        writer.Write(builder.GraphXLineStep);
        WriteInt16BigEndian(writer, builder.GraphXScaleMin);
        WriteInt16BigEndian(writer, builder.GraphXScaleMax);
        writer.Write(builder.GraphYValueStep);
        writer.Write(builder.GraphYLineStep);
        WriteInt16BigEndian(writer, builder.GraphYScaleMin);
        WriteInt16BigEndian(writer, builder.GraphYScaleMax);

        var colors = new (byte R, byte G, byte B)[]
        {
            (255, 0, 0),
            (0, 255, 0),
            (0, 0, 255),
            (128, 128, 128),
            (255, 255, 0),
            (0, 255, 255)
        };
        foreach (var color in colors)
        {
            writer.Write(color.R);
            writer.Write(color.G);
            writer.Write(color.B);
        }

        var dotted = new byte[] { 0, 1, 0, 1, 0, 0 };
        writer.Write(dotted);

        writer.Write(builder.AWaveExists ? (byte)1 : (byte)0);
        writer.Write(builder.AWaveMsNormalMin);
        writer.Write(builder.AWaveMsNormalMax);
        WriteUInt16BigEndian(writer, builder.AWaveMkVNormalMin);
        WriteUInt16BigEndian(writer, builder.AWaveMkVNormalMax);
        writer.Write(builder.BWaveMsNormalMin);
        writer.Write(builder.BWaveMsNormalMax);
        WriteUInt16BigEndian(writer, builder.BWaveMkVNormalMin);
        WriteUInt16BigEndian(writer, builder.BWaveMkVNormalMax);
        writer.Write((byte)0);
        writer.Write((byte)0);
        WriteInt16BigEndian(writer, 0);

        WriteEye(writer, flat: false, builder.RightEye);
        WriteEye(writer, flat: true, builder.LeftEye);
    }

    private static void WriteEye(BinaryWriter writer, bool flat, EyeOptions? options)
    {
        options ??= flat ? EyeOptions.DefaultLeft : EyeOptions.DefaultRight;

        writer.Write((byte)(flat ? 1 : 0));

        byte? quality = options.Quality ?? (flat ? (byte?)null : (byte?)3);
        writer.Write((byte)(quality ?? 255));

        byte? valueCount = options.ValueCount ?? (flat ? (byte?)null : (byte?)2);
        writer.Write((byte)(valueCount ?? 255));

        var aMs = BuildArray(options.AWaveMs, flat ? NullMarkers : DefaultRightAWaveMs, 3);
        var bMs = BuildArray(options.BWaveMs, flat ? NullMarkers : DefaultRightBWaveMs, 3);
        var aMkV = BuildArray(options.AWaveMkV, flat ? NullAmplitudes : DefaultRightAWaveMkV, 6);
        var bMkV = BuildArray(options.BWaveMkV, flat ? NullAmplitudes : DefaultRightBWaveMkV, 6);

        WriteMarkerArray(writer, aMs);
        WriteAmplitudeArray(writer, aMkV);
        WriteMarkerArray(writer, bMs);
        WriteAmplitudeArray(writer, bMkV);

        byte? aMarker = options.AWaveMarker ?? (flat ? (byte?)null : (byte?)14);
        writer.Write((byte)(aMarker ?? 255));

        byte? bMarker = options.BWaveMarker ?? (flat ? (byte?)null : (byte?)56);
        writer.Write((byte)(bMarker ?? 255));

        byte graphCount = options.GraphCount ?? (byte)(flat ? 0 : 1);
        writer.Write(graphCount);

        var sampleFactory = options.SampleFactory ?? (flat
            ? (_, _) => (short)0
            : (int graph, int point) => (short)(graph == 0 ? point - 64 : 0));

        for (int graph = 0; graph < 6; graph++)
        {
            for (int point = 0; point < 128; point++)
            {
                writer.Write(sampleFactory(graph, point));
            }
        }

        writer.Write((byte)0);
        writer.Write((byte)0);
        WriteInt16BigEndian(writer, 0);
    }

    private static void WriteInt16BigEndian(BinaryWriter writer, short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        writer.Write(buffer);
    }

    private static void WriteUInt16BigEndian(BinaryWriter writer, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        writer.Write(buffer);
    }

    private static ushort?[] BuildArray(ushort?[]? source, ushort?[] fallback, int length)
    {
        var result = new ushort?[length];
        var values = source ?? fallback;
        for (int i = 0; i < length; i++)
        {
            result[i] = i < values.Length ? values[i] : null;
        }

        return result;
    }

    private static void WriteMarkerArray(BinaryWriter writer, ushort?[] values)
    {
        var buffer = new byte[values.Length * 2];
        for (int i = 0; i < values.Length; i++)
        {
            ushort raw = values[i] ?? ushort.MaxValue;
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(i * 2, 2), raw);
        }

        writer.Write(buffer);
    }

    private static void WriteAmplitudeArray(BinaryWriter writer, ushort?[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            ushort raw = values[i] ?? ushort.MaxValue;
            WriteUInt16BigEndian(writer, raw);
        }
    }

    private static readonly ushort?[] NullMarkers = new ushort?[] { null, null, null };
    private static readonly ushort?[] NullAmplitudes = new ushort?[] { null, null, null, null, null, null };
    private static readonly ushort?[] DefaultRightAWaveMs = new ushort?[] { 15, 16, null };
    private static readonly ushort?[] DefaultRightBWaveMs = new ushort?[] { 55, 57, null };
    private static readonly ushort?[] DefaultRightAWaveMkV = new ushort?[] { 120, 110, null, null, null, null };
    private static readonly ushort?[] DefaultRightBWaveMkV = new ushort?[] { 200, 190, null, null, null, null };

    private sealed record EyeOptions
    {
        public byte? Quality { get; init; }
        public byte? ValueCount { get; init; }
        public ushort?[]? AWaveMs { get; init; }
        public ushort?[]? BWaveMs { get; init; }
        public ushort?[]? AWaveMkV { get; init; }
        public ushort?[]? BWaveMkV { get; init; }
        public byte? AWaveMarker { get; init; }
        public byte? BWaveMarker { get; init; }
        public byte? GraphCount { get; init; }
        public Func<int, int, short>? SampleFactory { get; init; }

        public static EyeOptions DefaultRight { get; } = new EyeOptions
        {
            Quality = 3,
            ValueCount = 2,
            AWaveMs = DefaultRightAWaveMs,
            BWaveMs = DefaultRightBWaveMs,
            AWaveMkV = DefaultRightAWaveMkV,
            BWaveMkV = DefaultRightBWaveMkV,
            AWaveMarker = 14,
            BWaveMarker = 56,
            GraphCount = 1,
            SampleFactory = (graph, point) => (short)(graph == 0 ? point - 64 : 0)
        };

        public static EyeOptions DefaultLeft { get; } = new EyeOptions
        {
            Quality = null,
            ValueCount = null,
            AWaveMs = NullMarkers,
            BWaveMs = NullMarkers,
            AWaveMkV = NullAmplitudes,
            BWaveMkV = NullAmplitudes,
            AWaveMarker = null,
            BWaveMarker = null,
            GraphCount = 0,
            SampleFactory = (_, _) => 0
        };
    }

    private sealed class TestBuilder
    {
        public string TestName { get; set; } = "DA 0.01";
        public byte GraphNumPoints { get; set; } = 128;
        public byte GraphDt { get; set; } = 5;
        public byte GraphDiscr { get; set; } = 2;
        public byte GraphFlashPosition { get; set; } = 12;
        public byte GraphXValueStep { get; set; } = 1;
        public byte GraphXLineStep { get; set; } = 5;
        public short GraphXScaleMin { get; set; } = -50;
        public short GraphXScaleMax { get; set; } = 150;
        public byte GraphYValueStep { get; set; } = 2;
        public byte GraphYLineStep { get; set; } = 10;
        public short GraphYScaleMin { get; set; } = -100;
        public short GraphYScaleMax { get; set; } = 180;
        public bool AWaveExists { get; set; } = true;
        public byte AWaveMsNormalMin { get; set; } = 10;
        public byte AWaveMsNormalMax { get; set; } = 20;
        public ushort AWaveMkVNormalMin { get; set; } = 50;
        public ushort AWaveMkVNormalMax { get; set; } = 350;
        public byte BWaveMsNormalMin { get; set; } = 40;
        public byte BWaveMsNormalMax { get; set; } = 80;
        public ushort BWaveMkVNormalMin { get; set; } = 20;
        public ushort BWaveMkVNormalMax { get; set; } = 250;
        public EyeOptions RightEye { get; set; } = EyeOptions.DefaultRight;
        public EyeOptions LeftEye { get; set; } = EyeOptions.DefaultLeft;

        public static TestBuilder CreateDefault() => new TestBuilder();
    }

    [Fact]
    public void TryParseCommonInfo_ReturnsStructuredData()
    {
        var frame = BuildCommonInfoFrame();
        var success = ErgDataParser.TryParseCommonInfo(frame, out var info, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("ERG Report", info.ReportName);
        Assert.Equal("Microlux", info.DeviceName);
        Assert.Equal("1.3", info.SoftwareRev);
        Assert.Equal(5, info.TotalNumId);
        Assert.True(info.ChecksumValid);
        Assert.Equal(frame[^1], info.Checksum);
    }

    [Fact]
    public void TryParsePatient_ReturnsPopulatedModel()
    {
        var frame = BuildPatientFrame();
        var success = ErgDataParser.TryParsePatient(frame, out var patient, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal((uint)12345, patient.PatientId);
        Assert.Equal(AnimalKind.Dog, patient.Animal);
        Assert.Equal("15/09/2025 12:15", patient.TestDateTime.Trim());
        Assert.Equal((byte)1, patient.TotalNumTests);
        Assert.Single(patient.Tests);
        Assert.Equal("Описание пациента", patient.Description.Trim());
        Assert.True(patient.ChecksumValid);
        Assert.Empty(patient.Warnings);

        var test = patient.Tests.Single();
        Assert.Equal("DA 0.01", test.TestName);
        Assert.True(test.AWaveExists);
        Assert.Equal<byte?>(10, test.AWaveMsNormalMin);
        Assert.Equal<byte?>(20, test.AWaveMsNormalMax);
        Assert.Equal<uint?>(50u, test.AWaveMkVNormalMin);
        Assert.Equal<uint?>(350u, test.AWaveMkVNormalMax);
        Assert.Equal<byte?>(40, test.BWaveMsNormalMin);
        Assert.Equal<byte?>(80, test.BWaveMsNormalMax);
        Assert.Equal<uint?>(20u, test.BWaveMkVNormalMin);
        Assert.Equal<uint?>(250u, test.BWaveMkVNormalMax);

        Assert.False(test.RightEye.IsFlat);
        Assert.True(test.LeftEye.IsFlat);
        Assert.Equal<byte?>(3, test.RightEye.QualityIndex);
        Assert.Null(test.LeftEye.QualityIndex);
        Assert.Equal<byte?>(2, test.RightEye.ValueCount);
        Assert.Null(test.LeftEye.ValueCount);
        Assert.Equal<byte?>(14, test.RightEye.AWaveMarker);
        Assert.Null(test.LeftEye.AWaveMarker);
        Assert.Equal<byte?>(56, test.RightEye.BWaveMarker);
        Assert.Null(test.LeftEye.BWaveMarker);

        Assert.NotNull(test.RightEye.AWaveMs);
        Assert.Equal<ushort?>(15, test.RightEye.AWaveMs![0]);
        Assert.Equal<ushort?>(16, test.RightEye.AWaveMs![1]);
        Assert.Null(test.RightEye.AWaveMs![2]);
        Assert.NotNull(test.RightEye.AWaveMkV);
        Assert.Equal<uint?>(120u, test.RightEye.AWaveMkV![0]);
        Assert.Equal<uint?>(110u, test.RightEye.AWaveMkV![1]);
        Assert.Null(test.RightEye.AWaveMkV![2]);

        Assert.NotNull(test.RightEye.BWaveMs);
        Assert.Equal<ushort?>(55, test.RightEye.BWaveMs![0]);
        Assert.Equal<ushort?>(57, test.RightEye.BWaveMs![1]);
        Assert.Null(test.RightEye.BWaveMs![2]);
        Assert.NotNull(test.RightEye.BWaveMkV);
        Assert.Equal<uint?>(200u, test.RightEye.BWaveMkV![0]);
        Assert.Equal<uint?>(190u, test.RightEye.BWaveMkV![1]);
        Assert.Null(test.RightEye.BWaveMkV![2]);

        Assert.NotNull(test.LeftEye.BWaveMs);
        Assert.Null(test.LeftEye.BWaveMs![0]);
        Assert.NotNull(test.LeftEye.BWaveMkV);
        Assert.Null(test.LeftEye.BWaveMkV![0]);

        Assert.Equal(128, test.GraphNumPoints);
        Assert.Equal(5, test.GraphDt);
        Assert.Equal(-50, test.GraphXScaleMin);
        Assert.Equal(150, test.GraphXScaleMax);
        Assert.Equal(-100, test.GraphYScaleMin);
        Assert.Equal(180, test.GraphYScaleMax);
        Assert.Equal<byte>(1, test.RightEye.GraphCount);
        Assert.NotNull(test.RightEye.GraphSamples);
        var rawGraph = Assert.Single(test.RightEye.GraphSamples!);
        Assert.Equal(128, rawGraph.Length);
        Assert.Equal<short>(-64, rawGraph[0]);
        Assert.Equal<short>(63, rawGraph[127]);

        Assert.NotNull(test.RightEye.GraphsNormalized);
        var normalizedGraph = Assert.Single(test.RightEye.GraphsNormalized!);
        Assert.Equal(-32.0, normalizedGraph[0], 5);
        Assert.Equal(31.5, normalizedGraph[127], 5);

        Assert.Equal<byte>(0, test.LeftEye.GraphCount);
        Assert.Null(test.LeftEye.GraphSamples);
        Assert.Null(test.LeftEye.GraphsNormalized);
    }

    [Fact]
    public void TryParsePatient_ScalesGraphAmplitudesUsingMarkers()
    {
        var frame = BuildPatientFrame(builder =>
        {
            builder.GraphDt = 1;
            builder.GraphDiscr = 14;
            builder.RightEye = builder.RightEye with
            {
                ValueCount = 1,
                BWaveMs = new ushort?[] { 34, null, null },
                BWaveMkV = new ushort?[] { 14, null, null, null, null, null },
                BWaveMarker = 34,
                GraphCount = 1,
                SampleFactory = (graph, point) =>
                {
                    if (graph == 0 && point == 34)
                        return (short)(14 * 14 * 40);
                    return 0;
                }
            };
        });

        var success = ErgDataParser.TryParsePatient(frame, out var patient, out var error);

        Assert.True(success);
        Assert.Null(error);

        var test = patient.Tests.Single();
        Assert.Equal((byte)1, test.GraphDt);
        Assert.Equal((byte)14, test.GraphDiscrPerMkV);

        Assert.NotNull(test.RightEye.GraphSamples);
        var rawGraph = Assert.Single(test.RightEye.GraphSamples!);
        Assert.Equal<short>(14 * 14 * 40, rawGraph[34]);

        Assert.NotNull(test.RightEye.GraphsNormalized);
        var graph = Assert.Single(test.RightEye.GraphsNormalized!);
        Assert.InRange(graph[34], 13.5, 14.5);
        Assert.Equal<uint?>(14u, test.RightEye.BWaveMkV![0]);
        Assert.Equal<ushort?>(34, test.RightEye.BWaveMs![0]);
    }

    private static void WriteFixedString(BinaryWriter writer, string value, int length, Encoding encoding)
    {
        var bytes = new byte[length];
        var data = encoding.GetBytes(value);
        var count = Math.Min(data.Length, length - 1);
        Array.Copy(data, bytes, count);
        writer.Write(bytes);
    }

    private static byte ComputeChecksum(ReadOnlySpan<byte> data)
    {
        int sum = 0;
        foreach (var b in data)
            sum += b;
        return (byte)(sum & 0xFF);
    }
}
