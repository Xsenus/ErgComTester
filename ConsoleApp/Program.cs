using System.IO.Compression;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using ErgData;

namespace ErgComTester;

internal class Program
{
    static void FinalizePack(string baseDir, string sessionStamp, Logger logger, bool zipUnlessNozip)
    {
        try
        {
            // summary.txt
            var sumPath = Path.Combine(baseDir, "out", $"summary_{sessionStamp}.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(sumPath)!);
            using (var sw = new StreamWriter(sumPath))
            {
                sw.WriteLine($"ERG COM Tester Session {sessionStamp}");
                sw.WriteLine($".NET: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
                sw.WriteLine($"OS  : {Environment.OSVersion} | {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
                sw.WriteLine($"Ports: {string.Join(", ", SerialPort.GetPortNames().OrderBy(x => x))}");
            }
            logger.Info($"Summary saved: {sumPath}");

            if (zipUnlessNozip)
            {
                var zipPath = Path.Combine(baseDir, $"ERG_Support_{sessionStamp}.zip");
                if (File.Exists(zipPath)) File.Delete(zipPath);
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    void addDir(string rel)
                    {
                        var dir = Path.Combine(baseDir, rel);
                        if (!Directory.Exists(dir)) return;
                        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                        {
                            var entryPath = Path.GetRelativePath(baseDir, file);
                            zip.CreateEntryFromFile(file, entryPath);
                        }
                    }
                    addDir("logs");
                    addDir("out");
                }
                logger.Info($"Support package: {zipPath}");
            }
        }
        catch (Exception ex)
        {
            logger.Warn($"Finalize pack failed: {ex.Message}");
        }
    }

    static void ListPorts(Logger logger)
    {
        var ports = SerialPort.GetPortNames().OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
        if (ports.Length == 0) logger.Info("Ports: <none>");
        else logger.Info($"Ports: {string.Join(", ", ports)}");
    }

    static int Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var options = CliOptions.Parse(args);
        var sessionStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseDir = AppContext.BaseDirectory;
        var logDir = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, $"ergtester_{sessionStamp}.log");
        using var logger = new Logger(logPath, verbose: true);

        PrintHeader(logger, options);

        try
        {
            if (options.Mode == RunMode.Parse)
            {
                return RunParseMode(options, logger);
            }

            // AUTO mode by default
            if (options.Mode == RunMode.Auto)
            {
                var rc = RunAuto(options, logger);
                FinalizePack(baseDir, sessionStamp, logger, zipUnlessNozip: !options.NoZip);
                return rc;
            }

            // Manual modes preserved
            if (options.Mode == RunMode.List) { ListPorts(logger); return 0; }
            if (options.Mode == RunMode.Single)
            {
                if (string.IsNullOrWhiteSpace(options.PortName))
                {
                    logger.Error("Single mode requires --port=COMx");
                    return 1;
                }
                var rc = TestSinglePort(options.PortName!, options, logger, stopOnSuccess: false);
                FinalizePack(baseDir, sessionStamp, logger, zipUnlessNozip: !options.NoZip);
                return rc;
            }

            logger.Error("Unknown mode.");
            return 1;
        }
        catch (Exception ex)
        {
            logger.Error(ex.ToString());
            return 1;
        }
    }

