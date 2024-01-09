namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class RuleConfig
{
    public RuleSource Source { get; set; }
    public required string Name { get; set; }
    public WeightRule? Weight { get; set; }
    public FailureRule? Failure { get; set; }
}
