using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErgData;
using MicroluxErgConnect.Models;

namespace MicroluxErgConnect.Services;

public sealed class TelegramNotificationService : IDisposable
{
    private const int TelegramMessageLimit = 4000;
    private const long TelegramDocumentLimitBytes = 49L * 1024 * 1024; // Telegram Bot API ограничивает документы ~50 МБ.

    private readonly SettingsService _settings;
    private readonly LogService _log;
    private readonly HttpClient _httpClient;
    private readonly Channel<Func<CancellationToken, Task>> _queue;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processorTask;
    private readonly object _configLock = new();

    private TelegramSettings _configSnapshot;
    private string? _lastConfigIssue;
    private int _suppressForwarding;

    public TelegramNotificationService(SettingsService settings, LogService log)
    {
        _settings = settings;
        _log = log;
        _configSnapshot = settings.Current.Telegram ?? new TelegramSettings();

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _processorTask = Task.Run(ProcessQueueAsync);

        _settings.SettingsChanged += OnSettingsChanged;
    }

    public bool IsEnabled
    {
        get
        {
            lock (_configLock)
            {
                return _configSnapshot.Enabled
                    && !string.IsNullOrWhiteSpace(_configSnapshot.BotToken)
                    && !string.IsNullOrWhiteSpace(_configSnapshot.ChatId);
            }
        }
    }

    public void NotifyApplicationStarted(string version)
    {
        var message = new StringBuilder()
            .AppendLine("🚀 Microlux ERG-Connect запущено")
            .AppendLine($"Версия: {version}")
            .AppendLine($"Пользователь: {Environment.UserDomainName}\\{Environment.UserName}")
            .AppendLine($"Машина: {Environment.MachineName}")
            .ToString();
        EnqueueMessage(message);
    }

    public void NotifyAutoUpdaterSummary(Version? manifestVersion, string? packageUrl, bool? mandatory, string? mandatoryMode, string? description, string? error, bool exitRequested)
    {
        var summary = new StringBuilder()
            .AppendLine("🔁 Проверка обновлений (AutoUpdater.NET)");

        if (manifestVersion != null)
        {
            summary.AppendLine($"Версия манифеста: {manifestVersion}");
        }

        if (!string.IsNullOrWhiteSpace(packageUrl))
        {
            summary.AppendLine($"Пакет: {packageUrl}");
        }

        if (mandatory.HasValue)
        {
            summary.AppendLine($"Обязательное обновление: {(mandatory.Value ? $"да (режим {mandatoryMode ?? "?"})" : "нет")}");
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            summary.AppendLine("Описание:");
            foreach (var line in NormalizeMultiline(description, 800))
            {
                summary.AppendLine(line);
            }
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            summary.AppendLine($"⚠️ Ошибка: {error}");
        }

        summary.AppendLine(exitRequested
            ? "Режим: установка обновления инициирована."
            : "Режим: приложение продолжит работу.");

        EnqueueMessage(summary.ToString());
    }

    public void NotifyApplicationStopping(string logPath)
    {
        var message = $"⏹️ Microlux ERG-Connect завершает работу. Лог: {logPath}";
        EnqueueMessage(message);

        if (File.Exists(logPath) && CurrentSettings.SendLogOnExit)
        {
            EnqueueDocument(logPath, "Лог сессии приложения");
        }
    }

    public void NotifyDeviceConnected(DeviceConnectionInfo info)
    {
        var payload = new StringBuilder()
            .AppendLine("🔌 Устройство подключено")
            .AppendLine($"Порт: {info.PortName}")
            .AppendLine($"Прибор: {info.DeviceInfo.DeviceName}")
            .AppendLine($"Отчет: {info.DeviceInfo.ReportName}")
            .AppendLine($"ПО: {info.DeviceInfo.SoftwareRev}")
            .AppendLine($"Пациентов в памяти: {info.DeviceInfo.TotalNumId}")
            .ToString();
        EnqueueMessage(payload);
    }

    public void NotifyDeviceDisconnected(string? reason)
    {
        var text = string.IsNullOrWhiteSpace(reason)
            ? "🔌 Устройство отключено"
            : $"🔌 Устройство отключено: {reason}";
        EnqueueMessage(text);
    }

    public void NotifySessionStarted(string portName, string sessionDirectory, int expectedPatients)
    {
        var message = new StringBuilder()
            .AppendLine("📥 Начата синхронизация пациентов")
            .AppendLine($"Порт: {portName}")
            .AppendLine($"Каталог: {sessionDirectory}")
            .AppendLine($"Ожидается пациентов: {expectedPatients}")
            .ToString();
        EnqueueMessage(message);
    }

