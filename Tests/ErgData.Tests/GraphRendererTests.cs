using ErgData;
using Xunit;

namespace ErgData.Tests;

public class GraphRendererTests
{
    [Fact]
    public void GetNormalizedGraphs_UsesExistingNormalizedData()
    {
        var normalized = new[]
        {
            new[] { 1.2, -3.4 }
        };

        var eye = new EyeData
        {
            GraphCount = 1,
            GraphsNormalized = normalized
        };

        var test = new ErgTest
        {
            GraphNumPoints = 2,
            GraphDiscrPerMkV = 7,
            RightEye = eye,
            LeftEye = new EyeData()
        };

        var result = GraphRenderer.GetNormalizedGraphs(test, eye);

        Assert.Same(normalized, result);
    }

    [Fact]
    public void GetNormalizedGraphs_ComputesFromSamples()
    {
        var eye = new EyeData
        {
            GraphCount = 1,
            GraphSamples = new[]
            {
                new short[] { 10, -20, 30 }
            }
        };

        var test = new ErgTest
        {
            GraphNumPoints = 2,
            GraphDiscrPerMkV = 2,
            RightEye = eye,
            LeftEye = new EyeData()
        };

        var result = GraphRenderer.GetNormalizedGraphs(test, eye);

        Assert.NotNull(result);
        var samples = Assert.Single(result!);
        Assert.Equal(new[] { 5d, -10d }, samples);
    }

    [Fact]
    public void GetNormalizedGraphs_ReturnsNull_WhenNoSamples()
    {
        var eye = new EyeData { GraphCount = 0 };
        var test = new ErgTest
        {
            GraphNumPoints = 0,
            GraphDiscrPerMkV = 1,
            RightEye = eye,
            LeftEye = new EyeData()
        };

        var result = GraphRenderer.GetNormalizedGraphs(test, eye);

        Assert.Null(result);
    }
}
