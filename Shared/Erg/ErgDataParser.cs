using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ErgData;

public static class ErgDataParser
{
    private static readonly Encoding Cp1251;

    static ErgDataParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp1251 = Encoding.GetEncoding(1251);
    }

    public static bool TryParseCommonInfo(ReadOnlySpan<byte> frame, out CommonInfo info, out string? error)
    {
        info = new CommonInfo();
        error = null;

        try
        {
            if (frame.Length < 136)
                throw new InvalidDataException($"COMMON_INFO frame too short: {frame.Length} bytes");

            var checksum = frame[^1];
            var body = frame[..^1];
            var checksumValid = checksum == ComputeChecksum(body);

            var reader = new SpanReader(body);
            var reportName = ReadZString(ref reader, 64, Cp1251);
            var deviceName = ReadZString(ref reader, 64, Cp1251);
            var softwareRev = ReadZString(ref reader, 6, Encoding.ASCII);
            var total = reader.ReadByte();

            info = new CommonInfo
            {
                ReportName = reportName,
                DeviceName = deviceName,
                SoftwareRev = softwareRev,
                TotalNumId = total,
                Checksum = checksum,
                ChecksumValid = checksumValid
            };

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryParsePatient(ReadOnlySpan<byte> frame, out ErgPatient patient, out string? error)
    {
        patient = new ErgPatient();
        error = null;

        try
        {
            if (frame.Length < 64)
                throw new InvalidDataException($"Patient frame too short: {frame.Length} bytes");

            var checksum = frame[^1];
            var body = frame[..^1];
            var checksumValid = checksum == ComputeChecksum(body);

            var reader = new SpanReader(body);
            var patientId = reader.ReadUInt32BigEndian();
            var animalRaw = reader.ReadByte();
            var animal = Enum.IsDefined(typeof(AnimalKind), animalRaw) ? (AnimalKind)animalRaw : AnimalKind.Other;
            var testDate = ReadZString(ref reader, 18, Cp1251);
            var totalTests = reader.ReadByte();

            var tests = new List<ErgTest>(Math.Max(1, totalTests));
            for (int i = 0; i < totalTests; i++)
            {
                tests.Add(ReadTest(ref reader, i));
            }

            var descriptionLength = Math.Min(500, reader.Remaining);
            var description = descriptionLength > 0
                ? ReadZString(ref reader, descriptionLength, Cp1251)
                : string.Empty;

            if (reader.Remaining > 0)
            {
                // Skip possible padding bytes
                reader.Skip(reader.Remaining);
            }

            patient = new ErgPatient
            {
                PatientId = patientId,
                Animal = animal,
                TestDateTime = testDate,
                TotalNumTests = totalTests,
                Tests = tests,
                Description = description,
                Checksum = checksum,
                ChecksumValid = checksumValid
            };

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            patient = new ErgPatient();
            return false;
        }
    }

    private static ErgTest ReadTest(ref SpanReader reader, int index)
    {
        var testName = ReadZString(ref reader, 100, Cp1251);
        var graphNumPoints = reader.ReadByte();
        var graphDt = reader.ReadByte();
        var graphDiscr = reader.ReadByte();
        var graphFlashPosition = reader.ReadByte();
        var graphXValueStep = reader.ReadByte();
        var graphXLineStep = reader.ReadByte();
        var graphXScaleMin = reader.ReadInt32LittleEndian();
        var graphXScaleMax = reader.ReadInt32LittleEndian();
        var graphYValueStep = reader.ReadByte();
        var graphYLineStep = reader.ReadByte();
        var graphYScaleMin = reader.ReadInt32LittleEndian();
        var graphYScaleMax = reader.ReadInt32LittleEndian();

        var styles = new GraphStyle[6];
        for (int i = 0; i < styles.Length; i++)
        {
            var color = reader.ReadBytes(3);
            styles[i] = new GraphStyle
            {
                Index = i,
                Red = color[0],
                Green = color[1],
                Blue = color[2]
            };
        }

        var dotted = reader.ReadBytes(6);
        for (int i = 0; i < styles.Length && i < dotted.Length; i++)
        {
            styles[i] = styles[i] with { Dotted = dotted[i] != 0 };
        }

        var aWaveExists = reader.ReadByte() != 0;
        var aMsNormalMin = reader.ReadByte();
        var aMsNormalMax = reader.ReadByte();
        var aMkVNormalMin = reader.ReadUInt16LittleEndian();
        var aMkVNormalMax = reader.ReadUInt16LittleEndian();
        var bMsNormalMin = reader.ReadByte();
        var bMsNormalMax = reader.ReadByte();
        var bMkVNormalMin = reader.ReadUInt16LittleEndian();
        var bMkVNormalMax = reader.ReadUInt16LittleEndian();
        var rezerv1 = reader.ReadByte();
        var rezerv2 = reader.ReadByte();
        var rezerv3 = reader.ReadInt32LittleEndian();

        var rightEye = ReadEye(ref reader);
        var leftEye = ReadEye(ref reader);

        return new ErgTest
        {
            TestName = string.IsNullOrWhiteSpace(testName) ? $"Тест #{index + 1}" : testName,
            GraphNumPoints = graphNumPoints,
            GraphDt = graphDt,
            GraphDiscrPerMkV = graphDiscr,
            GraphFlashPosition = graphFlashPosition,
            GraphXValueStep = graphXValueStep,
            GraphXLineStep = graphXLineStep,
            GraphXScaleMin = graphXScaleMin,
            GraphXScaleMax = graphXScaleMax,
            GraphYValueStep = graphYValueStep,
            GraphYLineStep = graphYLineStep,
            GraphYScaleMin = graphYScaleMin,
            GraphYScaleMax = graphYScaleMax,
            GraphStyles = styles,
            AWaveExists = aWaveExists,
            AWaveMsNormalMin = aMsNormalMin,
            AWaveMsNormalMax = aMsNormalMax,
            AWaveMkVNormalMin = aMkVNormalMin,
            AWaveMkVNormalMax = aMkVNormalMax,
            BWaveMsNormalMin = bMsNormalMin,
            BWaveMsNormalMax = bMsNormalMax,
            BWaveMkVNormalMin = bMkVNormalMin,
            BWaveMkVNormalMax = bMkVNormalMax,
            Rezerv1 = rezerv1,
            Rezerv2 = rezerv2,
            Rezerv3 = rezerv3,
            RightEye = rightEye,
            LeftEye = leftEye
        };
    }

    private static EyeData ReadEye(ref SpanReader reader)
    {
        var isFlat = reader.ReadByte() != 0;
        var quality = reader.ReadByte();
        var valueCount = reader.ReadByte();
        var aMs = reader.ReadBytes(6).ToArray();
        var aMkV = new ushort[6];
        for (int i = 0; i < aMkV.Length; i++) aMkV[i] = reader.ReadUInt16LittleEndian();
        var bMs = reader.ReadBytes(6).ToArray();
        var bMkV = new ushort[6];
        for (int i = 0; i < bMkV.Length; i++) bMkV[i] = reader.ReadUInt16LittleEndian();
        var aMarker = reader.ReadByte();
        var bMarker = reader.ReadByte();
        var graphCount = reader.ReadByte();

        var graphs = new int[6][];
        for (int curve = 0; curve < graphs.Length; curve++)
        {
            graphs[curve] = new int[128];
            for (int point = 0; point < graphs[curve].Length; point++)
            {
                graphs[curve][point] = reader.ReadInt32LittleEndian();
            }
        }

        var rezerv1 = reader.ReadByte();
        var rezerv2 = reader.ReadByte();
        var rezerv3 = reader.ReadInt32LittleEndian();

        return new EyeData
        {
            IsFlat = isFlat,
            QualityIndex = quality,
            ValueCount = valueCount,
            AWaveMs = aMs,
            AWaveMkV = aMkV,
            BWaveMs = bMs,
            BWaveMkV = bMkV,
            AWaveMarker = aMarker,
            BWaveMarker = bMarker,
            GraphCount = graphCount,
            Graphs = graphs,
            Rezerv1 = rezerv1,
            Rezerv2 = rezerv2,
            Rezerv3 = rezerv3
        };
    }

    private static string ReadZString(ref SpanReader reader, int maxBytes, Encoding encoding)
    {
        var span = reader.ReadBytes(maxBytes);
        int len = span.IndexOf((byte)0);
        if (len < 0) len = span.Length;
        if (len == 0)
            return string.Empty;
        return encoding.GetString(span[..len]);
    }

    private static byte ComputeChecksum(ReadOnlySpan<byte> data)
    {
        int sum = 0;
        foreach (var b in data)
            sum += b;
        return (byte)(sum & 0xFF);
    }

    private ref struct SpanReader
    {
        private ReadOnlySpan<byte> _span;
        private int _offset;

        public SpanReader(ReadOnlySpan<byte> span)
        {
            _span = span;
            _offset = 0;
        }

        public int Remaining => _span.Length - _offset;

        public void Skip(int count)
        {
            if (count < 0 || count > Remaining)
                throw new InvalidDataException("Invalid skip length");
            _offset += count;
        }

        public ReadOnlySpan<byte> ReadBytes(int count)
        {
            if (count < 0 || count > Remaining)
                throw new InvalidDataException("Unexpected end of data");
            var slice = _span.Slice(_offset, count);
            _offset += count;
            return slice;
        }

        public byte ReadByte()
            => ReadBytes(1)[0];

        public ushort ReadUInt16LittleEndian()
            => BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(2));

        public int ReadInt32LittleEndian()
            => BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));

        public uint ReadUInt32BigEndian()
            => BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(4));
    }
}