    public void NotifyNoPatients(string portName)
    {
        EnqueueMessage($"ℹ️ Устройство на {portName} не передало новых пациентов.");
    }

    public void NotifyPatientParseFailed(int patientIndex, string rawPath, string error)
    {
        var message = new StringBuilder()
            .AppendLine($"⚠️ Пациент #{patientIndex:000} не обработан")
            .AppendLine($"Причина: {error}")
            .AppendLine($"Сырые данные: {rawPath}")
            .ToString();
        EnqueueMessage(message);

        if (File.Exists(rawPath) && CurrentSettings.ForwardRawData)
        {
            EnqueueDocument(rawPath, $"patient_{patientIndex:000}.bin (ошибка)");
        }
    }

    public void NotifyPatientChecksumFailed(int patientIndex, string rawPath, int attempts)
    {
        var message = new StringBuilder()
            .AppendLine($"❌ Контрольная сумма пациента #{patientIndex:000} не подтверждена")
            .AppendLine($"Попыток: {attempts}")
            .AppendLine($"Дамп: {rawPath}")
            .ToString();
        EnqueueMessage(message);

        if (File.Exists(rawPath) && CurrentSettings.ForwardRawData)
        {
            EnqueueDocument(rawPath, $"patient_{patientIndex:000}.bin (ошибка контрольной суммы)");
        }
    }

    public void NotifyPatientTransferTimeout(int patientIndex, int attempts)
    {
        var message = new StringBuilder()
            .AppendLine($"⚠️ Пациент #{patientIndex:000} не получен")
            .AppendLine($"Попыток без ответа: {attempts}")
            .AppendLine("Устройство не передало данные в отведённое время.")
            .ToString();
        EnqueueMessage(message);
    }

    public void NotifyPatientProcessed(int patientIndex, ErgPatient patient, string rawPath, string jsonPath, string pdfPath, string docxPath)
    {
        var summary = new StringBuilder()
            .AppendLine($"✅ Пациент #{patientIndex:000} обработан")
            .AppendLine($"ID: {patient.PatientId}")
            .AppendLine($"Животное: {patient.Animal}")
            .AppendLine($"Дата: {patient.TestDateTime}")
            .AppendLine($"Тестов: {patient.Tests.Count}/{patient.TotalNumTests}");

        if (!string.IsNullOrWhiteSpace(patient.Description))
        {
            summary.AppendLine($"Заключение: {TrimText(patient.Description, 200)}");
        }

        summary.AppendLine($"RAW: {rawPath}");
        summary.AppendLine($"JSON: {jsonPath}");
        summary.AppendLine($"PDF: {pdfPath}");
        summary.AppendLine($"Word: {docxPath}");

        if (patient.Warnings is { Count: > 0 })
        {
            summary.AppendLine("Предупреждения:");
            foreach (var warning in patient.Warnings.Take(3))
            {
                summary.AppendLine($" • {TrimText(warning, 200)}");
            }

            if (patient.Warnings.Count > 3)
            {
                summary.AppendLine($" … и ещё {patient.Warnings.Count - 3}");
            }
        }

        EnqueueMessage(summary.ToString());

        var settings = CurrentSettings;
        if (settings.ForwardRawData && File.Exists(rawPath))
        {
            EnqueueDocument(rawPath, $"patient_{patientIndex:000}.bin");
        }

        if (settings.ForwardJson && File.Exists(jsonPath))
        {
            EnqueueDocument(jsonPath, $"patient_{patientIndex:000}.json");
        }

        if (settings.ForwardReports)
        {
            if (File.Exists(pdfPath))
            {
                EnqueueDocument(pdfPath, $"patient_{patientIndex:000}.pdf");
            }

            if (File.Exists(docxPath))
            {
                EnqueueDocument(docxPath, $"patient_{patientIndex:000}.docx");
            }
        }
    }

    public void NotifySessionCompleted(string portName, string? sessionDirectory, int processedPatients, int generatedReports)
    {
        var builder = new StringBuilder()
            .AppendLine("📦 Синхронизация завершена")
            .AppendLine($"Порт: {portName}")
            .AppendLine($"Получено пациентов: {processedPatients}")
            .AppendLine($"Отчетов сформировано: {generatedReports}");

        if (!string.IsNullOrWhiteSpace(sessionDirectory))
        {
            builder.AppendLine($"Каталог данных: {sessionDirectory}");
        }

        EnqueueMessage(builder.ToString());
    }

