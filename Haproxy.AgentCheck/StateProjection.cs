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

        public int Weight => ComputeWeight(State, _options.CurrentValue);

        internal static int ComputeWeight(State state, AgentCheckConfig config)
        {
            int weightCpu = WeightResponse(state.CpuPercent, config.CpuLimit, config.SystemResponse);
            int weightRequestLimit = WeightResponse(state.IisRequests, config.IisRequestsLimit, config.SystemResponse);
            int weight = Math.Min(weightCpu, weightRequestLimit);
            if (weight <= 0)
            {
                weight = 1; // security to avoid full drain mode
            }
            if (weight > 100)
            {
                weight = 100; // security to avoid over allocation
            }
            return weight;
        }

        internal static int WeightResponse(int currentValue, int limit, SystemResponse systemResponse)
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