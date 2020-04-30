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
                var computed = StateProjection.ComputeWeight(inputValue, maxValue, systemResponse);
                Assert.Equal(expected, computed);
            }
        }
    }
}
