using System.Buffers;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using ErgData;
using MicroluxErgConnect.Models;

namespace MicroluxErgConnect.Services;

public sealed class ReportGenerationService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly ILog _log;
    private readonly TelegramNotificationService? _telegram;
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private string? _activePort;
    private DeviceConnectionInfo? _lastDeviceInfo;
    private bool _pdfGenerationEnabled;
    private string? _pdfGenerationIssue;
    private string? _lastPdfWarningMessage;

    public event EventHandler<string>? ReportGenerated;
    public event EventHandler<string>? SyncStateChanged;

    static ReportGenerationService()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
    }

    public ReportGenerationService(SettingsService settings, ILog log, TelegramNotificationService? telegram = null)
    {
        _settings = settings;
        _log = log;
        _telegram = telegram;
        _pdfGenerationEnabled = RenderingSupport.PdfSupported;
        _pdfGenerationIssue = RenderingSupport.PdfIssue;

        if (!string.IsNullOrWhiteSpace(RenderingSupport.LegacyRenderingNotice))
        {
            _log.Info(RenderingSupport.LegacyRenderingNotice);
        }
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
        string? sessionDir = null;
        var generatedPdfReports = new List<string>();
        var generatedWordReports = new List<string>();
        int maxPatients = Math.Max(1, _lastDeviceInfo?.DeviceInfo.TotalNumId ?? 1);
        bool sessionAnnounced = false;
        int processedPatients = 0;

        for (int index = 1; index <= maxPatients; index++)
        {
            ct.ThrowIfCancellationRequested();

            bool requestNextPatient = false;
            bool stopRequested = false;
            int attempt = 0;
            string? attemptPath = null;
            string? finalRawPath = null;

            while (!ct.IsCancellationRequested)
            {
                attempt++;
                byte[] command;
                string commandCaption;
                if (attempt == 1)
                {
                    command = ErgProtocol.BuildGetNext();
                    commandCaption = "0xE5";
                }
                else
                {
                    command = ErgProtocol.BuildRepeat();
                    commandCaption = "0xEA";
                }

                port.Write(command, 0, command.Length);
                _log.Debug($"[{portName}] отправлен запрос {commandCaption} (пациент #{index}, попытка {attempt})");

                var block = ReadPatientBlock(port, options);
                if (block.Length == 0)
                {
                    if (attempt == 1)
                    {
                        _log.Info("Передача пациентов завершена устройством.");
                        stopRequested = true;
                    }
                    else
                    {
                        _log.Warn($"[{portName}] повторная передача пациента #{index} вернула пустой блок.");
                        requestNextPatient = true;
                    }
                    break;
                }

                sessionDir ??= CreateSessionDirectory();
                if (!sessionAnnounced)
                {
                    _telegram?.NotifySessionStarted(portName, sessionDir, maxPatients);
                    sessionAnnounced = true;
                }
                attemptPath = SavePatientAttempt(sessionDir, index, attempt, block);

                var checksumValid = ErgProtocol.ValidateChecksum(block);
                if (!checksumValid)
                {
                    _log.Warn($"[{portName}] контрольная сумма пациента #{index} (попытка {attempt}) не совпала, данные сохранены: {attemptPath}");
                    if (attempt >= Math.Max(1, options.RetryCount))
                    {
                        finalRawPath = PromoteAttemptToFinal(sessionDir, index, attemptPath);
                        _log.Error($"[{portName}] не удалось получить корректные данные пациента #{index} после {attempt} попыток. Используйте сохранённый дамп: {finalRawPath}");
                        _telegram?.NotifyPatientChecksumFailed(index, finalRawPath, attempt);
                        requestNextPatient = true;
                        processedPatients++;
                        break;
                    }

                    port.DiscardInBuffer();
                    _log.Debug($"[{portName}] запрос повторной передачи пациента #{index} (попытка {attempt + 1}).");
                    continue;
                }

                finalRawPath = PromoteAttemptToFinal(sessionDir, index, attemptPath);
                if (!ErgParser.TryParsePatientBlock(block, out var patient, out var err))
                {
                    _log.Warn($"[{portName}] получены данные пациента #{index}, но разбор завершился ошибкой: {err}. Сырой дамп: {finalRawPath}");
                    _telegram?.NotifyPatientParseFailed(index, finalRawPath, err ?? "Неизвестная ошибка");
                    processedPatients++;
                }
                else
                {
                    _log.Info($"Получен пациент #{index}: ID={patient.PatientId}, животное={DescribeAnimal(patient.Animal)}, тестов={patient.Tests.Count}/{patient.TotalNumTests}");
                    LogPatientWarnings(patient, $"[{portName}] пациент #{index}");

                    var jsonPath = Path.Combine(sessionDir, $"patient_{index:000}.json");
                    ErgDataSerializer.SaveJson(jsonPath, patient);
                    _log.Debug($"Структурированные данные пациента #{index} сохранены: {jsonPath}");

                    string? pdfPath = null;
                    if (_pdfGenerationEnabled)
                    {
                        var candidatePdfPath = Path.Combine(sessionDir, $"patient_{index:000}.pdf");
                        try
                        {
                            ErgReportBuilder.BuildPatientReport(patient, candidatePdfPath, _lastDeviceInfo?.DeviceInfo, clinicName: null, rawFilePath: finalRawPath);
                            _log.Info($"PDF-отчет для пациента #{index} создан: {candidatePdfPath}");
                            pdfPath = candidatePdfPath;
                            ReportGenerated?.Invoke(this, pdfPath);
                            generatedPdfReports.Add(pdfPath);
                        }
                        catch (Exception ex)
                        {
                            var reason = $"Не удалось создать PDF-отчет: {ex.Message}";
                            _log.Error($"[{portName}] не удалось создать PDF-отчет для пациента #{index}: {ex}");
                            RenderingSupport.DisablePdf(reason);
                            _pdfGenerationEnabled = false;
                            _pdfGenerationIssue = RenderingSupport.PdfIssue ?? reason;
                            LogPdfGenerationDisabled(portName);
                        }
                    }
                    else
                    {
                        LogPdfGenerationDisabled(portName);
                    }

                    string? docxPath = null;
                    var candidateDocx = Path.Combine(sessionDir, $"patient_{index:000}.docx");
                    try
                    {
                        ErgReportBuilder.BuildPatientWordReport(patient, candidateDocx, _lastDeviceInfo?.DeviceInfo, clinicName: null, rawFilePath: finalRawPath);
                        _log.Info($"Word-отчет для пациента #{index} создан: {candidateDocx}");
                        docxPath = candidateDocx;
                        ReportGenerated?.Invoke(this, docxPath);
                        generatedWordReports.Add(docxPath);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"[{portName}] не удалось создать Word-отчет для пациента #{index}: {ex.Message}");
                        _telegram?.NotifyMessage($"⚠️ Не удалось создать Word-отчет пациента #{index:000}: {ex.Message}");
                    }

                    processedPatients++;
                    var pdfPathForNotification = pdfPath ?? "<не создан>";
                    var docxPathForNotification = docxPath ?? "<не создан>";
                    _telegram?.NotifyPatientProcessed(index, patient, finalRawPath, jsonPath, pdfPathForNotification, docxPathForNotification);
                }

                requestNextPatient = true;
                break;
            }

            if (stopRequested)
            {
                break;
            }

            if (requestNextPatient)
            {
                SendContinueAck(port, portName, $"подтверждение получения пациента #{index}");
            }
        }

        var totalReports = generatedPdfReports.Count + generatedWordReports.Count;
        if (totalReports == 0)
        {
            if (sessionDir != null)
            {
                _log.Warn($"Сырые данные пациентов сохранены в {sessionDir}, но обработка не выполнена.");
                _telegram?.NotifySessionCompleted(portName, sessionDir, processedPatients, totalReports);
            }
            else
            {
                _log.Info("Новых данных пациентов не обнаружено.");
                _telegram?.NotifyNoPatients(portName);
                _telegram?.NotifySessionCompleted(portName, null, processedPatients, totalReports);
            }
        }
        else
        {
            string summary;
            if (generatedPdfReports.Count > 0 && generatedWordReports.Count > 0)
            {
                summary = $"Создано {generatedPdfReports.Count} PDF и {generatedWordReports.Count} Word отчет(ов).";
            }
            else if (generatedPdfReports.Count > 0)
            {
                summary = $"Создано {generatedPdfReports.Count} PDF-отчет(ов).";
            }
            else
            {
                summary = $"Создано {generatedWordReports.Count} Word-отчет(ов).";
            }

            _log.Info(summary);
            TryPlayNotificationSound();
            _telegram?.NotifySessionCompleted(portName, sessionDir, processedPatients, totalReports);
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

    private string CreateSessionDirectory()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var sessionDir = Path.Combine(_settings.Current.ReportsDirectory, timestamp);
        Directory.CreateDirectory(sessionDir);
        _log.Info($"Отчеты будут сохранены в каталоге {sessionDir}.");
        return sessionDir;
    }

    private string SavePatientAttempt(string sessionDir, int patientIndex, int attempt, byte[] raw)
    {
        var fileName = $"patient_{patientIndex:000}_attempt{attempt:00}.bin";
        var path = Path.Combine(sessionDir, fileName);
        File.WriteAllBytes(path, raw);
        _log.Debug($"Сырые данные пациента #{patientIndex} (попытка {attempt}) сохранены: {path}");
        return path;
    }

    private string PromoteAttemptToFinal(string sessionDir, int patientIndex, string attemptPath)
    {
        var finalPath = Path.Combine(sessionDir, $"patient_{patientIndex:000}.bin");
        File.Copy(attemptPath, finalPath, overwrite: true);
        if (!string.Equals(finalPath, attemptPath, StringComparison.OrdinalIgnoreCase))
        {
            _log.Debug($"Финальные данные пациента #{patientIndex} сохранены: {finalPath}");
        }
        return finalPath;
    }

    private void LogPdfGenerationDisabled(string portName)
    {
        var reason = RenderingSupport.PdfIssue ?? _pdfGenerationIssue ?? "Генерация PDF отключена.";
        _pdfGenerationIssue = reason;
        if (string.Equals(reason, _lastPdfWarningMessage, StringComparison.Ordinal))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(portName))
        {
            _log.Warn($"[{portName}] {reason}");
        }
        else
        {
            _log.Warn(reason);
        }

        _telegram?.NotifyMessage($"⚠️ {reason}");
        _lastPdfWarningMessage = reason;
    }

    private static void TryPlayNotificationSound()
    {
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

    public async Task<ManualConversionResult> ConvertPatientFileAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь к файлу не задан.", nameof(filePath));

        var result = new ManualConversionResult { RawPath = filePath };

        if (!File.Exists(filePath))
        {
            var reason = "Файл не найден.";
            _log.Warn($"[{filePath}] {reason}");
            _telegram?.NotifyManualConversionFailed(filePath, reason);
            return result with { ErrorMessage = reason };
        }

        _log.Info($"Запущено ручное преобразование файла пациента: {filePath}");
        _telegram?.NotifyManualConversionStarted(filePath);

        byte[] data;
        try
        {
            data = await File.ReadAllBytesAsync(filePath, ct);
        }
        catch (OperationCanceledException)
        {
            _log.Warn($"[{filePath}] Ручное преобразование отменено пользователем.");
            throw;
        }
        catch (Exception ex)
        {
            var reason = $"Не удалось прочитать файл: {ex.Message}";
            _log.Error($"[{filePath}] {reason}");
            _telegram?.NotifyManualConversionFailed(filePath, reason);
            return result with { ErrorMessage = reason };
        }

        _log.Debug($"[{Path.GetFileName(filePath)}] считано {data.Length} байт.");

        if (!ErgProtocol.ValidateChecksum(data))
        {
            var reason = "Контрольная сумма файла не совпадает.";
            _log.Warn($"[{filePath}] {reason}");
            _telegram?.NotifyManualConversionFailed(filePath, reason);
            return result with { ErrorMessage = reason };
        }

        if (!ErgParser.TryParsePatientBlock(data, out var patient, out var parseError))
        {
            var reason = parseError ?? "Неизвестная ошибка разбора.";
            _log.Warn($"[{filePath}] {reason}");
            _telegram?.NotifyManualConversionFailed(filePath, reason);
            return result with { ErrorMessage = reason };
        }

        LogPatientWarnings(patient, $"[{Path.GetFileName(filePath)}]");

        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }
        Directory.CreateDirectory(directory);

        var baseName = Path.GetFileNameWithoutExtension(filePath);
        var jsonPath = Path.Combine(directory, $"{baseName}.json");
        var pdfPath = Path.Combine(directory, $"{baseName}.pdf");
        var docxPath = Path.Combine(directory, $"{baseName}.docx");

        try
        {
            ErgDataSerializer.SaveJson(jsonPath, patient);
            _log.Info($"JSON сохранен: {jsonPath}");
        }
        catch (Exception ex)
        {
            var reason = $"Ошибка сохранения JSON: {ex.Message}";
            _log.Error($"[{filePath}] {reason}");
            _telegram?.NotifyManualConversionFailed(filePath, reason);
            return result with { ErrorMessage = reason };
        }

        try
        {
            ErgReportBuilder.BuildPatientReport(patient, pdfPath, _lastDeviceInfo?.DeviceInfo, clinicName: null, rawFilePath: filePath);
            _log.Info($"PDF-отчет сохранен: {pdfPath}");
        }
        catch (Exception ex)
        {
            var reason = $"Ошибка генерации PDF: {ex.Message}";
            _log.Error($"[{filePath}] {reason}");
            _telegram?.NotifyManualConversionFailed(filePath, reason);
            return result with { ErrorMessage = reason, JsonPath = jsonPath };
        }

        try
        {
            ErgReportBuilder.BuildPatientWordReport(patient, docxPath, _lastDeviceInfo?.DeviceInfo, clinicName: null, rawFilePath: filePath);
            _log.Info($"Word-отчет сохранен: {docxPath}");
        }
        catch (Exception ex)
        {
            var reason = $"Ошибка генерации Word: {ex.Message}";
            _log.Error($"[{filePath}] {reason}");
            try
            {
                if (File.Exists(pdfPath))
                {
                    File.Delete(pdfPath);
                }
            }
            catch (Exception cleanupEx)
            {
                _log.Warn($"[{pdfPath}] не удалось удалить PDF после ошибки Word: {cleanupEx.Message}");
            }
            _telegram?.NotifyManualConversionFailed(filePath, reason);
            return result with { ErrorMessage = reason, JsonPath = jsonPath };
        }

        _log.Info($"Ручное преобразование успешно завершено для {filePath}.");
        _telegram?.NotifyManualConversionSucceeded(filePath, patient, jsonPath, pdfPath, docxPath);
        return result with { Success = true, JsonPath = jsonPath, PdfPath = pdfPath, DocxPath = docxPath, Patient = patient };
    }

    private void LogPatientWarnings(ErgPatient patient, string context)
    {
        if (patient.Warnings is not { Count: > 0 })
        {
            return;
        }

        foreach (var warning in patient.Warnings)
        {
            _log.Warn($"{context}: {warning}");
        }
    }
}
