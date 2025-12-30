using Lucca.Infra.Haproxy.AgentCheck.Config;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lucca.Infra.Haproxy.AgentCheck.Tests;

public class CountersStateTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void UpdateState_WithCounterValue_UpdatesWeight()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "custom-counter",
                Source = RuleSource.Counters,
                Weight = new WeightRule
                {
                    SystemResponse = SystemResponse.Linear,
                    MinValue = 0,
                    MaxValue = 100,
                    MinWeight = 0,
                    MaxWeight = 100
                }
            });

        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new CountersState
        {
            Values = new Dictionary<string, double> { ["custom-counter"] = 50 }
        });

        Assert.True(sut.IsUp);
        Assert.Equal(50, sut.Weight, 0.01);
    }

    [Fact]
    public void UpdateState_WithCounterAboveThreshold_BreaksCircuit()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "error-rate",
                Source = RuleSource.Counters,
                Failure = new FailureRule
                {
                    EnterThreshold = 10,
                    LeaveThreshold = 5
                }
            });

        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new CountersState
        {
            Values = new Dictionary<string, double> { ["error-rate"] = 15 }
        });

        Assert.False(sut.IsUp);
        Assert.Contains("Counters/error-rate", sut.BrokenCircuitsBreakers);
    }

    [Fact]
    public void UpdateState_WhenCounterRemoved_RemovesBrokenCircuit()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "temp-counter",
                Source = RuleSource.Counters,
                Failure = new FailureRule
                {
                    EnterThreshold = 10,
                    LeaveThreshold = 5
                }
            });

        var sut = serviceProvider.GetRequiredService<State>();
        
        // First update with counter above threshold
        sut.UpdateState(new CountersState
        {
            Values = new Dictionary<string, double> { ["temp-counter"] = 15 }
        });
        Assert.Contains("Counters/temp-counter", sut.BrokenCircuitsBreakers);

        // Second update without the counter
        sut.UpdateState(new CountersState
        {
            Values = new Dictionary<string, double>()
        });
        
        Assert.DoesNotContain("Counters/temp-counter", sut.BrokenCircuitsBreakers);
    }

    [Fact]
    public void UpdateState_WithMultipleCounters_UsesLowestWeight()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "counter1",
                Source = RuleSource.Counters,
                Weight = new WeightRule
                {
                    SystemResponse = SystemResponse.Linear,
                    MinValue = 0,
                    MaxValue = 100,
                    MinWeight = 0,
                    MaxWeight = 100
                }
            },
            new RuleConfig
            {
                Name = "counter2",
                Source = RuleSource.Counters,
                Weight = new WeightRule
                {
                    SystemResponse = SystemResponse.Linear,
                    MinValue = 0,
                    MaxValue = 100,
                    MinWeight = 0,
                    MaxWeight = 100
                }
            });

        var sut = serviceProvider.GetRequiredService<State>();
        sut.UpdateState(new CountersState
        {
            Values = new Dictionary<string, double>
            {
                ["counter1"] = 20,  // Weight would be 80
                ["counter2"] = 90   // Weight would be 10
            }
        });

        Assert.True(sut.IsUp);
        Assert.Equal(10, sut.Weight, 0.01);
    }

    [Fact]
    public void UpdateState_CombineSystemAndCounters_UsesLowestWeight()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Weight = new WeightRule
                {
                    SystemResponse = SystemResponse.Linear,
                    MinValue = 0,
                    MaxValue = 100,
                    MinWeight = 0,
                    MaxWeight = 100
                }
            },
            new RuleConfig
            {
                Name = "custom-metric",
                Source = RuleSource.Counters,
                Weight = new WeightRule
                {
                    SystemResponse = SystemResponse.Linear,
                    MinValue = 0,
                    MaxValue = 100,
                    MinWeight = 0,
                    MaxWeight = 100
                }
            });

        var sut = serviceProvider.GetRequiredService<State>();
        
        // CPU at 30% -> weight 70
        sut.UpdateState(new SystemState { CpuPercent = 30 });
        
        // Custom metric at 80 -> weight 20
        sut.UpdateState(new CountersState
        {
            Values = new Dictionary<string, double> { ["custom-metric"] = 80 }
        });

        Assert.True(sut.IsUp);
        Assert.Equal(20, sut.Weight, 0.01);
    }

    private IServiceProvider CreateServiceProvider(params RuleConfig[] rules)
    {
        return new ServiceCollection()
            .AddOptions()
            .Configure<RulesConfig>(o => o.AddRange(rules))
            .AddFakeLogging(o => o.OutputSink = testOutputHelper.WriteLine)
            .AddSingleton<TestTimeProvider>()
            .AddSingleton<TimeProvider>(p => p.GetRequiredService<TestTimeProvider>())
            .AddSingleton<State>()
            .BuildServiceProvider();
    }
}
