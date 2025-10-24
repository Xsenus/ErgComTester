using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MicroluxErgConnect.Infrastructure;
using MicroluxErgConnect.Models;
using MicroluxErgConnect;

namespace MicroluxErgConnect.Services;

public sealed class DeviceMonitorService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly ILog _log;
    private readonly ReportGenerationService _reports;
    private readonly object _stateLock = new();

    private CancellationTokenSource? _cts;
    private Task? _monitorTask;
    private DeviceConnectionInfo? _currentConnection;
    private DeviceStatus _status = new(false, null, null, DateTime.MinValue, "Ожидание запуска");

    public event EventHandler<DeviceStatus>? StatusChanged;
    public event EventHandler<DeviceConnectionInfo>? DeviceConnected;
    public event EventHandler? DeviceDisconnected;

    public DeviceMonitorService(SettingsService settings, ILog log, ReportGenerationService reports)
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
            tasks.Add(ProbeWithStartupStrategyAsync(port, ct));
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
            catch (SerialPortInUseException ex)
            {
                _log.Debug($"[{ex.PortName}] порт занят другим процессом, пропускаем.");
                continue;
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

    private DeviceConnectionInfo? ProbePort(string portName, CancellationToken ct, bool toggleLines = true)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var serial = _settings.Current.Serial;
            using var sp = SerialPortUtility.CreatePort(portName, serial);
            sp.Open();
            ct.ThrowIfCancellationRequested();
            if (toggleLines)
            {
                SerialPortUtility.ToggleLinesIfNeeded(sp, serial, _log);
                var warmup = serial.WarmupAfterToggle;
                if (warmup > TimeSpan.Zero)
                {
                    _log.Debug($"[{portName}] ожидание {warmup.TotalMilliseconds:F0} мс после переключения линий.");
                    WaitWithCancellation(warmup, ct);
                    ct.ThrowIfCancellationRequested();
                }
            }
            else
            {
                _log.Debug($"[{portName}] проверка без переключения линий DTR/RTS.");
                var passiveDelay = serial.PassiveProbeDelay;
                if (passiveDelay > TimeSpan.Zero)
                {
                    _log.Debug($"[{portName}] ожидание {passiveDelay.TotalMilliseconds:F0} мс перед пассивным опросом.");
                    WaitWithCancellation(passiveDelay, ct);
                    ct.ThrowIfCancellationRequested();
                }
            }
            ct.ThrowIfCancellationRequested();
            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();
            var ping = ErgProtocol.BuildPing();
            sp.Write(ping, 0, ping.Length);
            _log.Debug($"[{portName}] отправлен PING (0xE0)");
            ct.ThrowIfCancellationRequested();
            var reply = ReadChunk(sp, _settings.Current.Serial.MinCommonInfoSize);
            if (reply.Length == 0)
            {
                _log.Debug($"[{portName}] нет ответа.");
                return null;
            }
            ct.ThrowIfCancellationRequested();
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
        catch (UnauthorizedAccessException ex)
        {
            throw new SerialPortInUseException(portName, ex);
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
        var data = ErgIo.ReadChunk(
            sp,
            _log,
            minExpected,
            _settings.Current.Serial.QuietTimeMs,
            _settings.Current.Serial.MaxReadWindowMs);
        var elapsed = Environment.TickCount - start;
        _log.Debug($"[{sp.PortName}] прием завершен: {data.Length} байт за {elapsed} мс.");
        return data;
    }

    private static void WaitWithCancellation(TimeSpan delay, CancellationToken ct)
    {
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        if (ct.WaitHandle.WaitOne(delay))
        {
            ct.ThrowIfCancellationRequested();
        }
    }

    private async Task<DeviceConnectionInfo?> ProbeWithWatchdogAsync(string portName, CancellationToken ct, bool toggleLines)
    {
        var timeout = _settings.Current.Serial.ProbeTimeout;
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linkedCts.CancelAfter(timeout);

        var probeTask = Task.Run(() => ProbePort(portName, linkedCts.Token, toggleLines));
        var delayTask = Task.Delay(timeout, ct);
        var completed = await Task.WhenAny(probeTask, delayTask).ConfigureAwait(false);

        if (completed == probeTask)
        {
            try
            {
                return await probeTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                _log.Warn($"[{portName}] проверка порта отменена сторожевым таймером.");
                return null;
            }
        }

        if (ct.IsCancellationRequested)
        {
            linkedCts.Cancel();
            try
            {
                await probeTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            ct.ThrowIfCancellationRequested();
        }

        linkedCts.Cancel();
        _log.Warn($"[{portName}] проверка заняла больше {timeout.TotalMilliseconds:F0} мс и будет повторена после освобождения ресурсов.");

        try
        {
            await probeTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _log.Debug($"[{portName}] ошибка после срабатывания сторожевого таймера: {ex.Message}");
        }

        return null;
    }

    private async Task<DeviceConnectionInfo?> ProbeWithStartupStrategyAsync(string portName, CancellationToken ct)
    {
        var serial = _settings.Current.Serial;
        if (!serial.ToggleLinesOnOpen)
        {
            return await ProbeWithWatchdogAsync(portName, ct, toggleLines: false).ConfigureAwait(false);
        }

        try
        {
            var passive = await ProbeWithWatchdogAsync(portName, ct, toggleLines: false).ConfigureAwait(false);
            if (passive != null || ct.IsCancellationRequested)
            {
                return passive;
            }
        }
        catch (SerialPortInUseException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.Debug($"[{portName}] пассивная проверка завершилась ошибкой: {ex.Message}");
        }

        if (ct.IsCancellationRequested)
        {
            return null;
        }

        _log.Debug($"[{portName}] повторная попытка с переключением линий DTR/RTS.");
        return await ProbeWithWatchdogAsync(portName, ct, toggleLines: true).ConfigureAwait(false);
    }

    private async Task<bool> VerifyAsync(DeviceConnectionInfo connection, CancellationToken ct)
    {
        try
        {
            _log.Debug($"[{connection.PortName}] повторное сканирование для проверки связи.");
            var info = await ProbeWithWatchdogAsync(connection.PortName, ct, toggleLines: false).ConfigureAwait(false);
            if (info == null) return false;
            _log.Debug($"[{connection.PortName}] устройство ответило корректно при повторной проверке.");
            return true;
        }
        catch (SerialPortInUseException ex)
        {
            _log.Debug($"[{ex.PortName}] порт временно занят, предполагаем, что устройство всё ещё подключено.");
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
        _cts = null;
        _monitorTask = null;
    }
}
