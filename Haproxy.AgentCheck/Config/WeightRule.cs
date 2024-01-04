using Lucca.Infra.Haproxy.AgentCheck.Metrics;

namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class WeightRule
{
    public SystemResponse SystemResponse { get; set; }
    public int MinValue { get; set; } = 0;
    public int MaxValue { get; set; } = 100;
    public int MinWeight { get; set; } = 0;
    public int MaxWeight { get; set; } = 100;
}
