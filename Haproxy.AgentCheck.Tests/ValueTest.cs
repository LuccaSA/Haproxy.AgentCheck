using System;
using System.Linq;
using Haproxy.AgentCheck.Config;
using Haproxy.AgentCheck.Metrics;
using Xunit;

namespace Haproxy.AgentCheck.Tests
{
    public class LimitTests
    {
        [Theory]
        [InlineData(100, 100, 1)]
        [InlineData(0, 100, 100)]
        public void TestSingleWeightResponseInRange(int inputValue, int maxValue, int expected)
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
        [InlineData(50, 50, 1)]
        [InlineData(12, 12, 1)]
        [InlineData(80, 80, 1)]
        [InlineData(42, 42, 1)]
        public void BoundedWeight(int inputValue, int maxValue, int expected)
        {
            var state = new State
            {
                CpuPercent = inputValue,
                IisRequests = inputValue,
            };
            var config = new AgentCheckConfig
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
