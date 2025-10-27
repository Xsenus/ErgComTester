using System;
using System.IO;
using System.Linq;
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
            RenderingSupport.Reload();
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
            RenderingSupport.Reload();
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
}
