using System;

namespace Haproxy.AgentCheck.Config
{
    public class AgentCheckConfigurationException : Exception
    {
        public AgentCheckConfigurationException(string message)
            : base(message)
        {
        }
    }
}
