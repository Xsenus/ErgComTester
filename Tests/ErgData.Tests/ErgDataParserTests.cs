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

    private static byte[] BuildPatientFrame()
    {
        var body = new MemoryStream();
        using (var writer = new BinaryWriter(body, Encoding.ASCII, leaveOpen: true))
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, 12345);
            writer.Write(buffer);
            writer.Write((byte)AnimalKind.Dog);
            WriteFixedString(writer, "15/09/2025 12:15", 18, Encoding.GetEncoding(1251));
            writer.Write((byte)1); // total tests

            WriteTest(writer);

            WriteFixedString(writer, "Описание пациента", 500, Encoding.GetEncoding(1251));
        }

        var checksum = ComputeChecksum(body.ToArray());
        var frame = body.ToArray().Concat(new byte[] { checksum }).ToArray();
        return frame;
    }

    private static void WriteTest(BinaryWriter writer)
    {
        var enc = Encoding.GetEncoding(1251);
        WriteFixedString(writer, "DA 0.01", 100, enc);
        writer.Write((byte)128);
        writer.Write((byte)5);
        writer.Write((byte)2);
        writer.Write((byte)12);
        writer.Write((byte)1);
        writer.Write((byte)5);
        WriteInt16BigEndian(writer, -50);
        WriteInt16BigEndian(writer, 150);
        writer.Write((byte)2);
        writer.Write((byte)10);
        WriteInt16BigEndian(writer, -100);
        WriteInt16BigEndian(writer, 180);

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

        writer.Write((byte)1); // a wave exists
        writer.Write((byte)10);
        writer.Write((byte)20);
        writer.Write(50u);
        writer.Write(350u);
        writer.Write((byte)40);
        writer.Write((byte)80);
        writer.Write(20u);
        writer.Write(250u);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write(0);

        WriteEye(writer, flat: false);
        WriteEye(writer, flat: true);
    }

    private static void WriteInt16BigEndian(BinaryWriter writer, short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        writer.Write(buffer);
    }

    private static void WriteEye(BinaryWriter writer, bool flat)
    {
        writer.Write((byte)(flat ? 1 : 0));
        writer.Write((byte)(flat ? 255 : 3));
        writer.Write((byte)(flat ? 255 : 2));

        var aMsBytes = new byte[6];
        var bMsBytes = new byte[6];
        var aMkV = new uint[6];
        var bMkV = new uint[6];

        if (flat)
        {
            for (int i = 0; i < 3; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(aMsBytes.AsSpan(i * 2, 2), ushort.MaxValue);
                BinaryPrimitives.WriteUInt16LittleEndian(bMsBytes.AsSpan(i * 2, 2), ushort.MaxValue);
            }

            for (int i = 0; i < 6; i++)
            {
                aMkV[i] = uint.MaxValue;
                bMkV[i] = uint.MaxValue;
            }
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(aMsBytes.AsSpan(0, 2), 15);
            BinaryPrimitives.WriteUInt16LittleEndian(aMsBytes.AsSpan(2, 2), 16);
            BinaryPrimitives.WriteUInt16LittleEndian(aMsBytes.AsSpan(4, 2), ushort.MaxValue);

            BinaryPrimitives.WriteUInt16LittleEndian(bMsBytes.AsSpan(0, 2), 55);
            BinaryPrimitives.WriteUInt16LittleEndian(bMsBytes.AsSpan(2, 2), 57);
            BinaryPrimitives.WriteUInt16LittleEndian(bMsBytes.AsSpan(4, 2), ushort.MaxValue);

            aMkV[0] = 120;
            aMkV[1] = 110;
            for (int i = 2; i < 6; i++) aMkV[i] = 0x0000FFFF;

            bMkV[0] = 200;
            bMkV[1] = 190;
            for (int i = 2; i < 6; i++) bMkV[i] = uint.MaxValue;
        }

        writer.Write(aMsBytes);
        foreach (var value in aMkV) writer.Write(value);
        writer.Write(bMsBytes);
        foreach (var value in bMkV) writer.Write(value);

        writer.Write((byte)(flat ? 255 : 14));
        writer.Write((byte)(flat ? 255 : 56));
        writer.Write((byte)(flat ? 0 : 1));

        for (int graph = 0; graph < 6; graph++)
        {
            for (int point = 0; point < 128; point++)
            {
                short sample = flat ? (short)0 : (short)(graph == 0 ? point - 64 : 0);
                writer.Write(sample);
            }
        }

        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write(0);
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
        Assert.NotNull(test.RightEye.Graphs);
        Assert.Single(test.RightEye.Graphs!);
        Assert.Equal(128, test.RightEye.Graphs![0].Length);
        Assert.Equal(-32.0, test.RightEye.Graphs![0][0], 5);
        Assert.Equal(31.5, test.RightEye.Graphs![0][127], 5);

        Assert.Equal<byte>(0, test.LeftEye.GraphCount);
        Assert.Null(test.LeftEye.Graphs);
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
