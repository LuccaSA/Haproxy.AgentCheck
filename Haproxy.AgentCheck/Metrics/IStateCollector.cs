using System;

namespace Haproxy.AgentCheck.Metrics
{
    public interface IStateCollector : IDisposable
    {
        void Collect();
    }
}
