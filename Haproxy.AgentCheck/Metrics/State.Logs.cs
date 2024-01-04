namespace Lucca.Infra.Haproxy.AgentCheck.Metrics;

internal partial class State
{
    [LoggerMessage(LogLevel.Information, "Circuit breaker {circuitBreaker} is now broken.")]
    static partial void LogBrokenCircuitBreaker(ILogger logger, string circuitBreaker);

    [LoggerMessage(LogLevel.Information, "Circuit breaker {circuitBreaker} is now fixed.")]
    static partial void LogFixedCircuitBreaker(ILogger logger, string circuitBreaker);

    [LoggerMessage(LogLevel.Information, "Circuit breaker {circuitBreaker} was removed.")]
    static partial void LogCircuitBreakerRemoved(ILogger logger, string circuitBreaker);

    [LoggerMessage(LogLevel.Information, "Circuit breaker {circuitBreaker} is fixed but awaiting {time:O}")]
    static partial void LogFixedCircuitBreakerAwaitingDelay(ILogger logger, string circuitBreaker, DateTimeOffset time);

    [LoggerMessage(LogLevel.Information, "Circuit breaker {circuitBreaker} was broken while awaiting {time:O}")]
    static partial void LogBrokenCircuitBreakerWhileAwaitingDelay(ILogger logger, string circuitBreaker, DateTimeOffset time);
}
