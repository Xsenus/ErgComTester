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

            var tests = new List<ErgTest>(Math.Max((byte)1, totalTests));
            var warnings = new List<string>();

            for (int i = 0; i < totalTests; i++)
            {
                if (reader.Remaining <= 0)
                {
                    warnings.Add($"Данные завершились до получения теста #{i + 1}.");
                    break;
                }

                var localReader = reader;
                try
                {
                    var test = ReadTest(ref localReader, i);
                    tests.Add(test);
                    reader = localReader;
                }
                catch (InvalidDataException ex) when (IsUnexpectedEnd(ex))
                {
                    warnings.Add($"Данные теста #{i + 1} обрезаны: {ex.Message}.");
                    break;
                }
            }

            if (tests.Count < totalTests)
            {
                warnings.Add($"Прибор заявил тестов: {totalTests}, распознано: {tests.Count}.");
            }

            var descriptionLength = Math.Min(500, reader.Remaining);
            if (descriptionLength < 500)
            {
                warnings.Add($"Длина текстового описания {descriptionLength} байт вместо ожидаемых 500.");
            }

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
                ChecksumValid = checksumValid,
                Warnings = warnings
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
        var graphXScaleMin = reader.ReadInt16BigEndian();
        var graphXScaleMax = reader.ReadInt16BigEndian();
        var graphYValueStep = reader.ReadByte();
        var graphYLineStep = reader.ReadByte();
        var graphYScaleMin = reader.ReadInt16BigEndian();
        var graphYScaleMax = reader.ReadInt16BigEndian();

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
        var aMsNormalMinRaw = reader.ReadByte();
        var aMsNormalMaxRaw = reader.ReadByte();
        var aMkVNormalMinRaw = reader.ReadUInt16BigEndian();
        var aMkVNormalMaxRaw = reader.ReadUInt16BigEndian();
        var bMsNormalMinRaw = reader.ReadByte();
        var bMsNormalMaxRaw = reader.ReadByte();
        var bMkVNormalMinRaw = reader.ReadUInt16BigEndian();
        var bMkVNormalMaxRaw = reader.ReadUInt16BigEndian();
        var rezerv1 = reader.ReadByte();
        var rezerv2 = reader.ReadByte();
        var rezerv3 = reader.ReadInt16BigEndian();

        var rightEye = ReadEye(ref reader, graphNumPoints, graphDiscr, aWaveExists);
        var leftEye = ReadEye(ref reader, graphNumPoints, graphDiscr, aWaveExists);

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
            AWaveMsNormalMin = aWaveExists ? NormalizeByte(aMsNormalMinRaw) : null,
            AWaveMsNormalMax = aWaveExists ? NormalizeByte(aMsNormalMaxRaw) : null,
            AWaveMkVNormalMin = aWaveExists ? NormalizeAmplitude(aMkVNormalMinRaw) : null,
            AWaveMkVNormalMax = aWaveExists ? NormalizeAmplitude(aMkVNormalMaxRaw) : null,
            BWaveMsNormalMin = NormalizeByte(bMsNormalMinRaw),
            BWaveMsNormalMax = NormalizeByte(bMsNormalMaxRaw),
            BWaveMkVNormalMin = NormalizeAmplitude(bMkVNormalMinRaw),
            BWaveMkVNormalMax = NormalizeAmplitude(bMkVNormalMaxRaw),
            Rezerv1 = rezerv1,
            Rezerv2 = rezerv2,
            Rezerv3 = rezerv3,
            RightEye = rightEye,
            LeftEye = leftEye
        };
    }

    private static EyeData ReadEye(ref SpanReader reader, int graphNumPoints, byte graphDiscrPerMkV, bool aWaveExists)
    {
        var isFlat = reader.ReadByte() != 0;
        var qualityRaw = reader.ReadByte();
        var valueCountRaw = reader.ReadByte();

        var aMsBytes = reader.ReadBytes(6);
        var aMkV = new uint?[6];
        for (int i = 0; i < aMkV.Length; i++)
            aMkV[i] = NormalizeAmplitude(reader.ReadUInt16BigEndian());

        var bMsBytes = reader.ReadBytes(6);
        var bMkV = new uint?[6];
        for (int i = 0; i < bMkV.Length; i++)
            bMkV[i] = NormalizeAmplitude(reader.ReadUInt16BigEndian());

        var aMarkerRaw = reader.ReadByte();
        var bMarkerRaw = reader.ReadByte();
        var graphCountRaw = reader.ReadByte();

        var allGraphs = ReadGraphs(ref reader, graphNumPoints, graphDiscrPerMkV);

        var rezerv1 = reader.ReadByte();
        var rezerv2 = reader.ReadByte();
        var rezerv3 = reader.ReadInt16BigEndian();

        var graphCount = ClampGraphCount(graphCountRaw, graphNumPoints);
        var graphs = graphCount == 0 ? null : allGraphs[..graphCount];

        return new EyeData
        {
            IsFlat = isFlat,
            QualityIndex = NormalizeByte(qualityRaw),
            ValueCount = NormalizeByte(valueCountRaw),
            AWaveMs = aWaveExists ? ParseMarkers(aMsBytes) : null,
            AWaveMkV = aWaveExists ? aMkV : null,
            BWaveMs = ParseMarkers(bMsBytes),
            BWaveMkV = bMkV,
            AWaveMarker = NormalizeByte(aMarkerRaw),
            BWaveMarker = NormalizeByte(bMarkerRaw),
            GraphCount = graphCount,
            Graphs = graphs,
            Rezerv1 = rezerv1,
            Rezerv2 = rezerv2,
            Rezerv3 = rezerv3
        };
    }

    private static ushort?[] ParseMarkers(ReadOnlySpan<byte> data)
    {
        const int markerCount = 3;
        var result = new ushort?[markerCount];
        for (int i = 0; i < markerCount; i++)
        {
            int offset = i * 2;
            if (offset + 1 >= data.Length)
            {
                result[i] = null;
                continue;
            }

            ushort raw = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            result[i] = raw == ushort.MaxValue ? null : raw;
        }

        return result;
    }

    private static double[][] ReadGraphs(ref SpanReader reader, int graphNumPoints, byte graphDiscrPerMkV)
    {
        const int graphSlots = 6;
        const int graphCapacity = 128;
        var divisor = graphDiscrPerMkV == 0 ? 1.0 : graphDiscrPerMkV;
        var result = new double[graphSlots][];
        int samplesToExpose = Math.Clamp(graphNumPoints, 0, graphCapacity);

        for (int graph = 0; graph < graphSlots; graph++)
        {
            var rawSamples = new short[graphCapacity];
            for (int point = 0; point < graphCapacity; point++)
            {
                rawSamples[point] = reader.ReadInt16LittleEndian();
            }

            if (samplesToExpose <= 0)
            {
                result[graph] = Array.Empty<double>();
                continue;
            }

            var converted = new double[samplesToExpose];
            for (int point = 0; point < samplesToExpose; point++)
            {
                converted[point] = rawSamples[point] / divisor;
            }

            result[graph] = converted;
        }

        return result;
    }

    private static byte ClampGraphCount(byte raw, int graphNumPoints)
    {
        if (graphNumPoints <= 0)
            return 0;
        if (raw == byte.MaxValue)
            return 0;
        return Math.Clamp(raw, (byte)0, (byte)6);
    }

    private static byte? NormalizeByte(byte value)
        => value == byte.MaxValue ? null : value;

    private static uint? NormalizeAmplitude(ushort value)
    {
        if (value == ushort.MaxValue)
            return null;
        return value;
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

    private static bool IsUnexpectedEnd(Exception ex)
        => ex is InvalidDataException ide
           && ide.Message.IndexOf("Unexpected end of data", StringComparison.OrdinalIgnoreCase) >= 0;

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

        public ushort ReadUInt16BigEndian()
            => BinaryPrimitives.ReadUInt16BigEndian(ReadBytes(2));

        public short ReadInt16BigEndian()
            => BinaryPrimitives.ReadInt16BigEndian(ReadBytes(2));

        public short ReadInt16LittleEndian()
            => BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(2));

        public int ReadInt32LittleEndian()
            => BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));

        public uint ReadUInt32LittleEndian()
            => BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(4));

        public uint ReadUInt32BigEndian()
            => BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(4));
    }
}
