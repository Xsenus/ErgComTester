using System.Text;

namespace ErgComTester;

internal static class ErgProtocol
{
    public const byte CMD_GET = 0xE5;
    public const byte CMD_PING = 0xE0;
    public const byte CMD_REPEAT = 0xEA;

    public static byte[] BuildGetNext() => new byte[] { CMD_GET };

    public static byte[] BuildPing() => new byte[] { CMD_PING };
    public static byte[] BuildRepeat() => new byte[] { CMD_REPEAT };

    public static byte[] BuildRtcSet(DateTime dt)
    {
        var s = "DT" + dt.ToString("dd-MM-yyyy HH:mm");
        var bytes = Encoding.ASCII.GetBytes(s);
        var kc = LsbSum(bytes);
        var cmd = new byte[bytes.Length + 1];
        Buffer.BlockCopy(bytes, 0, cmd, 0, bytes.Length);
        cmd[^1] = kc;
        return cmd;
    }

    public static byte LsbSum(ReadOnlySpan<byte> data)
    {
        int sum = 0; foreach (var b in data) sum += b;
        return (byte)(sum & 0xFF);
    }

    public static bool ValidateChecksum(byte[] frame)
    {
        if (frame == null || frame.Length < 2) return false;
        var expected = frame[^1];
        var actual = LsbSum(frame.AsSpan(0, frame.Length - 1));
        return expected == actual;
    }
}
