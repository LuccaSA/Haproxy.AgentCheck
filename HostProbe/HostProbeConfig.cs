namespace HostProbe
{
    public class HostProbeConfig
    {
        public int RefreshIntervalInMs { get; set; }
        public int CpuLimit { get; set; }
        public int IisRequestsLimit { get; set; }
        public SystemResponse SystemResponse { get; set; }
    }

    public enum SystemResponse
    {
        Linear,
        FirstOrder
    }
}