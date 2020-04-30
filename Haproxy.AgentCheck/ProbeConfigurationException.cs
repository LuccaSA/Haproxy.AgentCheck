using System;

namespace Haproxy.AgentCheck
{
    public class ProbeConfigurationException : Exception
    {
        public ProbeConfigurationException(string message)
            : base(message)
        {
        }
    }
}