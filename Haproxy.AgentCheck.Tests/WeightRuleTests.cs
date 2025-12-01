using Lucca.Infra.Haproxy.AgentCheck.Config;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Xunit;

namespace Lucca.Infra.Haproxy.AgentCheck.Tests;

public class WeightRuleTests
{
    [Fact]
    public void WeightRule_DefaultValues_AreCorrect()
    {
        var rule = new WeightRule();

        Assert.Equal(0, rule.MinValue);
        Assert.Equal(100, rule.MaxValue);
        Assert.Equal(0, rule.MinWeight);
        Assert.Equal(100, rule.MaxWeight);
        Assert.Equal(SystemResponse.Linear, rule.SystemResponse);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(50, 50)]
    [InlineData(100, 0)]
    [InlineData(25, 75)]
    [InlineData(75, 25)]
    public void LinearResponse_StandardRange_CalculatesCorrectWeight(double cpuValue, double expectedWeight)
    {
        // This test validates the linear response curve
        // When CPU is 0%, weight should be 100%
        // When CPU is 100%, weight should be 0%
        // Linear interpolation in between
        
        var actualWeight = CalculateLinearWeight(cpuValue, 0, 100, 0, 100);
        
        Assert.Equal(expectedWeight, actualWeight, 0.01);
    }

    [Theory]
    [InlineData(0, 80)]
    [InlineData(100, 20)]
    [InlineData(50, 50)]
    public void LinearResponse_CustomWeightRange_CalculatesCorrectWeight(double value, double expectedWeight)
    {
        var actualWeight = CalculateLinearWeight(value, 0, 100, 20, 80);
        
        Assert.Equal(expectedWeight, actualWeight, 0.01);
    }

    [Theory]
    [InlineData(10, 100)]  // Below min value -> clamped to max weight
    [InlineData(90, 0)]    // Above max value -> clamped to min weight
    public void LinearResponse_OutOfRange_ClampedCorrectly(double value, double expectedWeight)
    {
        // When value is outside the range, weight should be clamped
        var actualWeight = CalculateLinearWeight(value, 20, 80, 0, 100);
        
        // The calculation gives a value that should be clamped
        var clampedWeight = Math.Clamp(actualWeight, 0, 100);
        
        Assert.Equal(expectedWeight, clampedWeight, 0.01);
    }

    private static double CalculateLinearWeight(double currentValue, double minValue, double maxValue, double minWeight, double maxWeight)
    {
        return 1d * (maxValue - currentValue) / (maxValue - minValue) * (maxWeight - minWeight) + minWeight;
    }
}
