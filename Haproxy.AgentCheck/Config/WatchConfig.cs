namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class WatchConfig
{
    public required SystemWatchConfig System { get; set; }
    public required CountersWatchConfig Counters { get; set; }
}
