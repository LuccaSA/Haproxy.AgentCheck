using Haproxy.AgentCheck.Config;
using Haproxy.AgentCheck.Metrics;
using Xunit;

namespace Haproxy.AgentCheck.Tests
{
    public class ConfigTests
    {
        [Fact]
        public void CpuLimit()
        {
            var conf = new AgentCheckConfig
            {
                IisRequestsLimit = 42,
                RefreshIntervalInMs = 2000
            };

            Assert.Throws<AgentCheckConfigurationException>(() =>
            {
                conf.CpuLimit = 0;
                ValidationExtensions.CheckConfig(conf);
            });
            Assert.Throws<AgentCheckConfigurationException>(() =>
            {
                conf.CpuLimit = 101;
                ValidationExtensions.CheckConfig(conf);
            });
            conf.CpuLimit = 42;
            ValidationExtensions.CheckConfig(conf);
        }

        [Fact]
        public void IisLimit()
        {
            var conf = new AgentCheckConfig
            {
                CpuLimit = 80,
                RefreshIntervalInMs = 2000
            };

            Assert.Throws<AgentCheckConfigurationException>(() =>
            {
                conf.IisRequestsLimit = -1;
                ValidationExtensions.CheckConfig(conf);
            });
            Assert.Throws<AgentCheckConfigurationException>(() =>
            {
                conf.IisRequestsLimit = 0;
                ValidationExtensions.CheckConfig(conf);
            });
            conf.IisRequestsLimit = 42;
            ValidationExtensions.CheckConfig(conf);
        }

        [Fact]
        public void IntervalLimit()
        {
            var conf = new AgentCheckConfig
            {
                CpuLimit = 80,
                IisRequestsLimit = 42
            };

            Assert.Throws<AgentCheckConfigurationException>(() =>
            {
                conf.RefreshIntervalInMs = -1;
                ValidationExtensions.CheckConfig(conf);
            });
            Assert.Throws<AgentCheckConfigurationException>(() =>
            {
                conf.RefreshIntervalInMs = 0;
                ValidationExtensions.CheckConfig(conf);
            });
            conf.RefreshIntervalInMs = 2000;
            ValidationExtensions.CheckConfig(conf);
        }

        [Fact]
        public void InvalidSystemResponse()
        {
            var conf = new AgentCheckConfig
            {
                CpuLimit = 80,
                IisRequestsLimit = 42,
                RefreshIntervalInMs = 2000,
                SystemResponse = (SystemResponse) (-1)
            };
            Assert.Throws<AgentCheckConfigurationException>(() =>
            { 
                ValidationExtensions.CheckConfig(conf);
            });
        }
    }
}
