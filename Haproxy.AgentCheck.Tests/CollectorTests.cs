using Lucca.Infra.Haproxy.AgentCheck.Hosting;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lucca.Infra.Haproxy.AgentCheck.Tests;

public class CollectorTests
{
    [Fact]
    public void CollectTest()
    {
        var sc = new ServiceCollection();
        sc.AddMetricCollector();
        var collector = sc.BuildServiceProvider().GetRequiredService<IStateCollector>();

        collector.Collect();
        Assert.True(true);
    }
}
