using OpenTelemetry.Resources;

namespace Lucca.Infra.Haproxy.AgentCheck.Instrumentation;

public class DeploymentDetector : IResourceDetector
{
    /// <inheritdoc />
    public Resource Detect()
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables("LUCCA_").Build();
        var resourceAttributes = new Dictionary<string, object>();

        AddAsAttributeIfNotNull("CLUSTER", "deployment.cluster");
        AddAsAttributeIfNotNull("REGION", "deployment.region");
        AddAsAttributeIfNotNull("ENVIRONMENT", "deployment.environment");

        return new(resourceAttributes);

        void AddAsAttributeIfNotNull(string key, string @as)
        {
            string? value;
            if ((value = configuration.GetValue<string?>(key)) is not null)
            {
                resourceAttributes[@as] = value;
            }
        }
    }
}
