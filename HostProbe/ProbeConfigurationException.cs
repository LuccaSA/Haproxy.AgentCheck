using System;

namespace HostProbe
{
    public class ProbeConfigurationException : Exception
    {
        public ProbeConfigurationException(string message)
            : base(message)
        {
        }
    }
}