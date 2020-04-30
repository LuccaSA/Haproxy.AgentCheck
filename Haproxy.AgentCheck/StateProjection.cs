using System;
using Microsoft.Extensions.Options;

namespace Haproxy.AgentCheck
{
    public class StateProjection
    {
        private const double _k = 4.61;
        private readonly IOptionsMonitor<AgentCheckConfig> _options;
        public State State { get; }

        public StateProjection(IOptionsMonitor<AgentCheckConfig> options, State state)
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

                int weightCpu = ComputeWeight(State.CpuPercent, config.CpuLimit, config.SystemResponse);
                int weightRequestLimit = ComputeWeight(State.IisRequests, config.IisRequestsLimit, config.SystemResponse);
                int weight = Math.Min(weightCpu, weightRequestLimit);

                if (weight <= 0)
                {
                    // security to avoid full drain mode
                    weightCpu = 1;
                }
                return weightCpu;
            }
        }

        internal static int ComputeWeight(int currentValue, int limit, SystemResponse systemResponse)
        {
            return systemResponse switch
            {
                SystemResponse.Linear => (int)((limit - currentValue) / (double)limit * 100),
                SystemResponse.FirstOrder => (int)Math.Exp(-currentValue / (_k * limit)) * 100,
                _ => throw new ArgumentOutOfRangeException(nameof(systemResponse), systemResponse, null)
            };
        }
    }
}