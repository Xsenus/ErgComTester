using System;
using System.IO.Ports;
using System.Threading;
using MicroluxErgConnect;

namespace MicroluxErgConnect.Services;

public static class SerialPortUtility
{
    public static SerialPort CreatePort(string portName, SerialCommunicationOptions options)
        => new(portName, options.BaudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = options.ReadTimeoutMs,
            WriteTimeout = options.WriteTimeoutMs,
            DtrEnable = options.DtrEnable,
            RtsEnable = options.RtsEnable,
            Handshake = Handshake.None,
            NewLine = "\r\n"
        };

    public static void ToggleLinesIfNeeded(SerialPort port, SerialCommunicationOptions options, ILog log)
    {
        if (!options.ToggleLinesOnOpen) return;
        try
        {
            log.Debug($"[{port.PortName}] переключение линий DTR/RTS для инициализации.");
            port.DtrEnable = false;
            port.RtsEnable = false;
            Thread.Sleep(50);
            port.DtrEnable = options.DtrEnable;
            port.RtsEnable = options.RtsEnable;
            Thread.Sleep(50);
            log.Debug($"[{port.PortName}] линии установлены: DTR={port.DtrEnable}, RTS={port.RtsEnable}.");
        }
        catch (Exception ex)
        {
            log.Warn($"Не удалось переключить линии: {ex.Message}");
        }
    }
}
