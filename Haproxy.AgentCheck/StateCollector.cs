using System;
using System.Diagnostics;

namespace Haproxy.AgentCheck
{
    public class WindowsStateCollector : IStateCollector
    {
        private readonly State _state;
        private readonly PerformanceCounter _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private readonly PerformanceCounter _iisRequests = new PerformanceCounter(@"ASP.NET", "requests current", true);
       
        public WindowsStateCollector(State state)
        {
            _state = state;
        }

        public void Collect()
        {
            _state.CpuPercent = (int)_cpuCounter.NextValue();
            _state.IisRequests = (int)_iisRequests.NextValue();
        }
    }

    public class LinuxStateCollector : IStateCollector
    {
        public void Collect()
        {
            throw new NotImplementedException("TODO : /proc/stat parsing");
        }
    }

    public interface IStateCollector
    {
        void Collect();
    }
}