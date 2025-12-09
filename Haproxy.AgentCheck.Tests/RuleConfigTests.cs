using Lucca.Infra.Haproxy.AgentCheck.Config;
using Xunit;

namespace Lucca.Infra.Haproxy.AgentCheck.Tests;

public class RuleConfigTests
{
    [Fact]
    public void RuleConfig_WithWeightRule_IsValid()
    {
        var rule = new RuleConfig
        {
            Name = "CPU",
            Source = RuleSource.System,
            Weight = new WeightRule()
        };

        Assert.True(rule.IsValid());
    }

    [Fact]
    public void RuleConfig_WithFailureRule_IsValid()
    {
        var rule = new RuleConfig
        {
            Name = "CPU",
            Source = RuleSource.System,
            Failure = new FailureRule
            {
                EnterThreshold = 80,
                LeaveThreshold = 60
            }
        };

        Assert.True(rule.IsValid());
    }

    [Fact]
    public void RuleConfig_WithBothRules_IsValid()
    {
        var rule = new RuleConfig
        {
            Name = "CPU",
            Source = RuleSource.System,
            Weight = new WeightRule(),
            Failure = new FailureRule
            {
                EnterThreshold = 80,
                LeaveThreshold = 60
            }
        };

        Assert.True(rule.IsValid());
    }

    [Fact]
    public void RuleConfig_WithNoRules_IsNotValid()
    {
        var rule = new RuleConfig
        {
            Name = "CPU",
            Source = RuleSource.System
        };

        Assert.False(rule.IsValid());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RuleConfig_WithInvalidName_IsNotValid(string? name)
    {
        var rule = new RuleConfig
        {
            Name = name!,
            Source = RuleSource.System,
            Weight = new WeightRule()
        };

        Assert.False(rule.IsValid());
    }

    [Fact]
    public void RulesConfig_AllValid_ReturnsTrue()
    {
        var rules = new RulesConfig
        {
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Weight = new WeightRule()
            },
            new RuleConfig
            {
                Name = "IisRequests",
                Source = RuleSource.System,
                Failure = new FailureRule { EnterThreshold = 100, LeaveThreshold = 50 }
            }
        };

        Assert.True(rules.AreValid());
    }

    [Fact]
    public void RulesConfig_OneInvalid_ReturnsFalse()
    {
        var rules = new RulesConfig
        {
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Weight = new WeightRule()
            },
            new RuleConfig
            {
                Name = "Invalid",
                Source = RuleSource.System
                // No Weight or Failure rule
            }
        };

        Assert.False(rules.AreValid());
    }

    [Fact]
    public void RulesConfig_Empty_ReturnsTrue()
    {
        var rules = new RulesConfig();

        Assert.True(rules.AreValid());
    }
}
