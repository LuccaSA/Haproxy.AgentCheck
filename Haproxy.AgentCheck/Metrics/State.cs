using System.Diagnostics.Contracts;
using System.Diagnostics.Metrics;
using Lucca.Infra.Haproxy.AgentCheck.Config;
using Microsoft.Extensions.Options;

namespace Lucca.Infra.Haproxy.AgentCheck.Metrics;

internal partial class State(IOptionsMonitor<RulesConfig> options, ILogger<State> logger, TimeProvider timeProvider)
{
    private static readonly Gauge<int> UpGauge = Instrumentation.Sources.Meter.CreateGauge<int>("agent_check_up");
    private static readonly Gauge<double> WeightGauge = Instrumentation.Sources.Meter.CreateGauge<double>("agent_check_weight");
    private static readonly Gauge<int> RuleUpGauge = Instrumentation.Sources.Meter.CreateGauge<int>("agent_check_rule_up");
    private static readonly Gauge<double> RuleWeightGauge = Instrumentation.Sources.Meter.CreateGauge<double>("agent_check_rule_weight");

    private readonly HashSet<string> _brokenCircuitBreakers = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> _fixedCircuitBreakersAwaitingDelay = new(StringComparer.InvariantCultureIgnoreCase);

    public SystemState? System { get; private set; }
    public CountersState? Counters { get; private set; }
    public double Weight { get; private set; }
    public bool IsUp { get; private set; }
    public IReadOnlyCollection<string> BrokenCircuitsBreakers => _brokenCircuitBreakers;

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
        var missingCircuitBreakers = _brokenCircuitBreakers
                .Where(name => name.StartsWith(nameof(RuleSource.Counters), StringComparison.InvariantCultureIgnoreCase))
                .Where(name => !state.Values.ContainsKey(name[(name.IndexOf('/', StringComparison.InvariantCultureIgnoreCase) + 1)..]))
            .ToList();
        foreach (var name in missingCircuitBreakers)
        {
            LogCircuitBreakerRemoved(logger, name);
            _brokenCircuitBreakers.Remove(name);
            _fixedCircuitBreakersAwaitingDelay.Remove(name);
        }
    }

    private void ComputeState()
    {
        var weight = 100d;
        var isDown = false;

        foreach (var rule in options.CurrentValue)
        {
            var key = $"{rule.Source}/{rule.Name}";
            var value = GetValueForRule(rule);

            if (value is null) continue;

            if (rule.Weight is not null)
            {
                var ruleWeight = ComputeWeight(value.Value, rule.Weight);
                RuleWeightGauge.Record(ruleWeight, new KeyValuePair<string, object?>("rule", key));
                weight = Math.Min(weight, ruleWeight);
            }

            if (rule.Failure is not null)
            {
                var circuitBrokenIsDown = ComputeStatus(key, value.Value, rule.Failure);
                RuleUpGauge.Record(circuitBrokenIsDown ? 0 : 1, new KeyValuePair<string, object?>("rule", key));
                isDown |= circuitBrokenIsDown;
            }
        }


        Weight = weight;
        IsUp = !isDown;
        UpGauge.Record(IsUp ? 1 : 0);
        WeightGauge.Record(Weight);
    }

    private double? GetValueForRule(RuleConfig rule)
    {
        return rule switch
        {
            { Source: RuleSource.System, Name: "CPU" } when System is not null => System.CpuPercent,
            { Source: RuleSource.System, Name: "IisRequests" } when System is not null => System.IisRequests,
            { Source: RuleSource.Counters } when Counters is not null && Counters.Values.TryGetValue(rule.Name, out var counterValue) => counterValue,
            _ => default(double?)
        };
    }

    private bool ComputeStatus(string ruleName, double currentValue, FailureRule failureRule)
    {
        if (currentValue > failureRule.EnterThreshold)
        {
            _fixedCircuitBreakersAwaitingDelay.Remove(ruleName);
            if (_brokenCircuitBreakers.Add(ruleName))
            {
                LogBrokenCircuitBreaker(logger, ruleName);
            }
        }
        else if (currentValue < failureRule.LeaveThreshold)
        {
            if (!_brokenCircuitBreakers.Contains(ruleName))
            {
                return false;
            }

            bool isFixed = false;
            if (failureRule.LeaveAfter is { } leaveAfter)
            {
                if (_fixedCircuitBreakersAwaitingDelay.TryGetValue(ruleName, out var time) && time <= timeProvider.GetUtcNow())
                {
                    isFixed = true;
                }
                else if (!_fixedCircuitBreakersAwaitingDelay.ContainsKey(ruleName))
                {
                    var awaitingUntil = timeProvider.GetUtcNow().Add(leaveAfter);
                    _fixedCircuitBreakersAwaitingDelay[ruleName] = awaitingUntil;
                    LogFixedCircuitBreakerAwaitingDelay(logger, ruleName, awaitingUntil);
                }
            }
            else
            {
                isFixed = true;
            }

            if (isFixed && _brokenCircuitBreakers.Remove(ruleName))
            {
                LogFixedCircuitBreaker(logger, ruleName);
            }
        }
        else if (_fixedCircuitBreakersAwaitingDelay.TryGetValue(ruleName, out var awaitingUntil))
        {
            LogBrokenCircuitBreakerWhileAwaitingDelay(logger, ruleName, awaitingUntil);
            _fixedCircuitBreakersAwaitingDelay.Remove(ruleName);
        }

        return _brokenCircuitBreakers.Contains(ruleName);
    }

    private static double ComputeWeight(double currentValue, WeightRule weightRule)
    {
        var computedWeight = weightRule.SystemResponse switch
        {
            SystemResponse.Linear => ComputeLinearResponse(currentValue, weightRule),
            SystemResponse.FirstOrder => ComputeFirstOrderResponse(currentValue, weightRule),
            _ => throw new NotSupportedException()
        };

        return Math.Clamp(computedWeight, weightRule.MinWeight, weightRule.MaxWeight);
    }

    [Pure]
    private static double ComputeFirstOrderResponse(double currentValue, WeightRule weightRule)
    {
        var k = -Math.Log(1d / (weightRule.MaxWeight - weightRule.MinWeight + 1));
        return (weightRule.MaxWeight - weightRule.MinWeight + 1) * Math.Exp(-((currentValue - weightRule.MinValue) * k) / (weightRule.MaxValue - weightRule.MinValue)) + weightRule.MinWeight - 1;
    }

    [Pure]
    private static double ComputeLinearResponse(double currentValue, WeightRule weightRule)
    {
        return 1d * (weightRule.MaxValue - currentValue) / (weightRule.MaxValue - weightRule.MinValue) * (weightRule.MaxWeight - weightRule.MinWeight) + weightRule.MinWeight;
    }
}

internal record SystemState
{
    public double CpuPercent { get; init; }
    public double IisRequests { get; init; }
}

internal record CountersState
{
    public Dictionary<string, double> Values { get; init; } = new(StringComparer.InvariantCultureIgnoreCase);
}
