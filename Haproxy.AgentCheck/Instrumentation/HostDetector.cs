using OpenTelemetry.Resources;

namespace Lucca.Infra.Haproxy.AgentCheck.Instrumentation;

public class HostDetector : IResourceDetector
{
    /// <inheritdoc />
    public Resource Detect()
    {
        return new Resource(new Dictionary<string, object>
        {
            ["host.name"] = Environment.MachineName
        });
    }
}
