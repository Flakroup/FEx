using FEx.Flurlx.Configuration;
using FEx.Logging.Abstractions.Interfaces;
using Flurl.Http;
using Polly;
using Polly.Bulkhead;
using Polly.Fallback;
using Polly.Timeout;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FEx.Flurlx.Services;

/// <summary>
/// FEx Polly Policy Builder - Creates comprehensive resilience policies for HTTP requests.
/// </summary>
/// <remarks>
/// Builds a full resilience suite including:
/// - Retry with exponential backoff
/// - Circuit Breaker to prevent cascading failures
/// - Timeout to prevent hanging requests
/// - Bulkhead to limit concurrent requests (critical for slow APIs)
/// - Fallback for graceful degradation
/// Designed to handle slow APIs (e.g., Synology DSM) with proper throttling and resilience.
/// </remarks>
public class FExPollyPolicyBuilder : IFExPollyPolicyBuilder
{
    private readonly ILoggable _logger;

    public FExPollyPolicyBuilder(ILoggable logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Builds a comprehensive resilience policy with all Polly features.
    /// </summary>
    /// <param name="config">Polly policy configuration</param>
    /// <returns>Composite async policy with Retry, Circuit Breaker, Timeout, Bulkhead, and Fallback</returns>
    public IAsyncPolicy<IFlurlResponse> BuildFullSuitePolicy(PollyPolicyConfiguration config)
    {
        var retryPolicy = BuildRetryPolicy(config);
        var circuitBreakerPolicy = BuildCircuitBreakerPolicy(config);
        var timeoutPolicy = BuildTimeoutPolicy(config);
        var bulkheadPolicy = BuildBulkheadPolicy(config);

        IAsyncPolicy<IFlurlResponse> fallbackPolicy = config.EnableFallback
            ? BuildFallbackPolicy()
            : Policy.NoOpAsync<IFlurlResponse>();

        // Wrap all policies together (order matters!)
        // Fallback -> Retry -> Circuit Breaker -> Timeout -> Bulkhead
        return fallbackPolicy.WrapAsync(retryPolicy)
            .WrapAsync(circuitBreakerPolicy)
            .WrapAsync(timeoutPolicy)
            .WrapAsync(bulkheadPolicy);
    }

    /// <summary>
    /// Builds a Retry policy with exponential backoff.
    /// Handles transient errors: connection failures, timeouts, 5xx errors.
    /// </summary>
    private IAsyncPolicy<IFlurlResponse> BuildRetryPolicy(PollyPolicyConfiguration config) =>
        Policy.Handle<FlurlHttpException>()
            .Or<TimeoutRejectedException>()
            .Or<System.Net.Http.HttpRequestException>() // ✅ Connection failures
            .Or<System.Net.Sockets.SocketException>()   // ✅ Socket errors
            .Or<TaskCanceledException>()                 // ✅ Request cancellation/timeout
            .OrResult<IFlurlResponse>(static r => r == null 
                                                  || r.StatusCode == (int)HttpStatusCode.RequestTimeout
                                                  || r.StatusCode == 429 // TooManyRequests
                                                  || r.StatusCode >= (int)HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(config.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * config.InitialRetryDelay.TotalSeconds),
                (outcome, timespan, retryCount, _) =>
                {
                    _logger.LogWarning(
                        $"[FExPolly] Retry {retryCount}/{config.MaxRetryAttempts} after {timespan.TotalSeconds:F1}s due to {(outcome.Exception is not null ? $"exception: {outcome.Exception.Message}" : $"status code: {outcome.Result?.StatusCode}")}");
                });

    /// <summary>
    /// Builds a Circuit Breaker policy to prevent cascading failures.
    /// </summary>
    private IAsyncPolicy<IFlurlResponse> BuildCircuitBreakerPolicy(PollyPolicyConfiguration config)
    {
        return Policy.Handle<FlurlHttpException>()
            .Or<TimeoutRejectedException>()
            .Or<System.Net.Http.HttpRequestException>()
            .Or<System.Net.Sockets.SocketException>()
            .OrResult<IFlurlResponse>(r => r == null || r.StatusCode >= (int)HttpStatusCode.InternalServerError)
            .CircuitBreakerAsync(config.CircuitBreakerFailureThreshold,
                config.CircuitBreakerDuration,
                (_, duration) =>
                {
                    _logger.LogError(
                        $"[FExPolly] Circuit breaker OPENED for {duration.TotalSeconds:F0}s after {config.CircuitBreakerFailureThreshold} failures");
                },
                () => { _logger.LogInformation("[FExPolly] Circuit breaker CLOSED - endpoint recovered"); },
                () => { _logger?.LogInformation("[FExPolly] Circuit breaker HALF-OPEN - testing endpoint"); });
    }

    /// <summary>
    /// Builds a Timeout policy to prevent hanging requests.
    /// </summary>
    private IAsyncPolicy<IFlurlResponse> BuildTimeoutPolicy(PollyPolicyConfiguration config)
    {
        return Policy.TimeoutAsync<IFlurlResponse>(config.RequestTimeout,
            TimeoutStrategy.Pessimistic,
            (_, timespan, _) =>
            {
                _logger.LogWarning($"[FExPolly] Request timed out after {timespan.TotalSeconds:F1}s");

                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Builds a Bulkhead policy to limit concurrent requests.
    /// Critical for slow APIs to prevent overwhelming the endpoint.
    /// </summary>
    private AsyncBulkheadPolicy<IFlurlResponse> BuildBulkheadPolicy(PollyPolicyConfiguration config)
    {
        return Policy.BulkheadAsync<IFlurlResponse>(config.MaxParallelization,
            config.MaxQueuingActions,
            _ =>
            {
                _logger.LogWarning(
                    $"[FExPolly] Bulkhead rejected request - {config.MaxParallelization} concurrent + {config.MaxQueuingActions} queued limit reached");

                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Builds a Fallback policy for graceful degradation.
    /// </summary>
    private AsyncFallbackPolicy<IFlurlResponse> BuildFallbackPolicy()
    {
        return Policy<IFlurlResponse>.Handle<Exception>()
            .FallbackAsync(default(IFlurlResponse)!,
                async (result, context) =>
                {
                    _logger?.LogError(
                        $"[FExPolly] Fallback activated due to: {result.Exception?.Message ?? "Unknown error"}");

                    // Future enhancement: Try to get cached response
                    if (context.TryGetValue("CacheKey", out var cacheKey))
                        _logger?.LogInformation($"[FExPolly] Attempting to retrieve cached data for key: {cacheKey}");

                    // TODO: Implement cache retrieval
                    await Task.CompletedTask;
                });
    }
}