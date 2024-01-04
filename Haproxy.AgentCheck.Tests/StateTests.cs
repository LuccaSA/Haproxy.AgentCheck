using Lucca.Infra.Haproxy.AgentCheck.Config;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Lucca.Infra.Haproxy.AgentCheck.Tests;

public class StateTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Check_that_when_no_rule_is_defined_then_state_is_down()
    {
        var serviceProvider = CreateServiceProvider();
        var sut = serviceProvider.GetRequiredService<State>();
        Assert.False(sut.IsUp);
    }

    [Theory]
    [MemberData(memberName: nameof(GetLinearResponseTestData))]
    public void Check_linear_weight_rule(WeightRuleData data)
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Weight = new()
                {
                    SystemResponse = SystemResponse.Linear,
                    MinValue = data.MinValue,
                    MaxValue = data.MaxValue,
                    MinWeight = data.MinWeight,
                    MaxWeight = data.MaxWeight
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = data.Value });
        Assert.True(sut.IsUp);
        Assert.Equal(data.ExpectedWeight, sut.Weight);
    }

    [Theory]
    [MemberData(memberName: nameof(GetFirstOrderResponseTestData))]
    public void Check_firstOrder_weight_rule(WeightRuleData data)
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Weight = new()
                {
                    SystemResponse = SystemResponse.FirstOrder,
                    MinValue = data.MinValue,
                    MaxValue = data.MaxValue,
                    MinWeight = data.MinWeight,
                    MaxWeight = data.MaxWeight
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = data.Value });
        Assert.True(sut.IsUp);
        Assert.Equal(data.ExpectedWeight, sut.Weight);
    }

    [Fact]
    public void Check_that_the_lowest_weight_is_used()
    {
        var serviceProvider = CreateServiceProvider(
            new()
            {
                Name = "CPU",
                Source = RuleSource.System,
                Weight = new()
                {
                    SystemResponse = SystemResponse.Linear
                }
            },
            new()
            {
                Name = "IisRequests",
                Source = RuleSource.System,
                Weight = new()
                {
                    SystemResponse = SystemResponse.Linear
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = 100, IisRequests = 0 });
        Assert.True(sut.IsUp);
        Assert.Equal(0, sut.Weight);
    }

    [Fact]
    public void Check_that_given_metric_below_threshold_state_is_up()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Failure = new()
                {
                    EnterThreshold = 60,
                    LeaveThreshold = 40
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = 10 });
        Assert.True(sut.IsUp);
        Assert.DoesNotContain("System/CPU", sut.BrokenCircuitsBreakers);
    }

    [Fact]
    public void Check_that_given_metric_above_threshold_state_is_down()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Failure = new()
                {
                    EnterThreshold = 60,
                    LeaveThreshold = 40
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = 70 });
        Assert.False(sut.IsUp);
        Assert.Contains("System/CPU", sut.BrokenCircuitsBreakers);
    }

    [Fact]
    public void Check_that_given_one_metric_above_threshold_state_is_down()
    {
        var serviceProvider = CreateServiceProvider(
            new()
            {
                Name = "CPU",
                Source = RuleSource.System,
                Failure = new()
                {
                    EnterThreshold = 60,
                    LeaveThreshold = 40
                }
            },
            new()
            {
                Name = "IisRequests",
                Source = RuleSource.System,
                Failure = new()
                {
                    EnterThreshold = 10,
                    LeaveThreshold = 10
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = 70 });
        Assert.False(sut.IsUp);
        Assert.Contains("System/CPU", sut.BrokenCircuitsBreakers);
        Assert.DoesNotContain("System/IisRequests", sut.BrokenCircuitsBreakers);
    }

    [Fact]
    public void Check_that_given_broken_circuit_breaker_with_metric_between_thresholds_state_is_down()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Failure = new()
                {
                    EnterThreshold = 60,
                    LeaveThreshold = 40
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = 70 });
        sut.UpdateState(new SystemState { CpuPercent = 50 });
        Assert.False(sut.IsUp);
        Assert.Contains("System/CPU", sut.BrokenCircuitsBreakers);
    }

    [Fact]
    public void Check_that_given_broken_circuit_breaker_with_metric_below_threshold_state_is_up()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Failure = new()
                {
                    EnterThreshold = 60,
                    LeaveThreshold = 40
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = 70 });
        sut.UpdateState(new SystemState { CpuPercent = 30 });
        Assert.True(sut.IsUp);
        Assert.DoesNotContain("System/CPU", sut.BrokenCircuitsBreakers);
    }

    [Fact]
    public void Check_that_given_broken_circuit_breaker_with_metric_below_threshold_before_delay_state_is_down()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Failure = new()
                {
                    EnterThreshold = 60,
                    LeaveThreshold = 40,
                    LeaveAfter = TimeSpan.FromSeconds(10)
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = 70 });
        sut.UpdateState(new SystemState { CpuPercent = 30 });
        Assert.False(sut.IsUp);
        Assert.Contains("System/CPU", sut.BrokenCircuitsBreakers);
    }

    [Fact]
    public void Check_that_given_broken_circuit_breaker_with_metric_below_threshold_after_delay_state_is_up()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Failure = new()
                {
                    EnterThreshold = 60,
                    LeaveThreshold = 40,
                    LeaveAfter = TimeSpan.FromSeconds(10)
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = 70 });
        sut.UpdateState(new SystemState { CpuPercent = 30 });
        serviceProvider.GetRequiredService<TestTimeProvider>().MoveTime(TimeSpan.FromSeconds(10));
        sut.UpdateState(new SystemState { CpuPercent = 30 });
        Assert.True(sut.IsUp);
        Assert.DoesNotContain("System/CPU", sut.BrokenCircuitsBreakers);
    }

    [Fact]
    public void Check_that_given_fixed_circuit_breaker_with_metric_not_below_threshold_after_delay_state_is_down()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Failure = new()
                {
                    EnterThreshold = 60,
                    LeaveThreshold = 40,
                    LeaveAfter = TimeSpan.FromSeconds(10)
                }
            });
        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new SystemState { CpuPercent = 70 });
        sut.UpdateState(new SystemState { CpuPercent = 30 });
        sut.UpdateState(new SystemState { CpuPercent = 50 });
        serviceProvider.GetRequiredService<TestTimeProvider>().MoveTime(TimeSpan.FromSeconds(10));
        Assert.False(sut.IsUp);
        Assert.Contains("System/CPU", sut.BrokenCircuitsBreakers);
    }

    public static TheoryData<WeightRuleData> GetLinearResponseTestData()
    {
        return new TheoryData<WeightRuleData>
        {
            new(0, 100, 0, 100, 0, 100),
            new(50, 50, 0, 100, 0, 100),
            new(100, 0, 0, 100, 0, 100),

            new(0, 80, 0, 100, 10, 80),
            new(50, 45, 0, 100, 10, 80),
            new(100, 10, 0, 100, 10, 80),

            new(0, 100, 10, 80, 0, 100),
            new(10, 100, 10, 80, 0, 100),
            new(50, 42, 10, 80, 0, 100),
            new(80, 0, 10, 80, 0, 100),
            new(100, 0, 10, 80, 0, 100)
        };
    }

    public static TheoryData<WeightRuleData> GetFirstOrderResponseTestData()
    {
        return new TheoryData<WeightRuleData>
        {
            new(0, 100, 0, 100, 0, 100),
            new(50, 9, 0, 100, 0, 100),
            new(100, 0, 0, 100, 0, 100),

            new(0, 80, 0, 100, 10, 80),
            new(50, 17, 0, 100, 10, 80),
            new(100, 10, 0, 100, 10, 80),

            new(0, 100, 10, 80, 0, 100),
            new(10, 100, 10, 80, 0, 100),
            new(45, 9, 10, 80, 0, 100),
            new(80, 0, 10, 80, 0, 100),
            new(100, 0, 10, 80, 0, 100)
        };
    }

    private IServiceProvider CreateServiceProvider(params RuleConfig[] rules)
    {
        return new ServiceCollection()
            .AddOptions()
            .Configure<RulesConfig>(o => { o.AddRange(rules); })
            .AddFakeLogging(o => o.OutputSink = testOutputHelper.WriteLine)
            .AddSingleton<TestTimeProvider>()
            .AddSingleton<TimeProvider>(p => p.GetRequiredService<TestTimeProvider>())
            .AddSingleton<State>()
            .BuildServiceProvider();
    }
}

public record WeightRuleData(int Value, int ExpectedWeight, int MinValue, int MaxValue, int MinWeight, int MaxWeight)
{
    public override string ToString()
    {
        return $"Value: {Value,3} ({MinValue,3};{MaxValue,3}) -> Weight: {ExpectedWeight,3} ({MinWeight,3};{MaxWeight,3})";
    }
}

public class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _now = System.GetUtcNow();

    public override DateTimeOffset GetUtcNow() => _now;

    public void MoveTime(TimeSpan timeSpan)
    {
        _now = _now.Add(timeSpan);
    }
}