    public void NotifyMessage(string message)
        => EnqueueMessage(message);

    public void NotifyManualConversionStarted(string filePath)
    {
        var message = new StringBuilder()
            .AppendLine("🛠 Ручное преобразование файла пациента")
            .AppendLine(filePath)
            .ToString();
        EnqueueMessage(message);
    }

    public void NotifyManualConversionSucceeded(string filePath, ErgPatient patient, string jsonPath, string pdfPath, string docxPath)
    {
        var message = new StringBuilder()
            .AppendLine("✅ Ручное преобразование завершено успешно")
            .AppendLine(filePath)
            .AppendLine($"ID пациента: {patient.PatientId}")
            .AppendLine($"Животное: {patient.Animal}")
            .AppendLine($"Тестов: {patient.Tests.Count}/{patient.TotalNumTests}")
            .AppendLine($"JSON: {jsonPath}")
            .AppendLine($"PDF: {pdfPath}")
            .AppendLine($"Word: {docxPath}");

        if (!string.IsNullOrWhiteSpace(patient.Description))
        {
            message.AppendLine($"Заключение: {TrimText(patient.Description, 200)}");
        }

        if (patient.Warnings is { Count: > 0 })
        {
            message.AppendLine("Предупреждения:");
            foreach (var warning in patient.Warnings.Take(3))
            {
                message.AppendLine($" • {TrimText(warning, 200)}");
            }
            if (patient.Warnings.Count > 3)
            {
                message.AppendLine($" … и ещё {patient.Warnings.Count - 3}");
            }
        }

        EnqueueMessage(message.ToString());

        var settings = CurrentSettings;
        if (settings.ForwardRawData && File.Exists(filePath))
        {
            EnqueueDocument(filePath, Path.GetFileName(filePath));
        }
        if (settings.ForwardJson && File.Exists(jsonPath))
        {
            EnqueueDocument(jsonPath, Path.GetFileName(jsonPath));
        }
        if (settings.ForwardReports)
        {
            if (File.Exists(pdfPath))
            {
                EnqueueDocument(pdfPath, Path.GetFileName(pdfPath));
            }

            if (File.Exists(docxPath))
            {
                EnqueueDocument(docxPath, Path.GetFileName(docxPath));
            }
        }
    }

    public void NotifyManualConversionFailed(string filePath, string reason)
    {
        var message = new StringBuilder()
            .AppendLine("⚠️ Ручное преобразование не выполнено")
            .AppendLine(filePath)
            .AppendLine($"Причина: {reason}")
            .ToString();
        EnqueueMessage(message);
    }

    public void Dispose()
    {
        _settings.SettingsChanged -= OnSettingsChanged;
        _queue.Writer.TryComplete();

        var completed = false;
        try
        {
            completed = _processorTask.Wait(TimeSpan.FromSeconds(15));
        }
        catch (AggregateException)
        {
            _cts.Cancel();
        }

        if (!completed)
        {
            _cts.Cancel();
            try
            {
                _processorTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Игнорируем ошибки отмены фонового обработчика.
            }
        }

        _httpClient.Dispose();
        _cts.Dispose();
    }

