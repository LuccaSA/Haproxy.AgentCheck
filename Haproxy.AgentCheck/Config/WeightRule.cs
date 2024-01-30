using Lucca.Infra.Haproxy.AgentCheck.Metrics;

namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class WeightRule
{
    public SystemResponse SystemResponse { get; set; }
    public double MinValue { get; set; } = 0;
    public double MaxValue { get; set; } = 100;
    public double MinWeight { get; set; } = 0;
    public double MaxWeight { get; set; } = 100;
}
