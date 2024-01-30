namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class FailureRule
{
    public double EnterThreshold { get; set; }
    public double LeaveThreshold { get; set; }
    public TimeSpan? LeaveAfter { get; set; }
}
