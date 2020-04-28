using Microsoft.Extensions.DependencyInjection;

namespace HostProbe
{
    public static class ProbeConfigurationExtensions
    {
        public static void ValidateConfig(this IServiceCollection services)
        {
            services.PostConfigure<HostProbeConfig>(c =>
            {
                if (c.CpuLimit < 10 || c.CpuLimit > 100)
                {
                    throw new ProbeConfigurationException(
                        $"CpuLimit must be between 10 and 100. Actual value is {c.CpuLimit}");
                }

                if (c.IisRequestsLimit <= 0)
                {
                    throw new ProbeConfigurationException(
                        $"IisRequestsLimit must be positive and not null. Actual value is {c.IisRequestsLimit}");
                }

                if (c.RefreshIntervalInMs < 100)
                {
                    throw new ProbeConfigurationException(
                        $"RefreshIntervalInMs must be greater than 100ms. Small intervals impacts average. Actual value is {c.RefreshIntervalInMs}");
                }
            });
        }
    }
}