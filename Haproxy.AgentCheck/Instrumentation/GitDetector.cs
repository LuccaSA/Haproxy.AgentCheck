using OpenTelemetry.Resources;

namespace Lucca.Infra.Haproxy.AgentCheck.Instrumentation;

public class GitDetector : IResourceDetector
{
    /// <inheritdoc />
    public Resource Detect()
    {
        var resourceAttributes = new Dictionary<string, object>();

        if (EntrypointAssembly.TryGetMetadataAttribute("RepositoryRevision", out var revisionAttribute)
            && !string.IsNullOrWhiteSpace(revisionAttribute.Value))
        {
            resourceAttributes["git.commit.sha"] = revisionAttribute.Value;
        }

        if (EntrypointAssembly.TryGetMetadataAttribute("RepositoryUrl", out var urlAttribute)
            && !string.IsNullOrWhiteSpace(urlAttribute.Value))
        {
            resourceAttributes["git.repository_url"] = urlAttribute.Value;
        }

        return new(resourceAttributes);
    }
}
