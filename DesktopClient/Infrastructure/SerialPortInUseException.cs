using System;

namespace MicroluxErgConnect.Infrastructure;

public sealed class SerialPortInUseException : Exception
{
    public SerialPortInUseException(string portName, Exception innerException)
        : base($"Порт {portName} занят другим процессом и временно недоступен.", innerException)
    {
        PortName = portName;
    }

    public string PortName { get; }
}
