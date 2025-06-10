using System.Diagnostics;
using System.Runtime.Versioning;

namespace Lucca.Infra.Haproxy.AgentCheck.Metrics;

[SupportedOSPlatform("windows")]
internal sealed class WindowsStateCollector(State state) : IStateCollector, IDisposable
{
    private readonly PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");
    private readonly PerformanceCounter _iisRequests = new ("ASP.NET", "requests current", true);

    public void Collect()
    {
        state.UpdateState(new SystemState
        {
            CpuPercent = TryReadCounter(_cpuCounter),
            IisRequests = TryReadCounter(_iisRequests)
        });
    }

    private static double TryReadCounter(PerformanceCounter counter)
    {
        try
        {
            return counter.NextValue();
        }
        catch(InvalidOperationException)
        {
            return 0d;
        }
    }

    public void Dispose()
    {
        _cpuCounter.Dispose();
        _iisRequests.Dispose();
    }
}
