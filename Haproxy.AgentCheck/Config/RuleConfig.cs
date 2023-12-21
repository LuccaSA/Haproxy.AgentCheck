namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class RuleConfig
{
    public RuleSource Source { get; set; }
    public required string Name { get; set; }
    public RuleWeight? Weight { get; set; }
    public RuleMaintenance? Maintenance { get; set; }
}