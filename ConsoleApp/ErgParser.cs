using System.Text;

namespace ErgComTester;

internal record CommonInfo(string ReportName, string DeviceName, string SoftwareRev, int TotalNumId);
internal record PatientInfo(uint PatientId, byte AnimalType, string TestDateTime, byte TotalNumTests, string Description);

internal static class ErgParser
{
    public static bool TryParseCommonInfo(byte[] data, out CommonInfo info, out string? error)
    {
        info = new CommonInfo(string.Empty, string.Empty, string.Empty, 0); error = null;
        try
        {
            if (data.Length < 64 + 64 + 6 + 1 + 1) { error = "COMMON_INFO too short."; return false; }
            var report = ReadAsciiZ(data.AsSpan(0, 64));
            var device = ReadAsciiZ(data.AsSpan(64, 64));
            var swrev = ReadAsciiZ(data.AsSpan(128, 6));
            var total = data[134];
            info = new CommonInfo(report, device, swrev, total);
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    public static bool TryParsePatientBlock(byte[] data, out PatientInfo info, out string? error)
    {
        info = new PatientInfo(0, 0, string.Empty, 0, string.Empty); error = null;
        try
        {
            if (data.Length < 32) { error = "Patient block too short."; return false; }
            var body = data.AsSpan(0, data.Length - 1); // exclude KS

            uint patientId = ReadBeUInt32(body.Slice(0, 4));
            byte animal = body[4];
            string testDt = ReadAsciiZ(body.Slice(5, Math.Min(18, body.Length - 5)));
            byte tests = body.Length > 23 ? body[23] : (byte)0;

            string desc = string.Empty;
            int searchLen = Math.Min(500, body.Length);
            for (int i = body.Length - searchLen; i < body.Length - 1; i++)
            {
                if (i < 0) break;
                if (body[i] != 0)
                {
                    int start = i;
                    while (start > 0 && body[start - 1] != 0) start--;
                    int end = i;
                    while (end < body.Length && body[end] != 0) end++;
                    var segment = body.Slice(start, end - start);
                    if (LooksAscii(segment))
                    {
                        desc = Encoding.GetEncoding(1251).GetString(segment.ToArray());
                        break;
                    }
                    i = start;
                }
            }
            info = new PatientInfo(patientId, animal, testDt, tests, desc);
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    public static uint ReadBeUInt32(ReadOnlySpan<byte> s)
        => (uint)(s[0] << 24 | s[1] << 16 | s[2] << 8 | s[3]);

    public static string ReadAsciiZ(ReadOnlySpan<byte> s)
    {
        int len = 0;
        while (len < s.Length && s[len] != 0) len++;
        return Encoding.ASCII.GetString(s.Slice(0, len).ToArray());
    }

    private static bool LooksAscii(ReadOnlySpan<byte> s)
    {
        foreach (var b in s)
        {
            if (b == 0) return true;
            if (b < 9 || (b > 13 && b < 32) || b > 126) return false;
        }
        return true;
    }
}
