using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Lucca.Infra.Haproxy.AgentCheck.Instrumentation;

public static class EntrypointAssembly
{
    /// <summary>
    /// The <see cref="Assembly"/> instance, can be set.
    /// </summary>
    public static Assembly Instance { get; set; } = Assembly.GetEntryAssembly()!;

    /// <summary>
    /// Get an assembly attribute from <see cref="Instance"/>.
    /// </summary>
    /// <typeparam name="TAttribute">Type of the assembly attribute</typeparam>
    /// <returns>The assembly attribute</returns>
    /// <exception cref="InvalidOperationException">No such attribute found</exception>
    public static TAttribute GetAttribute<TAttribute>() where TAttribute : Attribute
    {
        return Instance.GetCustomAttribute<TAttribute>() ?? throw new InvalidOperationException("Attribute not found");
    }

    /// <summary>
    /// Try to get an assembly attribute from <see cref="Instance"/>.
    /// </summary>
    /// <typeparam name="TAttribute">Type of the assembly attribute</typeparam>
    /// <param name="attribute">The found attribute, <value>null</value> when <value>true</value> returned.</param>
    /// <returns>Is the attribute found</returns>
    public static bool TryGetAttribute<TAttribute>([NotNullWhen(true)] out TAttribute? attribute) where TAttribute : Attribute
    {
        attribute = Instance.GetCustomAttribute<TAttribute>();
        return attribute != null;
    }

    /// <summary>
    /// Get an <see cref="AssemblyMetadataAttribute"/> from <see cref="Instance"/>
    /// whose <see cref="AssemblyMetadataAttribute.Key"/> matches the <paramref name="key"/> parameter.
    /// </summary>
    /// <param name="key">The key to look for</param>
    /// <returns>The found attribute</returns>
    /// <exception cref="InvalidOperationException">No such attribute found</exception>
    public static AssemblyMetadataAttribute GetMetadataAttribute(string key)
    {
        return Instance
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .First(a => a.Key.Equals(key, StringComparison.Ordinal));
    }

    /// <summary>
    /// Try to get an <see cref="AssemblyMetadataAttribute"/> from <see cref="Instance"/>
    /// whose <see cref="AssemblyMetadataAttribute.Key"/> matches the <paramref name="key"/> parameter.
    /// </summary>
    /// <param name="key">The key to look for</param>
    /// <param name="attribute">The found attribute, not <value>null</value> when <value>true</value> returned.</param>
    /// <returns>Is the attribute found</returns>
    public static bool TryGetMetadataAttribute(string key, [NotNullWhen(true)] out AssemblyMetadataAttribute? attribute)
    {
        attribute = Instance.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key.Equals(key, StringComparison.Ordinal));
        return attribute != null;
    }

    /// <summary>
    /// The service name extracted from metadata
    /// </summary>
    public static string ServiceName
    {
        get
        {
            if (TryGetMetadataAttribute("ResourceServiceName", out var serviceNameMetadataAttribute)
                && !string.IsNullOrEmpty(serviceNameMetadataAttribute.Value))
            {
                return serviceNameMetadataAttribute.Value;
            }
            else
            {
                return Process.GetCurrentProcess().ProcessName;
            }
        }
    }

    /// <summary>
    /// The service namespace extracted from metadata
    /// </summary>
    public static string ServiceNamespace
    {
        get
        {
            if (TryGetMetadataAttribute("ResourceServiceNamespace", out var serviceNamespaceMetadataAttribute)
                && !string.IsNullOrEmpty(serviceNamespaceMetadataAttribute.Value))
            {
                return serviceNamespaceMetadataAttribute.Value;
            }
            else
            {
                return "Lucca.Unknown";
            }
        }
    }

    /// <summary>
    /// The service team extracted from metadata
    /// </summary>
    public static string? ServiceTeam
    {
        get
        {
            if (TryGetMetadataAttribute("ResourceServiceTeam", out var teamMetadataAttribute)
                && !string.IsNullOrEmpty(teamMetadataAttribute.Value))
            {
                return teamMetadataAttribute.Value;
            }

            return null;
        }
    }

    /// <summary>
    /// The service version extracted from metadata
    /// </summary>
    public static string? ServiceVersion
    {
        get
        {
            if (TryGetAttribute<AssemblyFileVersionAttribute>(out var assemblyFileVersionAttribute))
            {
                return assemblyFileVersionAttribute.Version;
            }

            return null;
        }
    }
}
