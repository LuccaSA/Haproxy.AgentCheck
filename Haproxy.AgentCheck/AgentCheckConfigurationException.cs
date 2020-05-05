using System;

namespace Haproxy.AgentCheck
{
    public class AgentCheckConfigurationException : Exception
    {
        public AgentCheckConfigurationException(string message)
            : base(message)
        {
        }
    }
}