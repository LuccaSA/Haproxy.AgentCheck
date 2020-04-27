using System;
using Microsoft.Extensions.Options;

namespace HostProbe
{
    public class StateProjection
    {
        private readonly IOptionsMonitor<HostProbeConfig> _options;
        public State State { get; }

        public StateProjection(IOptionsMonitor<HostProbeConfig> options, State state)
        {
            _options = options;
            State = state;
        }

        public bool IsServiceAvailable
        {
            get
            {
                var config = _options.CurrentValue;
                return State.CpuPercent > config.CpuLimit || State.IisRequests > config.IisRequestsLimit;
            }
        }

        public int Weight
        {
            get
            {
                var config = _options.CurrentValue;
                int weightCpu = (int)((config.CpuLimit - State.CpuPercent) / (double)config.CpuLimit * 100);
                int weightRequestLimit = (int)((config.IisRequestsLimit - State.IisRequests) / (double)config.IisRequestsLimit * 100);
                int weight = Math.Min(weightCpu, weightRequestLimit);
                if (weight <= 0)
                {
                    // security to avoid full drain mode
                    weightCpu = 1;
                }
                return weightCpu;
            }
        }
    }
}