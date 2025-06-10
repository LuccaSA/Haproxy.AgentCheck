using System.Diagnostics.Metrics;

namespace Lucca.Infra.Haproxy.AgentCheck.Instrumentation;

public static class Sources
{
    public const string MeterName = "Haproxy.AgentCheck";
    public static Meter Meter { get; } = new Meter(MeterName);
}
