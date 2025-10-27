using System;
using System.IO;
using System.Linq;
using ErgData;
using Xunit;

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
}
