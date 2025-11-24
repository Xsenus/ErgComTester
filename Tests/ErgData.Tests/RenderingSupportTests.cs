using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using ErgData;
using Xunit;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public class RenderingSupportTests
{
    [Fact]
    public void LegacyModeCanBeForcedViaEnvironmentVariable()
    {
        var original = Environment.GetEnvironmentVariable("ERG_FORCE_LEGACY_RENDERING");
        try
        {
            Environment.SetEnvironmentVariable("ERG_FORCE_LEGACY_RENDERING", "1");
            RenderingSupport.Reload();

            Assert.True(RenderingSupport.UseLegacyPdfGeneration);
            Assert.True(RenderingSupport.UseLegacyGraphRendering);
            Assert.False(string.IsNullOrWhiteSpace(RenderingSupport.LegacyRenderingNotice));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ERG_FORCE_LEGACY_RENDERING", original);
            RenderingSupport.Reload(ReportRenderingMode.Automatic);
        }
    }

    [Fact]
    public void GeneratesPdfInLegacyModeOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var original = Environment.GetEnvironmentVariable("ERG_FORCE_LEGACY_RENDERING");
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
        try
        {
            Environment.SetEnvironmentVariable("ERG_FORCE_LEGACY_RENDERING", "1");
            RenderingSupport.Reload();

            var patient = new ErgPatient
            {
                PatientId = 42,
                Animal = AnimalKind.Dog,
                TestDateTime = "01.01.2024 12:00",
                TotalNumTests = 1,
                Tests =
                {
                    new ErgTest
                    {
                        TestName = "Flash",
                        GraphNumPoints = 10,
                        GraphDt = 1,
                        GraphDiscrPerMkV = 1,
                        GraphFlashPosition = 5,
                        GraphXValueStep = 1,
                        GraphXLineStep = 1,
                        GraphXScaleMin = 0,
                        GraphXScaleMax = 9,
                        GraphYValueStep = 1,
                        GraphYLineStep = 1,
                        GraphYScaleMin = -5,
                        GraphYScaleMax = 5,
                        AWaveExists = true,
                        AWaveMsNormalMin = 1,
                        AWaveMsNormalMax = 3,
                        AWaveMkVNormalMin = 5,
                        AWaveMkVNormalMax = 15,
                        BWaveMsNormalMin = 5,
                        BWaveMsNormalMax = 7,
                        BWaveMkVNormalMin = 20,
                        BWaveMkVNormalMax = 40,
                        RightEye = new EyeData
                        {
                            QualityIndex = 2,
                            GraphCount = 1,
                            AWaveMs = new ushort?[] { 2 },
                            AWaveMkV = new uint?[] { 10 },
                            BWaveMs = new ushort?[] { 6 },
                            BWaveMkV = new uint?[] { 30 },
                            GraphsNormalized = new[]
                            {
                                Enumerable.Range(0, 10).Select(i => Math.Sin(i / 2.0)).ToArray()
                            }
                        },
                        LeftEye = new EyeData
                        {
                            QualityIndex = 3,
                            GraphCount = 1,
                            AWaveMs = new ushort?[] { 2 },
                            AWaveMkV = new uint?[] { 12 },
                            BWaveMs = new ushort?[] { 6 },
                            BWaveMkV = new uint?[] { 35 },
                            GraphsNormalized = new[]
                            {
                                Enumerable.Range(0, 10).Select(i => Math.Cos(i / 2.0)).ToArray()
                            }
                        }
                    }
                }
            };

            var deviceInfo = new CommonInfo
            {
                ReportName = "Legacy layout",
                DeviceName = "ERG-100",
                SoftwareRev = "2.3"
            };

            ErgReportBuilder.BuildPatientReport(patient, outputPath, deviceInfo, "Ветклиника");

            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            Environment.SetEnvironmentVariable("ERG_FORCE_LEGACY_RENDERING", original);
            RenderingSupport.Reload(ReportRenderingMode.Automatic);
        }
    }

    [Fact]
    public void GeneratesClientWordDocumentWithoutValidationErrors()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".docx");
        try
        {
            var patient = new ErgPatient
            {
                PatientId = 99,
                Animal = AnimalKind.Cat,
                TestDateTime = "02.02.2024 09:30",
                TotalNumTests = 1,
                Description = "Краткое описание\nСостояние стабильное",
                Tests =
                {
                    new ErgTest
                    {
                        TestName = "Flash",
                        GraphNumPoints = 10,
                        GraphDt = 1,
                        GraphDiscrPerMkV = 1,
                        GraphFlashPosition = 5,
                        GraphXValueStep = 1,
                        GraphXLineStep = 1,
                        GraphXScaleMin = 0,
                        GraphXScaleMax = 9,
                        GraphYValueStep = 1,
                        GraphYLineStep = 1,
                        GraphYScaleMin = -5,
                        GraphYScaleMax = 5,
                        AWaveExists = true,
                        AWaveMsNormalMin = 1,
                        AWaveMsNormalMax = 3,
                        AWaveMkVNormalMin = 5,
                        AWaveMkVNormalMax = 15,
                        BWaveMsNormalMin = 5,
                        BWaveMsNormalMax = 7,
                        BWaveMkVNormalMin = 20,
                        BWaveMkVNormalMax = 40,
                        RightEye = new EyeData
                        {
                            QualityIndex = 2,
                            GraphCount = 1,
                            AWaveMs = new ushort?[] { 2 },
                            AWaveMkV = new uint?[] { 10 },
                            BWaveMs = new ushort?[] { 6 },
                            BWaveMkV = new uint?[] { 30 },
                            GraphsNormalized = new[]
                            {
                                Enumerable.Range(0, 10).Select(i => Math.Sin(i / 2.0)).ToArray()
                            }
                        },
                        LeftEye = new EyeData
                        {
                            QualityIndex = 3,
                            GraphCount = 1,
                            AWaveMs = new ushort?[] { 2 },
                            AWaveMkV = new uint?[] { 12 },
                            BWaveMs = new ushort?[] { 6 },
                            BWaveMkV = new uint?[] { 35 },
                            GraphsNormalized = new[]
                            {
                                Enumerable.Range(0, 10).Select(i => Math.Cos(i / 2.0)).ToArray()
                            }
                        }

                    }
                }
            };

            var deviceInfo = new CommonInfo
            {
                ReportName = "Тестовый отчет",
                DeviceName = string.Empty,
                SoftwareRev = "1.0"
            };

            ErgReportBuilder.BuildPatientWordReport(patient, outputPath, deviceInfo, "Ветклиника");

            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);

            using (var archive = ZipFile.OpenRead(outputPath))
            {
                var entry = archive.GetEntry("[Content_Types].xml");
                Assert.NotNull(entry);

                XDocument manifest;
                using (var stream = entry!.Open())
                {
                    manifest = XDocument.Load(stream);
                }

                var contentNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types");
                var contentRoot = manifest.Root ?? throw new InvalidOperationException("Файл [Content_Types].xml поврежден.");

                var xmlDefault = contentRoot.Elements(contentNs + "Default").FirstOrDefault(e => string.Equals((string?)e.Attribute("Extension"), "xml", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(xmlDefault);
                Assert.Equal("application/xml", (string?)xmlDefault!.Attribute("ContentType"));

                var mainOverride = contentRoot.Elements(contentNs + "Override").FirstOrDefault(e => string.Equals((string?)e.Attribute("PartName"), "/word/document.xml", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(mainOverride);
                Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml", (string?)mainOverride!.Attribute("ContentType"));

                var coreOverride = contentRoot.Elements(contentNs + "Override").FirstOrDefault(e => string.Equals((string?)e.Attribute("PartName"), "/docProps/core.xml", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(coreOverride);
                Assert.Equal("application/vnd.openxmlformats-package.core-properties+xml", (string?)coreOverride!.Attribute("ContentType"));

                var appOverride = contentRoot.Elements(contentNs + "Override").FirstOrDefault(e => string.Equals((string?)e.Attribute("PartName"), "/docProps/app.xml", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(appOverride);
                Assert.Equal("application/vnd.openxmlformats-officedocument.extended-properties+xml", (string?)appOverride!.Attribute("ContentType"));

                Assert.NotNull(archive.GetEntry("docProps/core.xml"));
                Assert.NotNull(archive.GetEntry("docProps/app.xml"));

                var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
                var packageRelsEntry = archive.GetEntry("_rels/.rels");
                Assert.NotNull(packageRelsEntry);

                using (var relStream = packageRelsEntry!.Open())
                {
                    var relDoc = XDocument.Load(relStream);
                    var relRoot = relDoc.Root ?? throw new InvalidOperationException("Файл _rels/.rels поврежден.");
                    var relationships = relRoot.Elements(relsNs + "Relationship").ToList();
                    Assert.Contains(relationships, rel =>
                        string.Equals((string?)rel.Attribute("Type"), "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(NormalizeRelationshipTarget((string?)rel.Attribute("Target")), "word/document.xml", StringComparison.OrdinalIgnoreCase));

                    Assert.Contains(relationships, rel =>
                        string.Equals((string?)rel.Attribute("Type"), "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(NormalizeRelationshipTarget((string?)rel.Attribute("Target")), "docProps/core.xml", StringComparison.OrdinalIgnoreCase));

                    Assert.Contains(relationships, rel =>
                        string.Equals((string?)rel.Attribute("Type"), "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(NormalizeRelationshipTarget((string?)rel.Attribute("Target")), "docProps/app.xml", StringComparison.OrdinalIgnoreCase));
                }

                var documentRelsEntry = archive.GetEntry("word/_rels/document.xml.rels");
                if (documentRelsEntry != null)
                {
                    using var docRelStream = documentRelsEntry.Open();
                    var docRelDoc = XDocument.Load(docRelStream);
                    var docRelRoot = docRelDoc.Root ?? throw new InvalidOperationException("Файл word/_rels/document.xml.rels поврежден.");
                    foreach (var relationship in docRelRoot.Elements(relsNs + "Relationship"))
                    {
                        var target = (string?)relationship.Attribute("Target");
                        if (string.IsNullOrWhiteSpace(target))
                            continue;

                        var resolved = ResolveRelationshipTarget(target);
                        Assert.NotNull(archive.GetEntry(resolved));
        }
    }

    [Fact]
    public void ClientWordReportShowsPlaceholdersWhenOnlySentinelsProvided()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".docx");
        try
        {
            var patient = new ErgPatient
            {
                PatientId = 5,
                Animal = AnimalKind.Dog,
                TestDateTime = "03.03.2024 10:00",
                TotalNumTests = 1,
                Tests =
                {
                    new ErgTest
                    {
                        TestName = "Flash",
                        GraphNumPoints = 5,
                        GraphDt = 1,
                        GraphDiscrPerMkV = 1,
                        GraphFlashPosition = 2,
                        GraphXValueStep = 1,
                        GraphXLineStep = 1,
                        GraphXScaleMin = 0,
                        GraphXScaleMax = 4,
                        GraphYValueStep = 1,
                        GraphYLineStep = 1,
                        GraphYScaleMin = -2,
                        GraphYScaleMax = 2,
                        AWaveExists = true,
                        AWaveMsNormalMin = 1,
                        AWaveMsNormalMax = 3,
                        AWaveMkVNormalMin = 5,
                        AWaveMkVNormalMax = 15,
                        BWaveMsNormalMin = 5,
                        BWaveMsNormalMax = 7,
                        BWaveMkVNormalMin = 20,
                        BWaveMkVNormalMax = 40,
                        RightEye = new EyeData
                        {
                            QualityIndex = 2,
                            GraphCount = 1,
                            AWaveMs = new ushort?[] { byte.MaxValue },
                            AWaveMkV = new uint?[] { ushort.MaxValue },
                            BWaveMs = new ushort?[] { byte.MaxValue },
                            BWaveMkV = new uint?[] { ushort.MaxValue },
                            GraphsNormalized = new[] { Enumerable.Range(0, 5).Select(i => 0.0).ToArray() }
                        },
                        LeftEye = new EyeData
                        {
                            QualityIndex = 2,
                            GraphCount = 1,
                            AWaveMs = new ushort?[] { byte.MaxValue },
                            AWaveMkV = new uint?[] { ushort.MaxValue },
                            BWaveMs = new ushort?[] { byte.MaxValue },
                            BWaveMkV = new uint?[] { ushort.MaxValue },
                            GraphsNormalized = new[] { Enumerable.Range(0, 5).Select(i => 0.0).ToArray() }
                        }
                    }
                }
            };

            var deviceInfo = new CommonInfo
            {
                ReportName = "Placeholder check",
                DeviceName = "ERG-100",
                SoftwareRev = "2.3"
            };

            ErgReportBuilder.BuildPatientWordReport(patient, outputPath, deviceInfo, "Клиника");

            using var document = WordprocessingDocument.Open(outputPath, false);
            var text = document.MainDocumentPart?.Document?.InnerText;

            Assert.Contains("Замер #1", text);
            Assert.Contains("- -", text);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void ZeroAndSentinelMeasurementsRenderAsPlaceholders()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".docx");
        try
        {
            var patient = new ErgPatient
            {
                PatientId = 15,
                Animal = AnimalKind.Cat,
                TestDateTime = "01.01.2025 12:00",
                TotalNumTests = 1,
                Tests =
                {
                    new ErgTest
                    {
                        TestName = "FLAT",
                        GraphNumPoints = 10,
                        GraphDt = 1,
                        GraphDiscrPerMkV = 1,
                        GraphFlashPosition = 5,
                        GraphXValueStep = 1,
                        GraphXLineStep = 1,
                        GraphXScaleMin = 0,
                        GraphXScaleMax = 9,
                        GraphYValueStep = 1,
                        GraphYLineStep = 1,
                        GraphYScaleMin = -5,
                        GraphYScaleMax = 5,
                        AWaveExists = true,
                        AWaveMsNormalMin = 1,
                        AWaveMsNormalMax = 3,
                        AWaveMkVNormalMin = 5,
                        AWaveMkVNormalMax = 15,
                        BWaveMsNormalMin = 5,
                        BWaveMsNormalMax = 7,
                        BWaveMkVNormalMin = 20,
                        BWaveMkVNormalMax = 40,
                        RightEye = new EyeData
                        {
                            QualityIndex = 2,
                            GraphCount = 1,
                            AWaveMs = new ushort?[] { 0, byte.MaxValue },
                            AWaveMkV = new uint?[] { 0, ushort.MaxValue },
                            BWaveMs = new ushort?[] { 0 },
                            BWaveMkV = new uint?[] { 0 },
                            GraphsNormalized = new[]
                            {
                                Enumerable.Range(0, 10).Select(i => 0d).ToArray()
                            }
                        },
                        LeftEye = new EyeData
                        {
                            QualityIndex = 2,
                            GraphCount = 1,
                            AWaveMs = new ushort?[] { 0 },
                            AWaveMkV = new uint?[] { 0 },
                            BWaveMs = new ushort?[] { 0 },
                            BWaveMkV = new uint?[] { 0 },
                            GraphsNormalized = new[]
                            {
                                Enumerable.Range(0, 10).Select(i => 0d).ToArray()
                            }
                        }
                    }
                }
            };

            ErgReportBuilder.BuildPatientWordReport(patient, outputPath, null, "Клиника");

            Assert.True(File.Exists(outputPath));

            using var document = WordprocessingDocument.Open(outputPath, false);
            var text = string.Concat(document.MainDocumentPart!.Document.Body!.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                .Select(t => t.Text));

            Assert.Contains("- -", text);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }
}

            using var document = WordprocessingDocument.Open(outputPath, false);
            var validator = new OpenXmlValidator();
            var errors = validator.Validate(document).ToList();
            Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(e => $"{e.Path}: {e.Description}")));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void WordReport_UsesDoubleDashForMissingMeasurements()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".docx");
        try
        {
            var patient = new ErgPatient
            {
                PatientId = 101,
                Animal = AnimalKind.Cat,
                TestDateTime = "03.03.2024 10:00",
                TotalNumTests = 1,
                Tests =
                {
                    new ErgTest
                    {
                        TestName = "Flash",
                        GraphNumPoints = 10,
                        GraphDt = 1,
                        GraphDiscrPerMkV = 1,
                        GraphFlashPosition = 5,
                        GraphXValueStep = 1,
                        GraphXLineStep = 1,
                        GraphXScaleMin = 0,
                        GraphXScaleMax = 9,
                        GraphYValueStep = 1,
                        GraphYLineStep = 1,
                        GraphYScaleMin = -5,
                        GraphYScaleMax = 5,
                        AWaveExists = true,
                        RightEye = new EyeData
                        {
                            QualityIndex = 2,
                            ValueCount = 1,
                            AWaveMs = new ushort?[] { 255 },
                            AWaveMkV = new uint?[] { 65535u },
                            BWaveMs = new ushort?[] { 255 },
                            BWaveMkV = new uint?[] { 65535u },
                            GraphCount = 0
                        },
                        LeftEye = new EyeData
                        {
                            QualityIndex = 1,
                            ValueCount = 1,
                            AWaveMs = new ushort?[] { 255 },
                            AWaveMkV = new uint?[] { 65535u },
                            BWaveMs = new ushort?[] { 255 },
                            BWaveMkV = new uint?[] { 65535u },
                            GraphCount = 0
                        }
                    }
                }
            };

            ErgReportBuilder.BuildPatientWordReport(patient, outputPath);

            using var document = WordprocessingDocument.Open(outputPath, false);
            var text = document.MainDocumentPart!.Document.InnerText;

            Assert.Contains("- -", text);
            Assert.DoesNotContain("255 мс", text);
            Assert.DoesNotContain("65535 мкВ", text);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void ManualRenderingModeOverridesDetection()
    {
        var original = Environment.GetEnvironmentVariable("ERG_FORCE_LEGACY_RENDERING");
        try
        {
            Environment.SetEnvironmentVariable("ERG_FORCE_LEGACY_RENDERING", "0");
            RenderingSupport.Reload(ReportRenderingMode.Legacy);
            Assert.Equal(ReportRenderingMode.Legacy, RenderingSupport.Mode);
            Assert.True(RenderingSupport.UseLegacyPdfGeneration);
            Assert.True(RenderingSupport.UseLegacyGraphRendering);
            Assert.False(string.IsNullOrWhiteSpace(RenderingSupport.LegacyRenderingNotice));

            RenderingSupport.Reload(ReportRenderingMode.Modern);
            Assert.Equal(ReportRenderingMode.Modern, RenderingSupport.Mode);
            Assert.False(RenderingSupport.UseLegacyPdfGeneration);
            Assert.False(RenderingSupport.UseLegacyGraphRendering);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ERG_FORCE_LEGACY_RENDERING", original);
            RenderingSupport.Reload(ReportRenderingMode.Automatic);
        }
    }

    private static string NormalizeRelationshipTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return string.Empty;

        var trimmed = target.Replace('\\', '/').Trim();
        if (trimmed.StartsWith("../", StringComparison.Ordinal))
            trimmed = trimmed.Substring(3);

        return trimmed.TrimStart('/');
    }

    private static string ResolveRelationshipTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return string.Empty;

        var normalized = target.Replace('\\', '/').Trim();
        if (normalized.StartsWith("../", StringComparison.Ordinal))
            return "word/" + normalized.Substring(3);

        if (normalized.StartsWith("/", StringComparison.Ordinal))
            return normalized.TrimStart('/');

        return "word/" + normalized.TrimStart('/');
    }
}
