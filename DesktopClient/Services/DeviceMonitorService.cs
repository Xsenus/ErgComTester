using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MicroluxErgConnect.Models;

namespace MicroluxErgConnect.Services;

public sealed class DeviceMonitorService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly LogService _log;
    private readonly ReportGenerationService _reports;
    private readonly object _stateLock = new();

    private CancellationTokenSource? _cts;
    private Task? _monitorTask;
    private DeviceConnectionInfo? _currentConnection;
    private DeviceStatus _status = new(false, null, null, DateTime.MinValue, "Ожидание запуска");

    public event EventHandler<DeviceStatus>? StatusChanged;
    public event EventHandler<DeviceConnectionInfo>? DeviceConnected;
    public event EventHandler? DeviceDisconnected;

    public DeviceMonitorService(SettingsService settings, LogService log, ReportGenerationService reports)
    {
        _settings = settings;
        _log = log;
        _reports = reports;
    }

    public DeviceStatus CurrentStatus
    {
        get
        {
            lock (_stateLock) return _status;
        }
    }

    public void Start()
    {
        if (_monitorTask != null)
        {
            _log.Debug("Мониторинг уже запущен, повторный старт проигнорирован.");
            return;
        }
        _log.Info("Инициализация фонового мониторинга COM-портов.");
        _cts = new CancellationTokenSource();
        _monitorTask = Task.Run(() => MonitorLoopAsync(_cts.Token));
    }

    private async Task MonitorLoopAsync(CancellationToken ct)
    {
        _log.Info("Мониторинг COM-портов запущен.");
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_currentConnection == null)
                {
                    await ScanAsync(ct);
                    await Task.Delay(_settings.Current.DeviceScanInterval, ct);
                }
                else
                {
                    _log.Debug($"[{_currentConnection.PortName}] проверка активного подключения...");
                    var stillAlive = await VerifyAsync(_currentConnection, ct);
                    if (!stillAlive)
                    {
                        _log.Warn($"Связь с устройством {_currentConnection.PortName} потеряна.");
                        lock (_stateLock)
                        {
                            _currentConnection = null;
                            UpdateStatusLocked(new DeviceStatus(false, null, null, DateTime.Now, "Поиск устройства"));
                        }
                        DeviceDisconnected?.Invoke(this, EventArgs.Empty);
                        _reports.OnDeviceDisconnected();
                        await Task.Delay(_settings.Current.DeviceReconnectDelay, ct);
                    }
                    else
                    {
                        await Task.Delay(_settings.Current.DeviceReconnectDelay, ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.Error($"Ошибка фонового мониторинга: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private async Task ScanAsync(CancellationToken ct)
    {
        var ports = SerialPort.GetPortNames().OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        if (ports.Count == 0)
        {
            UpdateStatus(new DeviceStatus(false, null, null, DateTime.Now, "COM-порты не найдены"));
            _log.Warn("В системе нет доступных COM-портов.");
            return;
        }

        var preferred = _settings.Current.PreferredPort;
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            var idx = ports.FindIndex(p => string.Equals(p, preferred, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                var preferredPort = ports[idx];
                ports.RemoveAt(idx);
                ports.Insert(0, preferredPort);
                _log.Info($"Приоритетный порт {preferredPort} будет проверен первым.");
            }
        }

        UpdateStatus(new DeviceStatus(false, null, null, DateTime.Now, "Сканирование портов"));
        _log.Info($"Сканирование {ports.Count} портов...");

        var tasks = new List<Task<DeviceConnectionInfo?>>();
        foreach (var port in ports)
        {
            tasks.Add(Task.Run(() => ProbePort(port, ct), ct));
        }

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);
            DeviceConnectionInfo? info;
            try
            {
                info = await completed;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _log.Debug($"[{DateTime.Now:HH:mm:ss}] ошибка при сканировании: {ex.Message}");
                continue;
            }

            if (info != null)
            {
                HandleConnected(info);
                return;
            }
        }

        UpdateStatus(new DeviceStatus(false, null, null, DateTime.Now, "Устройство не найдено"));
    }

    private DeviceConnectionInfo? ProbePort(string portName, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using var sp = SerialPortUtility.CreatePort(portName, _settings.Current.Serial);
            sp.Open();
            SerialPortUtility.ToggleLinesIfNeeded(sp, _settings.Current.Serial, _log);
            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();
            var ping = ErgProtocol.BuildPing();
            sp.Write(ping, 0, ping.Length);
            _log.Debug($"[{portName}] отправлен PING (0xE0)");
            var reply = ReadChunk(sp, _settings.Current.Serial.MinCommonInfoSize);
            if (reply.Length == 0)
            {
                _log.Debug($"[{portName}] нет ответа.");
                return null;
            }
            _log.HexDump($"[{portName}] COMMON_INFO", reply);
            if (!ErgProtocol.ValidateChecksum(reply))
            {
                _log.Warn($"[{portName}] некорректная контрольная сумма COMMON_INFO.");
                return null;
            }
            if (!ErgParser.TryParseCommonInfo(reply, out var info, out var err))
            {
                _log.Warn($"[{portName}] не удалось распознать COMMON_INFO: {err}");
                return null;
            }
            _log.Info($"Прибор '{info.DeviceName}' обнаружен на порту {portName}.");
            return new DeviceConnectionInfo(portName, info, DateTime.Now, reply);
        }
        catch (TimeoutException)
        {
            _log.Debug($"[{portName}] таймаут ответа.");
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.Debug($"[{portName}] ошибка при проверке: {ex.Message}");
            return null;
        }
    }

    private byte[] ReadChunk(SerialPort sp, int minExpected)
    {
        var start = Environment.TickCount;
        var lastData = start;
        using var ms = new System.IO.MemoryStream();
        var buffer = new byte[4096];
        while (Environment.TickCount - start < _settings.Current.Serial.MaxReadWindowMs)
        {
            var toRead = Math.Min(buffer.Length, sp.BytesToRead);
            if (toRead > 0)
            {
                var read = sp.Read(buffer, 0, toRead);
                if (read > 0)
                {
                    ms.Write(buffer, 0, read);
                    lastData = Environment.TickCount;
                }
            }
            else
            {
                if (ms.Length >= minExpected && Environment.TickCount - lastData > _settings.Current.Serial.QuietTimeMs)
                {
                    break;
                }
                Thread.Sleep(5);
            }
        }
        var elapsed = Environment.TickCount - start;
        _log.Debug($"[{sp.PortName}] прием завершен: {ms.Length} байт за {elapsed} мс.");
        return ms.ToArray();
    }

    private async Task<bool> VerifyAsync(DeviceConnectionInfo connection, CancellationToken ct)
    {
        try
        {
            _log.Debug($"[{connection.PortName}] повторное сканирование для проверки связи.");
            var info = await Task.Run(() => ProbePort(connection.PortName, ct), ct);
            if (info == null) return false;
            _log.Debug($"[{connection.PortName}] устройство ответило корректно при повторной проверке.");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.Warn($"Ошибка проверки подключения: {ex.Message}");
            return false;
        }
    }

    private void HandleConnected(DeviceConnectionInfo info)
    {
        lock (_stateLock)
        {
            _currentConnection = info;
            UpdateStatusLocked(new DeviceStatus(true, info.PortName, info.DeviceInfo, DateTime.Now, "Устройство подключено"));
        }
        _log.Info($"Устройство сохранено как предпочитаемое: {info.PortName}.");
        _ = _settings.UpdateAsync(s => s.PreferredPort = info.PortName);
        DeviceConnected?.Invoke(this, info);
        _reports.OnDeviceConnected(info);
    }

    private void UpdateStatus(DeviceStatus status)
    {
        lock (_stateLock)
        {
            UpdateStatusLocked(status);
        }
    }

    private void UpdateStatusLocked(DeviceStatus status)
    {
        _status = status;
        StatusChanged?.Invoke(this, status);
    }

    public void Dispose()
    {
        _log.Info("Остановка фонового мониторинга COM-портов.");
        _cts?.Cancel();
        try
        {
            _monitorTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException) { }
        _cts?.Dispose();
    }
}
