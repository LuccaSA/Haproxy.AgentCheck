using System.ComponentModel.DataAnnotations;

namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class SystemWatchConfig
{
    [Range(100, int.MaxValue)] public int RefreshIntervalInMs { get; set; }
}
