using Haproxy.AgentCheck.Metrics;

namespace Haproxy.AgentCheck.Config
{
    public class AgentCheckConfig
    {
        public int RefreshIntervalInMs { get; set; }
        public int CpuLimit { get; set; }
        public int IisRequestsLimit { get; set; }
        public SystemResponse SystemResponse { get; set; }
    }
}
