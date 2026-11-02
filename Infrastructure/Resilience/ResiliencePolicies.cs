using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Infrastructure.Resilience;

/// <summary>Factoría de pipelines de resiliencia compartidos para clientes HTTP externos.</summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Crea un pipeline con reintentos, timeout y circuit breaker.
    /// </summary>
    /// <param name="timeout">Timeout por intento. Por defecto 30 s; la IA requiere más para generar.</param>
    public static ResiliencePipeline CreateHttpPipeline(TimeSpan? timeout = null) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(30)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(60)
            })
            .Build();
}
