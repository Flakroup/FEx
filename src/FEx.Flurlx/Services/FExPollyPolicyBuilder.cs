using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Flurlx.Configuration;
using Flurl.Http;
using Flurl.Util;
using Polly;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
    private readonly IFExLogger _logger;

    public FExPollyPolicyBuilder(IFExLogger logger)
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
#pragma warning disable IDISP001 // Polly policy, stateless, composed via WrapAsync
        var bulkheadPolicy = BuildBulkheadPolicy(config);

        var fallbackPolicy = config.EnableFallback
            ? BuildFallbackPolicy()
            : Policy.NoOpAsync<IFlurlResponse>();
#pragma warning restore IDISP001

        // Wrap all policies together (order matters!)
        // Fallback -> Retry -> Circuit Breaker -> Timeout -> Bulkhead
        return fallbackPolicy.WrapAsync(retryPolicy)
            .WrapAsync(circuitBreakerPolicy)
            .WrapAsync(timeoutPolicy)
            .WrapAsync(bulkheadPolicy);
    }

    /// <summary>
    /// Creates a mock response for Service Unavailable (503).
    /// </summary>
    private static IFlurlResponse CreateServiceUnavailableResponse() =>
        // Create a mock response with 503 status code
        // This is a fallback response when all retries are exhausted
        new FExFallbackResponse
        {
            StatusCode = (int)HttpStatusCode.ServiceUnavailable
        };

    private static async Task OnFallbackAsync(DelegateResult<IFlurlResponse> delegateResult, Context context)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Builds a Retry policy with exponential backoff.
    /// Handles transient errors: connection failures, timeouts, 5xx errors.
    /// </summary>
    private IAsyncPolicy<IFlurlResponse> BuildRetryPolicy(PollyPolicyConfiguration config) =>
        Policy.Handle<FlurlHttpException>()
            .Or<TimeoutRejectedException>()
            .Or<HttpRequestException>() // ✅ Connection failures
            .Or<SocketException>() // ✅ Socket errors
            .Or<TaskCanceledException>() // ✅ Request cancellation/timeout
            .OrResult<IFlurlResponse>(static r => r == null
                                                  || r.StatusCode == (int)HttpStatusCode.RequestTimeout
                                                  || r.StatusCode == 429 // TooManyRequests
                                                  || r.StatusCode >= (int)HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(config.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * config.InitialRetryDelay.TotalSeconds),
                (outcome, timespan, retryCount, _) =>
                {
                    _logger.Warning(
                        $"[FExPolly] Retry {retryCount}/{config.MaxRetryAttempts} after {timespan.TotalSeconds:F1}s due to {(outcome.Exception is not null ? $"exception: {outcome.Exception.Message}" : $"status code: {outcome.Result?.StatusCode}")}");
                });

    /// <summary>
    /// Builds a Circuit Breaker policy to prevent cascading failures.
    /// </summary>
    private IAsyncPolicy<IFlurlResponse> BuildCircuitBreakerPolicy(PollyPolicyConfiguration config)
    {
        return Policy.Handle<FlurlHttpException>()
            .Or<TimeoutRejectedException>()
            .Or<HttpRequestException>()
            .Or<SocketException>()
            .OrResult<IFlurlResponse>(r => r == null || r.StatusCode >= (int)HttpStatusCode.InternalServerError)
            .CircuitBreakerAsync(config.CircuitBreakerFailureThreshold,
                config.CircuitBreakerDuration,
                (_, duration) =>
                {
                    _logger.Error(
                        $"[FExPolly] Circuit breaker OPENED for {duration.TotalSeconds:F0}s after {config.CircuitBreakerFailureThreshold} failures");
                },
                () => { _logger.Information("[FExPolly] Circuit breaker CLOSED - endpoint recovered"); },
                () => { _logger?.Information("[FExPolly] Circuit breaker HALF-OPEN - testing endpoint"); });
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
                _logger.Warning($"[FExPolly] Request timed out after {timespan.TotalSeconds:F1}s");

                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Builds a Bulkhead policy to limit concurrent requests.
    /// Critical for slow APIs to prevent overwhelming the endpoint.
    /// </summary>
    private IAsyncPolicy<IFlurlResponse> BuildBulkheadPolicy(PollyPolicyConfiguration config)
    {
#pragma warning disable IDISP005 // Polly BulkheadAsync policy, stateless
        return Policy.BulkheadAsync<IFlurlResponse>(config.MaxParallelization,
            config.MaxQueuingActions,
            _ =>
            {
                _logger.Warning(
                    $"[FExPolly] Bulkhead rejected request - {config.MaxParallelization} concurrent + {config.MaxQueuingActions} queued limit reached");

                return Task.CompletedTask;
            });
#pragma warning restore IDISP005
    }

    /// <summary>
    /// Builds a Fallback policy for graceful degradation.
    /// Returns a 503 Service Unavailable response when all retries are exhausted.
    /// </summary>
    private IAsyncPolicy<IFlurlResponse> BuildFallbackPolicy()
    {
        return Policy<IFlurlResponse>.Handle<Exception>()
            .FallbackAsync((result, context, _) =>
                {
                    _logger?.Error(
                        $"[FExPolly] Fallback activated due to: {result.Exception?.Message ?? "Unknown error"}");

                    // Future enhancement: Try to get cached response
                    if (context.TryGetValue("CacheKey", out var cacheKey))
                        _logger?.Information($"[FExPolly] Attempting to retrieve cached data for key: {cacheKey}");

                    // Create a fallback response with 503 Service Unavailable
                    var fallbackResponse = CreateServiceUnavailableResponse();

                    return Task.FromResult(fallbackResponse);
                },
                OnFallbackAsync);
    }

    /// <summary>
    /// Fallback response implementation for graceful degradation.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private sealed class FExFallbackResponse : IFlurlResponse
    {
        public int StatusCode { get; init; }
        public IFlurlRequest Request => null;
        public HttpResponseMessage ResponseMessage => null;
        public IReadOnlyList<FlurlCookie> Cookies => null;
        public IReadOnlyNameValueList<string> Headers => null;

        public Task<T> GetJsonAsync<T>() => Task.FromResult<T>(default);
        public Task<string> GetStringAsync() => Task.FromResult("Service temporarily unavailable");
        public Task<byte[]> GetBytesAsync() => Task.FromResult(Array.Empty<byte>());
        public Task<Stream> GetStreamAsync() => Task.FromResult(Stream.Null);

        #region IDisposable
        public void Dispose()
        {
            // No resources to dispose
        }
        #endregion
    }
}