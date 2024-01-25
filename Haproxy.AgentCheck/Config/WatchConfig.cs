namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class WatchConfig
{
    public required TimeSpan SystemRefreshInterval { get; set; }
    public string? Process { get; set; }
    public List<string> DataSources { get; } = new();
}
