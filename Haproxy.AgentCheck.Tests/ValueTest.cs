using System;
using System.Linq;
using Xunit;

namespace Haproxy.AgentCheck.Tests
{
    public class LimitTests
    {
        [Theory]
        [InlineData(100, 100, 0)]
        [InlineData(0, 100, 100)]
        public void TestRanges(int inputValue, int maxValue, int expected)
        {
            foreach (var systemResponse in Enum.GetValues(typeof(SystemResponse)).OfType<SystemResponse>())
            {
                var computed = StateProjection.WeightResponse(inputValue, maxValue, systemResponse);
                Assert.Equal(expected, computed);
            }
        }

        [Theory]
        [InlineData(100, 100, 1)]
        [InlineData(200, 100, 1)]
        [InlineData(0, 100, 100)]
        [InlineData(-100, 100, 100)]
        public void BoundedWeight(int inputValue, int maxValue, int expected)
        {
            State state = new State()
            {
                CpuPercent = inputValue,
                IisRequests = inputValue,
            };
            AgentCheckConfig config = new AgentCheckConfig
            {
                CpuLimit = maxValue,
                IisRequestsLimit = maxValue
            };
            foreach (var systemResponse in Enum.GetValues(typeof(SystemResponse)).OfType<SystemResponse>())
            {
                config.SystemResponse = systemResponse;
                var computed = StateProjection.ComputeWeight(state, config);
                Assert.Equal(expected, computed);
            }
        }
    }
}
