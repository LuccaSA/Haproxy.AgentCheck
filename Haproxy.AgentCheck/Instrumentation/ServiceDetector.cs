using OpenTelemetry.Resources;

namespace Lucca.Infra.Haproxy.AgentCheck.Instrumentation;

public class ServiceDetector : IResourceDetector
{
    /// <summary>
    /// Unique id of the service instance
    /// </summary>
    public static string ServiceId { get; } = Guid.NewGuid().ToString("N");


    /// <inheritdoc />
    public Resource Detect()
    {
        var resourceAttributes = new Dictionary<string, object>
        {
            ["service.instance.id"] = ServiceId,
            ["service.name"] = EntrypointAssembly.ServiceName,
            ["service.namespace"] = EntrypointAssembly.ServiceNamespace
        };

        if (EntrypointAssembly.ServiceTeam is { } serviceTeam)
            resourceAttributes["service.team"] = serviceTeam;
        if (EntrypointAssembly.ServiceVersion is { } serviceVersion)
            resourceAttributes["service.version"] = serviceVersion;

        return new(resourceAttributes);
    }
}
