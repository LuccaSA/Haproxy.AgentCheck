using System.Text;
using Lucca.Infra.Haproxy.AgentCheck.Config;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Lucca.Infra.Haproxy.AgentCheck.Tests;

public class TcpResponseTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TcpResponse_WhenUp_ReturnsUpWithWeight()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Weight = new WeightRule
                {
                    SystemResponse = SystemResponse.Linear
                }
            });

        var state = serviceProvider.GetRequiredService<State>();
        var maintenanceStatus = serviceProvider.GetRequiredService<MaintenanceStatus>();
        
        state.UpdateState(new SystemState { CpuPercent = 30 });

        var response = FormatTcpResponse(state, maintenanceStatus);
        
        Assert.StartsWith("70%", response);
        Assert.Contains("up", response);
    }

    [Fact]
    public void TcpResponse_WhenDown_ReturnsDownWithFailures()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Failure = new FailureRule
                {
                    EnterThreshold = 80,
                    LeaveThreshold = 60
                }
            });

        var state = serviceProvider.GetRequiredService<State>();
        var maintenanceStatus = serviceProvider.GetRequiredService<MaintenanceStatus>();
        
        state.UpdateState(new SystemState { CpuPercent = 90 });

        var response = FormatTcpResponse(state, maintenanceStatus);
        
        Assert.Contains("down", response);
        Assert.Contains("System/CPU", response);
    }

    [Fact]
    public void TcpResponse_WhenMaintenance_ReturnsStopped()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Weight = new WeightRule()
            });

        var state = serviceProvider.GetRequiredService<State>();
        var maintenanceStatus = serviceProvider.GetRequiredService<MaintenanceStatus>();
        maintenanceStatus.IsMaintenance = true;
        
        state.UpdateState(new SystemState { CpuPercent = 10 });

        var response = FormatTcpResponse(state, maintenanceStatus);
        
        Assert.Contains("stopped", response);
        Assert.Contains("Requested maintenance", response);
    }

    [Fact]
    public void TcpResponse_MultipleFailures_ListsAllBrokenCircuits()
    {
        var serviceProvider = CreateServiceProvider(
            new RuleConfig
            {
                Name = "CPU",
                Source = RuleSource.System,
                Failure = new FailureRule { EnterThreshold = 50, LeaveThreshold = 30 }
            },
            new RuleConfig
            {
                Name = "IisRequests",
                Source = RuleSource.System,
                Failure = new FailureRule { EnterThreshold = 100, LeaveThreshold = 50 }
            });

        var state = serviceProvider.GetRequiredService<State>();
        var maintenanceStatus = serviceProvider.GetRequiredService<MaintenanceStatus>();
        
        state.UpdateState(new SystemState { CpuPercent = 60, IisRequests = 150 });

        var response = FormatTcpResponse(state, maintenanceStatus);
        
        Assert.Contains("down", response);
        Assert.Contains("#2 failures", response);
        Assert.Contains("System/CPU", response);
        Assert.Contains("System/IisRequests", response);
    }

    /// <summary>
    /// Simulates the TCP response format from TcpHandler
    /// </summary>
    private static string FormatTcpResponse(State state, MaintenanceStatus maintenanceStatus)
    {
        var up = state.IsUp ? "up" : "down";
        if (!state.IsUp)
        {
            up += $" #{state.BrokenCircuitsBreakers.Count} failures ({string.Join(",", state.BrokenCircuitsBreakers)})";
        }

        if (maintenanceStatus.IsMaintenance)
        {
            up = "stopped #Requested maintenance";
        }

        return $"{state.Weight:F0}% {up}\n";
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
            .AddSingleton<MaintenanceStatus>()
            .BuildServiceProvider();
    }
}
