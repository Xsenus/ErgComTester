using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ErgData;
using MicroluxErgConnect.Models;
using MicroluxErgConnect.Utils;
using QuestPDF.Drawing;

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
    private int _activeSyncOperations;
    private static readonly object _pdfFontLock = new();
    private static bool _pdfFontsRegistered;
    private static bool _pdfFontRegistrationAttempted;

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
        EnsurePdfFontsRegistered();
        // ApplyRenderingMode(settings.Current.ReportRenderingMode);
        ApplyRenderingMode(ReportRenderingMode.Legacy);
    }

    public void ApplyRenderingMode(ReportRenderingMode mode)
    {
        _ = mode; // параметр сохраняется для совместимости обработчиков
        // RenderingSupport.Reload(mode);
        RenderingSupport.Reload(ReportRenderingMode.Legacy);
        _pdfGenerationEnabled = RenderingSupport.PdfSupported;
        _pdfGenerationIssue = RenderingSupport.PdfIssue;

        if (!string.IsNullOrWhiteSpace(RenderingSupport.LegacyRenderingNotice))
        {
            _log.Info(RenderingSupport.LegacyRenderingNotice);
        }
        else
        {
            _log.Info("Используется современный режим генерации отчетов.");
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

    public bool IsPortBusy(string portName)
    {
        lock (_sync)
        {
            return string.Equals(_activePort, portName, StringComparison.OrdinalIgnoreCase)
                && Volatile.Read(ref _activeSyncOperations) > 0;
        }
    }

    private string? GetClinicHeader()
    {
        var header = _settings.Current.ReportHeader;
        if (string.IsNullOrWhiteSpace(header))
            return null;

        return header;
    }

    private void EnsurePdfFontsRegistered()
    {
        if (_pdfFontsRegistered || _pdfFontRegistrationAttempted)
            return;

        lock (_pdfFontLock)
        {
            if (_pdfFontsRegistered || _pdfFontRegistrationAttempted)
                return;

            _pdfFontRegistrationAttempted = true;

            try
            {
                if (TryRegisterWindowsPdfFonts())
                {
                    _pdfFontsRegistered = true;
                    _log.Info("Шрифты Arial зарегистрированы для PDF-отчетов.");
                }
                else
                {
                    _log.Warn("Не удалось автоматически зарегистрировать шрифты Arial для PDF. Будут использованы шрифты по умолчанию.");
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Не удалось зарегистрировать шрифты Arial для PDF: {ex.Message}");
            }
        }
    }

    private static bool TryRegisterWindowsPdfFonts()
    {
        var fontsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if (string.IsNullOrWhiteSpace(fontsDirectory))
            return false;

        var regular = Path.Combine(fontsDirectory, "arial.ttf");
        if (!File.Exists(regular))
            return false;

        var bold = Path.Combine(fontsDirectory, "arialbd.ttf");
        var italic = Path.Combine(fontsDirectory, "ariali.ttf");
        var boldItalic = Path.Combine(fontsDirectory, "arialbi.ttf");

        return QuestPdfFontRegistrar.TryRegisterFontFamily(
            "Arial",
            regular,
            File.Exists(bold) ? bold : null,
            File.Exists(italic) ? italic : null,
            File.Exists(boldItalic) ? boldItalic : null);
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
                Interlocked.Increment(ref _activeSyncOperations);
                try
                {
                    await SyncOnceAsync(portName, ct);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeSyncOperations);
                }

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
        var wordGenerationEnabled = _settings.Current.GenerateWordReports;
        if (!wordGenerationEnabled)
        {
            _log.Info("Генерация Word-отчетов отключена в настройках. DOCX-файлы создаваться не будут.");
        }
        int? expectedPatients = _lastDeviceInfo?.DeviceInfo.TotalNumId;
        int maxPatients = Math.Max(1, expectedPatients ?? 1);
        bool sessionAnnounced = false;
        int processedPatients = 0;
        // var template = _settings.Current.ReportTemplate;
        var template = ReportTemplate.Client;
        var clinicHeader = GetClinicHeader();

        for (int index = 1; index <= maxPatients; index++)
        {
            ct.ThrowIfCancellationRequested();

            bool requestNextPatient = false;
            bool stopRequested = false;
            int attempt = 0;
            string? attemptPath = null;
            string? finalRawPath = null;
            bool ackSent = false;

            void EnsureAckSent()
            {
                if (ackSent)
                {
                    return;
                }

                SendContinueAck(port, portName, $"подтверждение получения пациента #{index}");
                ackSent = true;
            }

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
                    if (attempt < Math.Max(1, options.RetryCount))
                    {
                        if (attempt == 1)
                        {
                            _log.Warn($"[{portName}] устройство не прислало данные пациента #{index} (попытка {attempt}). Повторим запрос.");
                        }
                        else
                        {
                            _log.Warn($"[{portName}] повторная передача пациента #{index} вернула пустой блок (попытка {attempt}).");
                        }

                        var retryDelay = options.AttemptDelay;
                        if (retryDelay > TimeSpan.Zero)
                        {
                            _log.Debug($"[{portName}] ожидание {retryDelay.TotalMilliseconds:F0} мс перед повторным запросом пациента #{index}.");
                            await Task.Delay(retryDelay, ct);
                        }

                        port.DiscardInBuffer();
                        continue;
                    }

                    if (attempt == 1)
                    {
                        _log.Info("Передача пациентов завершена устройством.");
                    }
                    else
                    {
                        _log.Warn($"[{portName}] устройство не передало данные пациента #{index} после {attempt} попыток. Синхронизация остановлена.");
                        _telegram?.NotifyPatientTransferTimeout(index, attempt);
                    }

                    stopRequested = true;
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
                        EnsureAckSent();
                        processedPatients++;
                        break;
                    }

                    port.DiscardInBuffer();
                    _log.Debug($"[{portName}] запрос повторной передачи пациента #{index} (попытка {attempt + 1}).");
                    continue;
                }

                finalRawPath = PromoteAttemptToFinal(sessionDir, index, attemptPath);
                EnsureAckSent();
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
                        var pdfNameInfo = ReportFileNaming.CreatePdfFileName(patient, DateTime.Now);
                        if (pdfNameInfo.UsedFallback)
                        {
                            _log.Warn($"[{portName}] не удалось определить дату из '{patient.TestDateTime}', используется {pdfNameInfo.Timestamp:dd.MM.yyyy HH:mm} для имени файла.");
                        }

                        var candidatePdfPath = Path.Combine(_settings.Current.ReportsDirectory, pdfNameInfo.FileName);
                        try
                        {
                            if (File.Exists(candidatePdfPath))
                            {
                                _log.Info($"Существующий PDF-отчет будет обновлен: {candidatePdfPath}");
                            }

                            ErgReportBuilder.BuildPatientReport(patient, candidatePdfPath, _lastDeviceInfo?.DeviceInfo, clinicName: clinicHeader, rawFilePath: finalRawPath, template: template);
                            _log.Info($"PDF-отчет для пациента #{index} создан: {candidatePdfPath}");
                            pdfPath = candidatePdfPath;
                            ReportGenerated?.Invoke(this, pdfPath);
                            generatedPdfReports.Add(pdfPath);
                        }
                        catch (IOException ex) when (IsFileInUse(ex))
                        {
                            var failure = HandlePdfSaveFailure(
                                candidatePdfPath,
                                ex,
                                $"[{portName}] пациент #{index}");

                            if (!failure.ShouldRetryLater)
                            {
                                var reason = $"Не удалось создать PDF-отчет: {failure.Reason}";
                                RenderingSupport.DisablePdf(reason);
                                _pdfGenerationEnabled = false;
                                _pdfGenerationIssue = RenderingSupport.PdfIssue ?? reason;
                                LogPdfGenerationDisabled(portName);
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            var failure = HandlePdfSaveFailure(
                                candidatePdfPath,
                                ex,
                                $"[{portName}] пациент #{index}");

                            if (!failure.ShouldRetryLater)
                            {
                                var reason = $"Не удалось создать PDF-отчет: {failure.Reason}";
                                RenderingSupport.DisablePdf(reason);
                                _pdfGenerationEnabled = false;
                                _pdfGenerationIssue = RenderingSupport.PdfIssue ?? reason;
                                LogPdfGenerationDisabled(portName);
                            }
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
                    if (wordGenerationEnabled)
                    {
                        var candidateDocx = Path.Combine(sessionDir, $"patient_{index:000}.docx");
                        try
                        {
                            ErgReportBuilder.BuildPatientWordReport(patient, candidateDocx, _lastDeviceInfo?.DeviceInfo, clinicName: clinicHeader, rawFilePath: finalRawPath, template: template);
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
                    }

                    processedPatients++;
                    var pdfPathForNotification = pdfPath ?? "<не создан>";
                    string? docxPathForNotification = null;
                    if (wordGenerationEnabled)
                    {
                        docxPathForNotification = docxPath ?? "<не создан>";
                    }
                    _telegram?.NotifyPatientProcessed(index, patient, finalRawPath, jsonPath, pdfPathForNotification, docxPathForNotification);
                }

                requestNextPatient = true;
                break;
            }

            if (stopRequested)
            {
                break;
            }

            if (requestNextPatient && !ackSent)
            {
                EnsureAckSent();
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
            if (!expectedPatients.HasValue)
            {
                _log.Warn($"[{portName}] синхронизация времени пропущена: нет данных о количестве пациентов в приборе.");
            }
            else
            {
                var expected = expectedPatients.Value;
                var pdfCount = generatedPdfReports.Count;
                var receivedCount = processedPatients;
                bool countsMatch = pdfCount == expected && receivedCount == expected;

                if (countsMatch)
                {
                    var rtc = ErgProtocol.BuildRtcSet(DateTime.Now);
                    port.Write(rtc, 0, rtc.Length);
                    _log.Info("Часы прибора синхронизированы.");
                }
                else
                {
                    var reasons = new List<string>();
                    if (receivedCount != expected)
                    {
                        reasons.Add($"получено пациентов {receivedCount} из {expected}");
                    }
                    if (pdfCount != expected)
                    {
                        reasons.Add($"создано PDF {pdfCount} из {expected}");
                    }

                    var reasonText = reasons.Count > 0
                        ? string.Join(", ", reasons)
                        : $"обнаружено несоответствие данным прибора ({expected})";

                    _log.Warn($"[{portName}] синхронизация времени пропущена: {reasonText}.");
                }
            }
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
        var root = Path.Combine(_settings.BaseDirectory, "Sessions");
        Directory.CreateDirectory(root);
        var sessionDir = Path.Combine(root, timestamp);
        Directory.CreateDirectory(sessionDir);
        _log.Info($"Рабочие файлы синхронизации сохраняются в каталоге {sessionDir}.");
        return sessionDir;
    }

    private static DateTime ResolveFallbackTimestamp(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            try
            {
                var info = new FileInfo(filePath);
                if (info.Exists)
                {
                    return info.LastWriteTime;
                }
            }
            catch
            {
                // ignore IO exceptions, fallback to current time
            }
        }

        return DateTime.Now;
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
        string? docxPath = null;
        if (_settings.Current.GenerateWordReports)
        {
            docxPath = Path.Combine(directory, $"{baseName}.docx");
        }

        var pdfFallback = ResolveFallbackTimestamp(filePath);
        var pdfNameInfo = ReportFileNaming.CreatePdfFileName(patient, pdfFallback);
        if (pdfNameInfo.UsedFallback)
        {
            _log.Warn($"[{filePath}] не удалось определить дату из '{patient.TestDateTime}', используется {pdfNameInfo.Timestamp:dd.MM.yyyy HH:mm} для имени файла.");
        }

        Directory.CreateDirectory(_settings.Current.ReportsDirectory);
        var pdfPath = Path.Combine(_settings.Current.ReportsDirectory, pdfNameInfo.FileName);
        if (File.Exists(pdfPath))
        {
            _log.Info($"Существующий PDF-отчет будет обновлен: {pdfPath}");
        }

        // var template = _settings.Current.ReportTemplate;
        var template = ReportTemplate.Client;
        var clinicHeader = GetClinicHeader();

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
            ErgReportBuilder.BuildPatientReport(patient, pdfPath, _lastDeviceInfo?.DeviceInfo, clinicName: clinicHeader, rawFilePath: filePath, template: template);
            _log.Info($"PDF-отчет сохранен: {pdfPath}");
        }
        catch (IOException ex) when (IsFileInUse(ex))
        {
            var failure = HandlePdfSaveFailure(
                pdfPath,
                ex,
                $"[{Path.GetFileName(filePath)}]",
                notifyTelegram: false);

            var reason = $"Ошибка генерации PDF: {failure.Reason}";
            _log.Error($"[{filePath}] {reason}");
            _telegram?.NotifyManualConversionFailed(filePath, reason);
            return result with { ErrorMessage = reason, JsonPath = jsonPath };
        }
        catch (UnauthorizedAccessException ex)
        {
            var failure = HandlePdfSaveFailure(
                pdfPath,
                ex,
                $"[{Path.GetFileName(filePath)}]",
                notifyTelegram: false);

            var reason = $"Ошибка генерации PDF: {failure.Reason}";
            _log.Error($"[{filePath}] {reason}");
            _telegram?.NotifyManualConversionFailed(filePath, reason);
            return result with { ErrorMessage = reason, JsonPath = jsonPath };
        }
        catch (Exception ex)
        {
            var reason = $"Ошибка генерации PDF: {ex.Message}";
            _log.Error($"[{filePath}] {reason}");
            _telegram?.NotifyManualConversionFailed(filePath, reason);
            return result with { ErrorMessage = reason, JsonPath = jsonPath };
        }

        if (_settings.Current.GenerateWordReports)
        {
            try
            {
                ErgReportBuilder.BuildPatientWordReport(patient, docxPath!, _lastDeviceInfo?.DeviceInfo, clinicName: clinicHeader, rawFilePath: filePath, template: template);
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
        }
        else
        {
            _log.Info("Генерация Word-отчетов отключена в настройках. DOCX-файл сохранен не будет.");
        }

        _log.Info($"Ручное преобразование успешно завершено для {filePath}.");
        _telegram?.NotifyManualConversionSucceeded(filePath, patient, jsonPath, pdfPath, docxPath);
        return result with { Success = true, JsonPath = jsonPath, PdfPath = pdfPath, DocxPath = docxPath, Patient = patient };
    }

    private readonly record struct PdfSaveResult(bool ShouldRetryLater, string Reason);

    private PdfSaveResult HandlePdfSaveFailure(
        string originalPath,
        Exception reason,
        string logContext,
        bool notifyTelegram = true)
    {
        var message = reason.Message;
        bool isBusy = reason is IOException ioEx && IsFileInUse(ioEx);
        bool isUnauthorized = reason is UnauthorizedAccessException;

        if (isBusy || isUnauthorized)
        {
            var status = isUnauthorized
                ? "файл недоступен: отказано в доступе"
                : "файл занят другим процессом";
            _log.Warn($"{logContext} не удалось сохранить PDF {originalPath}: {status} ({message}). Сохранение пропущено, повторим позже.");

            if (notifyTelegram)
            {
                var fileName = Path.GetFileName(originalPath);
                _telegram?.NotifyMessage($"⚠️ {logContext} не удалось сохранить PDF {fileName}: {status}. Закройте файл и повторите.");
            }

            return new PdfSaveResult(true, status);
        }

        _log.Error($"{logContext} не удалось сохранить PDF {originalPath}: {message}");
        if (notifyTelegram)
        {
            _telegram?.NotifyMessage($"⚠️ {logContext} не удалось сохранить PDF: {message}");
        }

        return new PdfSaveResult(false, message);
    }

    private static bool IsFileInUse(IOException ex)
    {
        const int ErrorSharingViolation = 32;
        const int ErrorLockViolation = 33;
        const int ErrorAccessDenied = 5;

        var code = ex.HResult & 0xFFFF;
        return code is ErrorSharingViolation or ErrorLockViolation or ErrorAccessDenied;
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

internal static class QuestPdfFontRegistrar
{
    public static bool TryRegisterFontFamily(string familyName, string regularPath, string? boldPath, string? italicPath, string? boldItalicPath)
    {
        var fontManagerType = typeof(FontManager);

        if (TryInvokeRegisterFontTypefaces(fontManagerType, familyName, regularPath, boldPath, italicPath, boldItalicPath))
            return true;

        if (TryRegisterUsingDescriptors(fontManagerType, familyName, regularPath, boldPath, italicPath, boldItalicPath))
            return true;

        return false;
    }

    private static bool TryInvokeRegisterFontTypefaces(Type fontManagerType, string familyName, string regularPath, string? boldPath, string? italicPath, string? boldItalicPath)
    {
        var method = fontManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => string.Equals(m.Name, "RegisterFontTypefaces", StringComparison.Ordinal));

        if (method == null)
            return false;

        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            args[i] = parameters[i].Name switch
            {
                "familyName" => familyName,
                "regular" or "normal" => regularPath,
                "bold" => boldPath,
                "italic" => italicPath,
                "boldItalic" or "bolditalic" or "bold_italic" => boldItalicPath,
                _ => null
            };
        }

        try
        {
            method.Invoke(null, args);
            return true;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
        {
            return true;
        }
        catch (InvalidOperationException)
        {
            return true;
        }

        return false;
    }

    private static bool TryRegisterUsingDescriptors(Type fontManagerType, string familyName, string regularPath, string? boldPath, string? italicPath, string? boldItalicPath)
    {
        var assembly = fontManagerType.Assembly;
        var collectionType = assembly.GetType("QuestPDF.Drawing.TypefaceCollection")
            ?? assembly.GetType("QuestPDF.Drawing.FontDescriptor");

        if (collectionType == null)
            return false;

        var registerMethod = fontManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => string.Equals(m.Name, "RegisterFont", StringComparison.Ordinal)
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType.IsAssignableFrom(collectionType));

        if (registerMethod == null)
            return false;

        var collection = Activator.CreateInstance(collectionType);
        if (collection == null)
            return false;

        SetProperty(collectionType, collection, "FamilyName", familyName);
        SetProperty(collectionType, collection, "Name", familyName);

        var typefaceType = DiscoverTypefaceType(collectionType);
        if (typefaceType == null)
            return false;

        AssignFontSlot(collection, collectionType, typefaceType, new[] { "Regular", "Normal" }, regularPath, isBold: false, isItalic: false);
        AssignFontSlot(collection, collectionType, typefaceType, new[] { "Bold" }, boldPath, isBold: true, isItalic: false);
        AssignFontSlot(collection, collectionType, typefaceType, new[] { "Italic" }, italicPath, isBold: false, isItalic: true);
        AssignFontSlot(collection, collectionType, typefaceType, new[] { "BoldItalic", "Bold_Italic", "Bolditalic" }, boldItalicPath, isBold: true, isItalic: true);

        try
        {
            registerMethod.Invoke(null, new[] { collection });
            return true;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
        {
            return true;
        }
        catch (InvalidOperationException)
        {
            return true;
        }

        return false;
    }

    private static Type? DiscoverTypefaceType(Type collectionType)
    {
        foreach (var property in collectionType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite)
                continue;

            var candidateName = property.Name;
            if (candidateName.Equals("Regular", StringComparison.OrdinalIgnoreCase) || candidateName.Equals("Normal", StringComparison.OrdinalIgnoreCase))
                return property.PropertyType;
        }

        return collectionType.Assembly.GetType("QuestPDF.Drawing.Typeface");
    }

    private static void AssignFontSlot(object collection, Type collectionType, Type typefaceType, IEnumerable<string> slotNames, string? path, bool isBold, bool isItalic)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        foreach (var slotName in slotNames)
        {
            var property = collectionType.GetProperty(slotName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null || !property.CanWrite)
                continue;

            var typeface = Activator.CreateInstance(typefaceType);
            if (typeface == null)
                continue;

            if (!TryAssignTypefaceData(typeface, typefaceType, path))
                continue;

            ApplyEnum(typeface, typefaceType, "FontWeight", isBold ? "Bold" : "Regular");
            ApplyEnum(typeface, typefaceType, "Weight", isBold ? "Bold" : "Regular");
            ApplyEnum(typeface, typefaceType, "FontStyle", isItalic ? "Italic" : "Normal");
            ApplyEnum(typeface, typefaceType, "Style", isItalic ? "Italic" : "Normal");

            property.SetValue(collection, typeface);
            break;
        }
    }

    private static bool TryAssignTypefaceData(object typeface, Type typefaceType, string path)
    {
        foreach (var propertyName in new[] { "FilePath", "Path", "Location", "Source" })
        {
            var property = typefaceType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null && property.CanWrite && property.PropertyType == typeof(string))
            {
                property.SetValue(typeface, path);
                return true;
            }
        }

        var dataProperty = typefaceType.GetProperty("Data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (dataProperty != null && dataProperty.CanWrite)
        {
            var bytes = File.ReadAllBytes(path);

            if (dataProperty.PropertyType == typeof(byte[]))
            {
                dataProperty.SetValue(typeface, bytes);
                return true;
            }

            if (dataProperty.PropertyType == typeof(ReadOnlyMemory<byte>))
            {
                dataProperty.SetValue(typeface, new ReadOnlyMemory<byte>(bytes));
                return true;
            }
        }

        var streamProperty = typefaceType.GetProperty("Stream", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (streamProperty != null && streamProperty.CanWrite && typeof(Delegate).IsAssignableFrom(streamProperty.PropertyType))
        {
            var streamType = streamProperty.PropertyType;
            if (streamType == typeof(Func<Stream>))
            {
                streamProperty.SetValue(typeface, (Func<Stream>)(() => File.OpenRead(path)));
                return true;
            }
        }

        return false;
    }

    private static void ApplyEnum(object instance, Type type, string propertyName, string value)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
            return;

        if (Enum.TryParse(property.PropertyType, value, true, out var parsed))
        {
            property.SetValue(instance, parsed);
        }
    }

    private static void SetProperty(Type type, object instance, string propertyName, object? value)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property != null && property.CanWrite)
        {
            property.SetValue(instance, value);
        }
    }
}
