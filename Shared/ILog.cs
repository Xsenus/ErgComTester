namespace MicroluxErgConnect;

public interface ILog
{
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Section(string title);
    void HexDump(string caption, byte[] data, int width = 16);
}
