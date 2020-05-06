using Haproxy.AgentCheck.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haproxy.AgentCheck.Tests
{
    public class CollectorTests
    {
        [Fact]
        public void CollectTest()
        {
            ServiceCollection sc = new ServiceCollection();
            sc.AddMetricCollector();
            var collector = sc.BuildServiceProvider().GetRequiredService<IStateCollector>();

            collector.Collect();
        }
    }
}
