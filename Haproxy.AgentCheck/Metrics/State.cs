using Lucca.Infra.Haproxy.AgentCheck.Config;
using Microsoft.Extensions.Options;

namespace Lucca.Infra.Haproxy.AgentCheck.Metrics;

internal partial class State(IOptionsMonitor<RulesConfig> options, ILogger<State> logger)
{
    private readonly HashSet<string> _brokenCircuitBreakers = new(StringComparer.InvariantCultureIgnoreCase);
    public SystemState? System { get; private set; }
    public CountersState? Counters { get; private set; }
    public int Weight { get; private set; }
    public bool IsReady { get; private set; }

    public void UpdateState(SystemState state)
    {
        System = state;
        ComputeState();
    }

    public void UpdateState(CountersState state)
    {
        RemoveMissingBrokenCircuits(state);
        Counters = state;
        ComputeState();
    }

    private void RemoveMissingBrokenCircuits(CountersState state)
    {
        var missingCircuitBreakers = _brokenCircuitBreakers.Where(name => !state.Values.ContainsKey($"{RuleSource.Counters}/{name}")).ToList();
        foreach (var name in missingCircuitBreakers)
        {
            LogCircuitBreakerRemoved(logger, name);
            _brokenCircuitBreakers.Remove(name);
        }
    }

    private void ComputeState()
    {
        int weight = 100;
        bool isDown = false;

        foreach (var rule in options.CurrentValue)
        {
            var key = $"{rule.Source}/{rule.Name}";
            var value = GetValueForRule(rule);

            if (value is null) continue;

            if (rule.Weight is not null)
            {
                weight = Math.Min(weight, ComputeWeight(value.Value, rule.Weight));
            }

            if (rule.Maintenance is not null)
            {
                if (value > rule.Maintenance.EnterThreshold && _brokenCircuitBreakers.Add(key))
                {
                    LogBrokenCircuitBreaker(logger, key);
                }

                if (value < rule.Maintenance.LeaveThreshold && _brokenCircuitBreakers.Remove(key))
                {
                    LogFixedCircuitBreaker(logger, key);
                }

                isDown = isDown || _brokenCircuitBreakers.Contains(key);
            }
        }

        Weight = weight;
        IsReady = !isDown;
    }

    private int? GetValueForRule(RuleConfig rule)
    {
        return rule switch
        {
            { Source: RuleSource.System, Name: "CPU" } when System is not null => System.CpuPercent,
            { Source: RuleSource.System, Name: "IisRequests" } when System is not null => System.IisRequests,
            { Source: RuleSource.Counters } when Counters is not null && Counters.Values.TryGetValue(rule.Name, out var counterValue) => counterValue,
            _ => default(int?)
        };
    }

    private static int ComputeWeight(int currentValue, RuleWeight ruleWeight)
    {
        const double k = 4.61;
        return Math.Clamp(ruleWeight.SystemResponse switch
        {
            SystemResponse.Linear => (int)((ruleWeight.Max - currentValue) / (double)ruleWeight.Max * 100),
            SystemResponse.FirstOrder => (int)Math.Ceiling(Math.Exp(-currentValue * k / ruleWeight.Max) * 100),
            _ => throw new NotSupportedException()
        }, 0, 100);
    }
}

internal partial class State
{
    [LoggerMessage(LogLevel.Information, "Circuit breaker {circuitBreaker} is now broken.")]
    static partial void LogBrokenCircuitBreaker(ILogger logger, string circuitBreaker);

    [LoggerMessage(LogLevel.Information, "Circuit breaker {circuitBreaker} is now fixed.")]
    static partial void LogFixedCircuitBreaker(ILogger logger, string circuitBreaker);

    [LoggerMessage(LogLevel.Information, "Circuit breaker {circuitBreaker} was removed.")]
    static partial void LogCircuitBreakerRemoved(ILogger logger, string circuitBreaker);
}

internal record SystemState
{
    public int CpuPercent { get; init; }
    public int IisRequests { get; init; }
}

internal record CountersState
{
    public Dictionary<string, int> Values { get; init; } = new(StringComparer.InvariantCultureIgnoreCase);
}
