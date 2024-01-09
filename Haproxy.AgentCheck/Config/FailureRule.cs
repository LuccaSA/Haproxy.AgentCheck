namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class FailureRule
{
    public int EnterThreshold { get; set; }
    public int LeaveThreshold { get; set; }
    public TimeSpan? LeaveAfter { get; set; }
}
