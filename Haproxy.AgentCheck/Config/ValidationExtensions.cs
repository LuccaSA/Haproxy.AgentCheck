using System;
using System.Linq;
using Haproxy.AgentCheck.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Haproxy.AgentCheck.Config
{
    public static class ValidationExtensions
    {
        public static void ValidateConfig(this IServiceCollection services)
        {
            services.PostConfigure<AgentCheckConfig>(CheckConfig);
        }

        internal static void CheckConfig(AgentCheckConfig c)
        {
            if (c.CpuLimit < 10 || c.CpuLimit > 100)
            {
                throw new AgentCheckConfigurationException(
                    $"CpuLimit must be between 10 and 100. Actual value is {c.CpuLimit}");
            }

            if (c.IisRequestsLimit <= 0)
            {
                throw new AgentCheckConfigurationException(
                    $"IisRequestsLimit must be positive and not null. Actual value is {c.IisRequestsLimit}");
            }

            if (c.RefreshIntervalInMs < 100)
            {
                throw new AgentCheckConfigurationException(
                    $"RefreshIntervalInMs must be greater than 100ms. Small intervals impacts average. Actual value is {c.RefreshIntervalInMs}");
            }

            if (!Enum.IsDefined(typeof(SystemResponse), c.SystemResponse))
            {
                throw new AgentCheckConfigurationException(
                    $"SystemResponse is invalid. Actual value is {c.RefreshIntervalInMs}. Valid values are {string.Join("'", Enum.GetValues(typeof(SystemResponse)).OfType<SystemResponse>().Select(i => i.ToString()))}");
            }
        }
    }
}
