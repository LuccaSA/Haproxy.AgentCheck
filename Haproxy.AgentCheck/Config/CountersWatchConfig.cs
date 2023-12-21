namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class CountersWatchConfig
{
    public List<string> Providers { get; } = new();
    public required string Process { get; set; }
}