    private TelegramSettings CurrentSettings
    {
        get
        {
            lock (_configLock)
            {
                return _configSnapshot;
            }
        }
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_queue.Reader.TryRead(out var work))
                {
                    try
                    {
                        await work(_cts.Token);
                    }
                    catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        using (SuppressForwarding())
                        {
                            _log.Warn($"[Telegram] Ошибка обработки очереди: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        lock (_configLock)
        {
            _configSnapshot = settings.Telegram ?? new TelegramSettings();
            _lastConfigIssue = null;
        }

        if (_configSnapshot.Enabled && (string.IsNullOrWhiteSpace(_configSnapshot.BotToken) || string.IsNullOrWhiteSpace(_configSnapshot.ChatId)))
        {
            using (SuppressForwarding())
            {
                _log.Warn("[Telegram] Уведомления включены, но не задан BotToken или ChatId.");
            }
        }
    }

    private void Enqueue(Func<CancellationToken, Task> work)
    {
        if (!TryGetConfiguration(out _, out _, logIssues: false))
        {
            return;
        }

        if (!_queue.Writer.TryWrite(work))
        {
            using (SuppressForwarding())
            {
                _log.Warn("[Telegram] Очередь уведомлений переполнена, событие пропущено.");
            }
        }
    }

    private void EnqueueMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        foreach (var chunk in SplitMessage(message))
        {
            Enqueue(ct => SendMessageAsync(chunk, ct));
        }
    }

    private void EnqueueDocument(string path, string caption)
    {
        if (!File.Exists(path))
        {
            return;
        }

        Enqueue(ct => SendDocumentAsync(path, caption, ct));
    }

    private async Task SendMessageAsync(string message, CancellationToken ct)
    {
        if (!TryGetConfiguration(out var config, out var chatId, logIssues: true))
        {
            return;
        }

        await SendMessageAsync(config, chatId, message, ct);
    }

    private async Task SendDocumentAsync(string path, string caption, CancellationToken ct)
    {
        if (!TryGetConfiguration(out var config, out var chatId, logIssues: true))
        {
            return;
        }

        var fileInfo = new FileInfo(path);
        var sendResult = fileInfo.Length > TelegramDocumentLimitBytes
            ? SendDocumentResult.TooLarge
            : await SendDocumentInternalAsync(config, chatId, path, caption, ct);

        if (sendResult != SendDocumentResult.TooLarge)
        {
            return;
        }

        string? zipPath = null;
        try
        {
            zipPath = await TryCreateZipCopyAsync(path, ct);
            if (zipPath == null)
            {
                NotifyFileTooLarge(path, fileInfo.Length);
                return;
            }

            var zipInfo = new FileInfo(zipPath);
            if (zipInfo.Length > TelegramDocumentLimitBytes)
            {
                NotifyFileTooLarge(path, zipInfo.Length, archived: true);
                return;
            }

            var zipCaption = string.IsNullOrWhiteSpace(caption)
                ? $"{Path.GetFileName(path)} (ZIP)"
                : $"{caption} (ZIP)";

            await SendMessageAsync(config, chatId,
                $"📦 Файл {Path.GetFileName(path)} упакован в ZIP ({FormatFileSize(zipInfo.Length)}).", ct);

            var zipResult = await SendDocumentInternalAsync(config, chatId, zipPath, zipCaption, ct);
            if (zipResult == SendDocumentResult.TooLarge)
            {
                NotifyFileTooLarge(path, zipInfo.Length, archived: true);
            }
        }
        finally
        {
            if (zipPath != null)
            {
                try
                {
                    File.Delete(zipPath);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private bool TryGetConfiguration(out TelegramSettings config, out string chatId, bool logIssues)
    {
        lock (_configLock)
        {
            config = _configSnapshot;
        }

        chatId = string.Empty;

        if (!config.Enabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.BotToken) || string.IsNullOrWhiteSpace(config.ChatId))
        {
            if (logIssues)
            {
                LogConfigIssue("Не указан BotToken или ChatId.");
            }
            return false;
        }

        chatId = config.ChatId;
        return true;
    }

    private void LogConfigIssue(string issue)
    {
        bool shouldLog;
        lock (_configLock)
        {
            shouldLog = !string.Equals(_lastConfigIssue, issue, StringComparison.Ordinal);
            if (shouldLog)
            {
                _lastConfigIssue = issue;
            }
        }

        if (!shouldLog)
        {
            return;
        }

        using (SuppressForwarding())
        {
            _log.Warn($"[Telegram] {issue}");
        }
    }

    private IDisposable SuppressForwarding()
        => new ForwardingScope(this);

    private static IEnumerable<string> NormalizeMultiline(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var normalized = text.ReplaceLineEndings("\n").Trim();
        if (normalized.Length > maxLength)
        {
            normalized = normalized.Substring(0, maxLength) + "…";
        }

        foreach (var line in normalized.Split('\n'))
        {
            yield return line.TrimEnd();
        }
    }

    private static IEnumerable<string> SplitMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            yield break;
        }

        for (int offset = 0; offset < message.Length; offset += TelegramMessageLimit)
        {
            var length = Math.Min(TelegramMessageLimit, message.Length - offset);
            yield return message.Substring(offset, length);
        }
    }

    private static string TrimText(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "…";
    }

    private static string GuessMimeType(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".json" => "application/json",
            ".zip" => "application/zip",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

    private static string FormatFileSize(long bytes)
    {
        const double OneKb = 1024.0;
        const double OneMb = OneKb * 1024.0;
        const double OneGb = OneMb * 1024.0;

        return bytes switch
        {
            >= (long)OneGb => string.Format(CultureInfo.InvariantCulture, "{0:F2} ГБ", bytes / OneGb),
            >= (long)OneMb => string.Format(CultureInfo.InvariantCulture, "{0:F2} МБ", bytes / OneMb),
            >= (long)OneKb => string.Format(CultureInfo.InvariantCulture, "{0:F1} КБ", bytes / OneKb),
            _ => string.Format(CultureInfo.InvariantCulture, "{0} байт", bytes)
        };
    }

    private async Task SendMessageAsync(TelegramSettings config, string chatId, string message, CancellationToken ct)
    {
        var url = $"https://api.telegram.org/bot{config.BotToken}/sendMessage";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["chat_id"] = chatId,
            ["text"] = message,
            ["disable_web_page_preview"] = "true"
        });

        using var response = await _httpClient.PostAsync(url, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            using (SuppressForwarding())
            {
                _log.Warn($"[Telegram] Ошибка отправки сообщения: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
            }
        }
    }

    private async Task<SendDocumentResult> SendDocumentInternalAsync(TelegramSettings config, string chatId, string path, string caption, CancellationToken ct)
    {
        try
        {
            await using var fileStream = File.OpenRead(path);
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(chatId), "chat_id");
            if (!string.IsNullOrWhiteSpace(caption))
            {
                content.Add(new StringContent(caption), "caption");
            }

            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(GuessMimeType(path));
            content.Add(streamContent, "document", Path.GetFileName(path));

            var url = $"https://api.telegram.org/bot{config.BotToken}/sendDocument";
            using var response = await _httpClient.PostAsync(url, content, ct);
            if (response.IsSuccessStatusCode)
            {
                return SendDocumentResult.Success;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            using (SuppressForwarding())
            {
                _log.Warn($"[Telegram] Ошибка отправки файла '{path}': {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
            }

            return IsTooLargeError(response.StatusCode, body)
                ? SendDocumentResult.TooLarge
                : SendDocumentResult.Failed;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            using (SuppressForwarding())
            {
                _log.Warn($"[Telegram] Ошибка чтения файла '{path}': {ex.Message}");
            }
            return SendDocumentResult.Failed;
        }
    }

    private static bool IsTooLargeError(HttpStatusCode statusCode, string? body)
    {
        if (statusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            return true;
        }

        if (statusCode == HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(body))
        {
            if (body.IndexOf("too large", StringComparison.OrdinalIgnoreCase) >= 0
                || body.IndexOf("file is too big", StringComparison.OrdinalIgnoreCase) >= 0
                || body.IndexOf("must be less than", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string?> TryCreateZipCopyAsync(string sourcePath, CancellationToken ct)
    {
        try
        {
            var zipPath = Path.Combine(Path.GetTempPath(), $"erg_{Guid.NewGuid():N}.zip");
            await using (var zipStream = File.Open(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);
                var entryName = Path.GetFileName(sourcePath);
                var entry = archive.CreateEntry(entryName, CompressionLevel.SmallestSize);
                await using var entryStream = entry.Open();
                await using var fileStream = File.OpenRead(sourcePath);
                await fileStream.CopyToAsync(entryStream, ct);
            }

            return zipPath;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            using (SuppressForwarding())
            {
                _log.Warn($"[Telegram] Не удалось упаковать файл '{sourcePath}' в ZIP: {ex.Message}");
            }
            return null;
        }
    }

    private void NotifyFileTooLarge(string originalPath, long size, bool archived = false)
    {
        var descriptor = archived ? "архив" : "файл";
        var message = new StringBuilder()
            .Append("⚠️ ")
            .Append(char.ToUpper(descriptor[0], CultureInfo.CurrentCulture))
            .Append(descriptor.AsSpan(1))
            .Append(' ')
            .Append(Path.GetFileName(originalPath))
            .Append(archived ? " слишком большой даже после упаковки." : " слишком большой для отправки.")
            .Append(' ')
            .Append($"Размер: {FormatFileSize(size)}.")
            .ToString();

        // Не дожидаемся завершения отправки — поместим уведомление в очередь.
        EnqueueMessage(message);
    }

    private enum SendDocumentResult
    {
        Success,
        TooLarge,
        Failed
    }

    private sealed class ForwardingScope : IDisposable
    {
        private readonly TelegramNotificationService _owner;
        private bool _disposed;

        public ForwardingScope(TelegramNotificationService owner)
        {
            _owner = owner;
            Interlocked.Increment(ref _owner._suppressForwarding);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            Interlocked.Decrement(ref _owner._suppressForwarding);
            _disposed = true;
        }
    }
}
