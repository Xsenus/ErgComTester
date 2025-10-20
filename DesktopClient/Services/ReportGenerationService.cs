using System.Buffers;
using System.Globalization;
using System.IO.Ports;
using ErgData;
using MicroluxErgConnect.Models;

namespace MicroluxErgConnect.Services;

public sealed class ReportGenerationService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly ILog _log;
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private string? _activePort;
    private DeviceConnectionInfo? _lastDeviceInfo;

    public event EventHandler<string>? ReportGenerated;
    public event EventHandler<string>? SyncStateChanged;

    static ReportGenerationService()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
    }

    public ReportGenerationService(SettingsService settings, ILog log)
    {
        _settings = settings;
        _log = log;
    }

    public void OnDeviceConnected(DeviceConnectionInfo info)
    {
        lock (_sync)
        {
            _lastDeviceInfo = info;
            _activePort = info.PortName;
            _log.Info($"[{info.PortName}] устройство подключено, запуск синхронизации данных.");
            RestartLoop();
        }
    }

    public void OnDeviceDisconnected()
    {
        lock (_sync)
        {
            _log.Info("Устройство отключено, фоновые задачи синхронизации остановлены.");
            _activePort = null;
            _lastDeviceInfo = null;
            CancelLoop();
        }
    }

    private void RestartLoop()
    {
        CancelLoop();
        if (string.IsNullOrWhiteSpace(_activePort)) return;
        _log.Debug($"[{_activePort}] запуск нового цикла синхронизации пациентов.");
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => SyncLoopAsync(_activePort!, _cts.Token));
    }

    private async Task SyncLoopAsync(string portName, CancellationToken ct)
    {
        _log.Info($"[{portName}] старт фоновой синхронизации пациентов.");
        SyncStateChanged?.Invoke(this, $"Подключено к {portName}. Синхронизация данных...");
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await SyncOnceAsync(portName, ct);
                SyncStateChanged?.Invoke(this, $"Ожидание {_settings.Current.BackgroundSyncInterval.TotalMinutes:F0} мин. до следующей проверки");
                await Task.Delay(_settings.Current.BackgroundSyncInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.Warn($"Ошибка синхронизации: {ex.Message}");
                SyncStateChanged?.Invoke(this, "Ошибка синхронизации, повтор через 1 минуту");
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }
        }
    }

    private async Task SyncOnceAsync(string portName, CancellationToken ct)
    {
        var options = _settings.Current.Serial;
        Directory.CreateDirectory(_settings.Current.ReportsDirectory);
        using var port = SerialPortUtility.CreatePort(portName, options);
        port.Open();
        SerialPortUtility.ToggleLinesIfNeeded(port, options, _log);
        port.DiscardInBuffer();
        port.DiscardOutBuffer();

        _log.Info($"[{portName}] запрос данных пациентов");
        var patients = new List<(ErgPatient info, byte[] raw)>();
        int maxPatients = Math.Max(1, _lastDeviceInfo?.DeviceInfo.TotalNumId ?? 1);

        for (int index = 1; index <= maxPatients; index++)
        {
            ct.ThrowIfCancellationRequested();
            var cmd = ErgProtocol.BuildGetNext();
            port.Write(cmd, 0, cmd.Length);
            _log.Debug($"[{portName}] отправлен запрос 0xE5 (пациент #{index})");
            var block = ReadPatientBlock(port, options);
            if (block.Length == 0)
            {
                _log.Info("Передача пациентов завершена устройством.");
                break;
            }

            if (!ErgProtocol.ValidateChecksum(block))
            {
                _log.Warn($"[{portName}] контрольная сумма пациента #{index} не совпала, запрос повтор");
                var repeat = ErgProtocol.BuildRepeat();
                port.DiscardInBuffer();
                port.Write(repeat, 0, repeat.Length);
                block = ReadPatientBlock(port, options);
                if (block.Length == 0 || !ErgProtocol.ValidateChecksum(block))
                {
                    _log.Error($"[{portName}] не удалось получить корректные данные пациента #{index}");
                    continue;
                }
            }

            if (!ErgParser.TryParsePatientBlock(block, out var patient, out var err))
            {
                _log.Warn($"[{portName}] получены данные пациента #{index}, но разбор завершился ошибкой: {err}");
            }
            else
            {
                _log.Info($"Получен пациент #{index}: ID={patient.PatientId}, животное={DescribeAnimal(patient.Animal)}, тестов={patient.Tests.Count}/{patient.TotalNumTests}");
                patients.Add((patient, block));
            }

            SendContinueAck(port, portName, $"подтверждение получения пациента #{index}");
        }

        if (patients.Count == 0)
        {
            _log.Info("Новых данных пациентов не обнаружено.");
        }
        else
        {
            GenerateReports(patients);
            _log.Info($"Создано {patients.Count} отчет(ов).");
        }

        if (options.EnableRtcSynchronization)
        {
            var rtc = ErgProtocol.BuildRtcSet(DateTime.Now);
            port.Write(rtc, 0, rtc.Length);
            _log.Info("Часы прибора синхронизированы.");
        }
    }

    private void SendContinueAck(SerialPort port, string portName, string context)
    {
        var ack = new byte[] { 0xE3 };
        port.Write(ack, 0, ack.Length);
        _log.Debug($"[{portName}] отправлено подтверждение 0xE3 ({context}).");
    }

    private byte[] ReadPatientBlock(SerialPort port, SerialCommunicationOptions options)
    {
        const int ContinueAckBlockSize = 2048;

        long start = Environment.TickCount64;
        long lastData = start;
        long nextAckThreshold = ContinueAckBlockSize;
        using var ms = new MemoryStream(4096);
        var buffer = ArrayPool<byte>.Shared.Rent(1024);

        try
        {
            while (Environment.TickCount64 - lastData < options.MaxReadWindowMs)
            {
                try
                {
                    var read = port.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        ms.Write(buffer, 0, read);
                        lastData = Environment.TickCount64;

                        var totalBytes = ms.Length;
                        while (totalBytes >= nextAckThreshold)
                        {
                            SendContinueAck(port, port.PortName, $"накоплено {totalBytes} байт");
                            nextAckThreshold += ContinueAckBlockSize;
                        }

                        continue;
                    }
                }
                catch (TimeoutException)
                {
                    if (ms.Length == 0)
                    {
                        if (Environment.TickCount64 - start >= options.MaxReadWindowMs)
                        {
                            break;
                        }

                        continue;
                    }
                }

                if (ms.Length == 0)
                {
                    continue;
                }

                if (Environment.TickCount64 - lastData < options.QuietTimeMs)
                {
                    continue;
                }

                if (!ms.TryGetBuffer(out var segment))
                {
                    break;
                }

                var span = segment.AsSpan(0, (int)ms.Length);
                if (span.Length >= options.MinPatientBlockSize && ErgProtocol.ValidateChecksum(span))
                {
                    break;
                }

                // Данных пока недостаточно — продолжим ожидание, если не вышли за окно чтения.
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        long stop = Environment.TickCount64;
        bool readWindowElapsed = stop - lastData >= options.MaxReadWindowMs;

        if (ms.Length == 0)
        {
            var elapsedEmpty = (int)(stop - start);
            _log.Debug($"[{port.PortName}] прием блока пациента завершен: 0 байт за {elapsedEmpty} мс (данных нет).");
            return Array.Empty<byte>();
        }

        var data = ms.ToArray();
        var elapsed = (int)(stop - start);
        var checksumValid = ErgProtocol.ValidateChecksum(data);
        var checksumStatus = checksumValid ? "контрольная сумма подтверждена" : "контрольная сумма НЕ совпала";
        _log.Debug($"[{port.PortName}] прием блока пациента завершен: {data.Length} байт за {elapsed} мс ({checksumStatus}).");

        if (!checksumValid && readWindowElapsed)
        {
            var idle = (int)(stop - lastData);
            _log.Debug($"[{port.PortName}] окно чтения истекло после простоя {idle} мс, получено {data.Length} байт (вероятно неполный блок).");
        }

        return data;
    }

    private void GenerateReports(List<(ErgPatient info, byte[] raw)> patients)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var sessionDir = Path.Combine(_settings.Current.ReportsDirectory, timestamp);
        Directory.CreateDirectory(sessionDir);
        _log.Info($"Отчеты будут сохранены в каталоге {sessionDir}.");

        for (int i = 0; i < patients.Count; i++)
        {
            var patient = patients[i];
            var rawPath = Path.Combine(sessionDir, $"patient_{i + 1:000}.bin");
            File.WriteAllBytes(rawPath, patient.raw);
            _log.Debug($"Сырые данные пациента #{i + 1} сохранены: {rawPath}");

            var jsonPath = Path.Combine(sessionDir, $"patient_{i + 1:000}.json");
            ErgDataSerializer.SaveJson(jsonPath, patient.info);
            _log.Debug($"Структурированные данные пациента #{i + 1} сохранены: {jsonPath}");

            var pdfPath = Path.Combine(sessionDir, $"patient_{i + 1:000}.pdf");
            ErgReportBuilder.BuildPatientReport(patient.info, pdfPath, _lastDeviceInfo?.DeviceInfo, clinicName: null, rawFilePath: rawPath);
            _log.Info($"PDF-отчет для пациента #{i + 1} создан: {pdfPath}");
            ReportGenerated?.Invoke(this, pdfPath);
        }

        try
        {
            System.Media.SystemSounds.Exclamation.Play();
        }
        catch
        {
            // ignore audio failures
        }
    }

    private static string DescribeAnimal(AnimalKind animal)
        => animal switch
        {
            AnimalKind.Cat => "Кошка",
            AnimalKind.Dog => "Собака",
            AnimalKind.Rabbit => "Кролик",
            AnimalKind.Horse => "Лошадь",
            AnimalKind.Other => "Прочие",
            _ => animal.ToString()
        };

    private void CancelLoop()
    {
        _cts?.Cancel();
        try
        {
            _loopTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException) { }
        _cts?.Dispose();
        _cts = null;
        _loopTask = null;
    }

    public void Dispose()
    {
        CancelLoop();
    }
}
