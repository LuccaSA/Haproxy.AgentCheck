using Lucca.Infra.Haproxy.AgentCheck.Metrics;

namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class RuleWeight
{
    public SystemResponse SystemResponse { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
}