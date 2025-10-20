using ErgData;

namespace MicroluxErgConnect;

public static class ErgParser
{
    public static bool TryParseCommonInfo(byte[] data, out CommonInfo info, out string? error)
        => ErgDataParser.TryParseCommonInfo(data, out info!, out error);

    public static bool TryParsePatientBlock(byte[] data, out ErgPatient info, out string? error)
        => ErgDataParser.TryParsePatient(data, out info!, out error);
}