    static void PrintHeader(Logger logger, CliOptions options)
    {
        logger.Section("ERG COM Tester (Auto)");
        logger.Info($"Version    : {Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0)}");
        logger.Info($".NET       : {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        logger.Info($"OS         : {Environment.OSVersion} | {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        logger.Info($"Machine    : {Environment.MachineName} | User: {Environment.UserName} | 64-bit: {Environment.Is64BitOperatingSystem}");
        logger.Info($"Args       : {string.Join(' ', Environment.GetCommandLineArgs().Skip(1))}");
        logger.Info($"Mode       : {options.Mode}");
        logger.Info($"Config     : baud={options.BaudRate}, retries={options.Retries}, quiet={options.QuietTimeMs}ms, readwin={options.MaxReadWindowMs}ms");
        logger.Info($"Lines      : DTR={(options.DtrEnable ? "on" : "off")}, RTS={(options.RtsEnable ? "on" : "off")}, toggleOnOpen={options.ToggleLinesOnOpen}");
        logger.Info($"Actions    : fetchPatients={!options.NoFetch}, rtcSync={options.Rtc}, zip={!options.NoZip}");

        if (!string.IsNullOrWhiteSpace(RenderingSupport.LegacyRenderingNotice))
        {
            logger.Info(RenderingSupport.LegacyRenderingNotice);
        }
    }

    static int RunAuto(CliOptions options, Logger logger)
    {
        // 1) List and log all ports
        ListPorts(logger);

        // 2) Scan all ports and try detect device
        var ports = SerialPort.GetPortNames().OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
        if (ports.Length == 0) { logger.Warn("No COM ports found."); return 2; }

        CommonInfo? foundInfo = null;
        string? foundPort = null;
        foreach (var p in ports)
        {
            var rc = TestSinglePort(p, options, logger, stopOnSuccess: true, onFound: (ci) => { foundInfo = ci; foundPort = p; });
            if (rc == 0) break;
        }

        if (foundInfo == null)
        {
            logger.Error("Device not found on any COM port.");
            return 3;
        }

        // 3) Summary
        logger.Section("SUMMARY");
        logger.Info($"Detected on : {foundPort}");
        logger.Info($"Device      : {foundInfo.DeviceName} | SW: {foundInfo.SoftwareRev}");
        logger.Info($"Report name : {foundInfo.ReportName}");
        logger.Info($"Patients    : {foundInfo.TotalNumId}");

        return 0;
    }

    static int TestSinglePort(string portName, CliOptions opt, Logger logger, bool stopOnSuccess = false, Action<CommonInfo>? onFound = null)
    {
        logger.Section($"Testing port {portName}");
        using var sp = new SerialPort(portName, opt.BaudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = opt.ReadTimeoutMs,
            WriteTimeout = opt.WriteTimeoutMs,
            DtrEnable = opt.DtrEnable,
            RtsEnable = opt.RtsEnable,
            Handshake = Handshake.None,
            NewLine = "\r\n"
        };

        try { sp.Open(); }
        catch (Exception ex) { logger.Warn($"Open failed: {ex.Message}"); return 10; }

        if (opt.ToggleLinesOnOpen)
        {
            try
            {
                logger.Debug("Toggling DTR/RTS...");
                sp.DtrEnable = false; sp.RtsEnable = false; Thread.Sleep(50);
                sp.DtrEnable = opt.DtrEnable; sp.RtsEnable = opt.RtsEnable; Thread.Sleep(50);
            }
            catch (Exception ex) { logger.Warn($"Toggle lines failed: {ex.Message}"); }
        }

        try { sp.DiscardInBuffer(); sp.DiscardOutBuffer(); } catch { }

        for (int attempt = 1; attempt <= opt.Retries; attempt++)
        {
            logger.Info($"[{portName}] PING attempt {attempt}/{opt.Retries}");
            try
            {
                var ping = ErgProtocol.BuildPing();
                ErgIo.Write(sp, ping, logger, "PING (0xE0)");

                var reply = ErgIo.ReadChunk(sp, logger, opt.MinCommonInfoSize, opt.QuietTimeMs, opt.MaxReadWindowMs);
                if (reply.Length == 0) { logger.Warn("No reply."); continue; }

                logger.HexDump("COMMON_INFO raw", reply);

                if (!ErgProtocol.ValidateChecksum(reply))
                {
                    logger.Warn("COMMON_INFO checksum invalid.");
                    continue;
                }

                if (!ErgParser.TryParseCommonInfo(reply, out var common, out var parseErr))
                {
                    logger.Warn($"COMMON_INFO parse warning: {parseErr}");
                }
                else
                {
                    logger.Info($"Device detected on {portName}:");
                    logger.Info($"  Report_Name  : {common.ReportName}");
                    logger.Info($"  Device_Name  : {common.DeviceName}");
                    logger.Info($"  Software_Rev : {common.SoftwareRev}");
                    logger.Info($"  Total_Num_ID : {common.TotalNumId}");
                    onFound?.Invoke(common);

                    if (!opt.NoFetch)
                    {
                        var outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "out", "raw"));
                        var jsonDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "out", "structured"));
                        var pdfDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "out", "pdf"));
                        var docxDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "out", "docx"));
                        Directory.CreateDirectory(outDir);
                        Directory.CreateDirectory(jsonDir);
                        Directory.CreateDirectory(pdfDir);
                        Directory.CreateDirectory(docxDir);
                        logger.Info($"Fetching up to {Math.Max(1, common.TotalNumId)} patient(s)...");

                        string? lastPdfWarning = null;
                        for (int i = 1; i <= Math.Max(1, common.TotalNumId); i++)
                        {
                            logger.Info($"GET (0xE5) block #{i}");
                            var get = ErgProtocol.BuildGetNext();
                            ErgIo.Write(sp, get, logger, "GET (0xE5)");
                            var block = ErgIo.ReadChunk(sp, logger, opt.MinPatientBlockSize, opt.QuietTimeMs, opt.MaxReadWindowMs);
                            if (block.Length == 0) { logger.Warn("Empty block, stop."); break; }

                            logger.HexDump($"PATIENT_BLOCK raw #{i}", block);
                            var rawPath = Path.Combine(outDir, $"patient_{i:000}.bin");
                            File.WriteAllBytes(rawPath, block);
                            logger.Info($"Saved raw: {rawPath}");

                            if (!ErgProtocol.ValidateChecksum(block))
                            {
                                logger.Warn("Checksum invalid -> REPEAT (0xEA)");
                                var rep = ErgProtocol.BuildRepeat();
                                ErgIo.Write(sp, rep, logger, "REPEAT (0xEA)");
                                var block2 = ErgIo.ReadChunk(sp, logger, opt.MinPatientBlockSize, opt.QuietTimeMs, opt.MaxReadWindowMs);
                                if (block2.Length > 0)
                                {
                                    logger.HexDump($"PATIENT_BLOCK raw (repeat) #{i}", block2);
                                    File.WriteAllBytes(rawPath.Replace(".bin", "_repeat.bin"), block2);
                                }
                            }
                            else
                            {
                                if (ErgParser.TryParsePatientBlock(block, out var pinfo, out var perr))
                                {
                                    logger.Info($"Patient_ID     : {pinfo.PatientId}");
                                    logger.Info($"Animal         : {pinfo.Animal}");
                                    logger.Info($"Tests          : {pinfo.Tests.Count}/{pinfo.TotalNumTests}");
                                    logger.Info($"Description    : {TrimForLog(pinfo.Description)}");

                                    for (int t = 0; t < pinfo.Tests.Count; t++)
                                    {
                                        var test = pinfo.Tests[t];
                                        logger.Info($"  Test #{t + 1}: {test.TestName} | Δt={test.GraphDt}мс, точки={test.GraphNumPoints}");
                                    }

                                    var jsonPath = Path.Combine(jsonDir, $"patient_{i:000}.json");
                                    ErgDataSerializer.SaveJson(jsonPath, pinfo);
                                    logger.Info($"Saved JSON   : {jsonPath}");

                                    if (RenderingSupport.PdfSupported)
                                    {
                                        var pdfPath = Path.Combine(pdfDir, $"patient_{i:000}.pdf");
                                        try
                                        {
                                            ErgReportBuilder.BuildPatientReport(pinfo, pdfPath, common, clinicName: opt.ClinicName, rawFilePath: rawPath);
                                            logger.Info($"Saved PDF    : {pdfPath}");
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Warn($"PDF generation failed: {ex.Message}");
                                            var reason = $"Не удалось создать PDF-отчет: {ex.Message}";
                                            RenderingSupport.DisablePdf(reason);
                                            var warning = RenderingSupport.PdfIssue ?? reason;
                                            if (!string.Equals(lastPdfWarning, warning, StringComparison.Ordinal))
                                            {
                                                logger.Warn($"PDF generation disabled: {warning}");
                                                lastPdfWarning = warning;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var warning = RenderingSupport.PdfIssue ?? "Генерация PDF отключена.";
                                        if (!string.Equals(lastPdfWarning, warning, StringComparison.Ordinal))
                                        {
                                            logger.Warn($"PDF generation skipped: {warning}");
                                            lastPdfWarning = warning;
                                        }
                                    }

                                    var docxPath = Path.Combine(docxDir, $"patient_{i:000}.docx");
                                    try
                                    {
                                        ErgReportBuilder.BuildPatientWordReport(pinfo, docxPath, common, clinicName: opt.ClinicName, rawFilePath: rawPath);
                                        logger.Info($"Saved Word   : {docxPath}");
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Warn($"Word export failed: {ex.Message}");
                                    }
                                }
                                else logger.Warn($"Patient parse warning: {perr}");
                            }
                        }
                    }

                    if (opt.Rtc)
                    {
                        var cmd = ErgProtocol.BuildRtcSet(DateTime.Now);
                        ErgIo.Write(sp, cmd, logger, "RTC SET (DT...)");
                        var ack = ErgIo.ReadChunk(sp, logger, 0, opt.QuietTimeMs, 500);
                        if (ack.Length > 0) logger.HexDump("RTC ACK raw", ack);
                        try { Console.Beep(); } catch { }
                    }

                    if (stopOnSuccess) return 0;
                }
            }
            catch (TimeoutException) { logger.Warn("Timeout."); }
            catch (Exception ex) { logger.Warn($"Attempt failed: {ex.GetType().Name}: {ex.Message}"); }

            Thread.Sleep(opt.AttemptDelayMs);
        }

        logger.Info($"Device not confirmed on {portName}.");
        return 11;
    }

    static string TrimForLog(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "<empty>";
        text = text.Replace('\r', ' ').Replace('\n', ' ');
        return text.Length > 200 ? text[..200] + "…" : text;
    }

    static int RunParseMode(CliOptions options, Logger logger)
    {
        if (string.IsNullOrWhiteSpace(options.ParseInputPath))
        {
            logger.Error("Parse mode requires --parse=<path-to-bin>");
            return 1;
        }

        var inputPath = Path.GetFullPath(options.ParseInputPath);
        if (!File.Exists(inputPath))
        {
            logger.Error($"Input file not found: {inputPath}");
            return 2;
        }

        logger.Section("Parse patient block");
        logger.Info($"Source: {inputPath}");

        var data = File.ReadAllBytes(inputPath);
        if (!ErgParser.TryParsePatientBlock(data, out var patient, out var error))
        {
            logger.Error($"Parse failed: {error}");
            return 3;
        }

        logger.Info($"Patient_ID     : {patient.PatientId}");
        logger.Info($"Animal         : {patient.Animal}");
        logger.Info($"Tests          : {patient.Tests.Count}/{patient.TotalNumTests}");
        logger.Info($"Date/Time      : {patient.TestDateTime}");
        logger.Info($"Description    : {TrimForLog(patient.Description)}");

        for (int i = 0; i < patient.Tests.Count; i++)
        {
            var test = patient.Tests[i];
            logger.Info($"  Test #{i + 1}: {test.TestName}, Δt={test.GraphDt}мс, точки={test.GraphNumPoints}");
        }

        var jsonPath = options.JsonOutputPath;
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            jsonPath = Path.ChangeExtension(inputPath, ".json");
        }
        jsonPath = Path.GetFullPath(jsonPath);
        Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
        ErgDataSerializer.SaveJson(jsonPath, patient);
        logger.Info($"JSON saved: {jsonPath}");

        if (!string.IsNullOrWhiteSpace(options.PdfOutputPath))
        {
            var pdfPath = Path.GetFullPath(options.PdfOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);
            if (RenderingSupport.PdfSupported)
            {
                try
                {
                    ErgReportBuilder.BuildPatientReport(patient, pdfPath, deviceInfo: null, clinicName: options.ClinicName, rawFilePath: inputPath);
                    logger.Info($"PDF saved : {pdfPath}");
                }
                catch (Exception ex)
                {
                    logger.Warn($"PDF export failed: {ex.Message}");
                    var reason = $"Не удалось создать PDF-отчет: {ex.Message}";
                    RenderingSupport.DisablePdf(reason);
                    logger.Warn($"PDF generation disabled: {RenderingSupport.PdfIssue ?? reason}");
                }
            }
            else
            {
                logger.Warn($"PDF export skipped: {RenderingSupport.PdfIssue ?? "Генерация PDF отключена."}");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.DocxOutputPath))
        {
            var docxPath = Path.GetFullPath(options.DocxOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(docxPath)!);
            try
            {
                ErgReportBuilder.BuildPatientWordReport(patient, docxPath, deviceInfo: null, clinicName: options.ClinicName, rawFilePath: inputPath);
                logger.Info($"Word saved: {docxPath}");
            }
            catch (Exception ex)
            {
                logger.Warn($"Word export failed: {ex.Message}");
            }
        }

        return 0;
    }

}
