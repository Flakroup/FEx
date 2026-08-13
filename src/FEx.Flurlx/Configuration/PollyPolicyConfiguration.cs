using System;

namespace FEx.Flurlx.Configuration;

/// <summary>
/// Configuration for Polly resilience policies.
/// </summary>
/// <remarks>
/// Provides comprehensive resilience configuration for HTTP requests including:
/// - Retry with exponential backoff
/// - Timeout handling
/// - Circuit breaker for failing endpoints
/// - Bulkhead for limiting concurrent requests (essential for slow APIs)
/// - Fallback for graceful degradation
/// </remarks>
public class PollyPolicyConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts before giving up.
    /// Default: 3 attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Initial delay before first retry. Subsequent retries use exponential backoff.
    /// Default: 1 second
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum timeout for a single HTTP request.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of consecutive failures before opening the circuit breaker.
    /// Default: 5 failures
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration the circuit breaker stays open before allowing test requests.
    /// Default: 1 minute
    /// </summary>
    public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum number of parallel requests allowed (Bulkhead limit).
    /// Essential for slow APIs to prevent overwhelming the endpoint.
    /// Default: 10 concurrent requests
    /// </summary>
    public int MaxParallelization { get; set; } = 10;

    /// <summary>
    /// Maximum number of requests that can be queued when Bulkhead limit is reached.
    /// Default: 20 queued actions
    /// </summary>
    public int MaxQueuingActions { get; set; } = 20;

    /// <summary>
    /// Enable fallback policy for graceful degradation (e.g., returning cached data).
    /// Default: true
    /// </summary>
    public bool EnableFallback { get; set; } = true;

    /// <summary>
    /// Creates a default configuration optimized for slow APIs (e.g., Synology DSM).
    /// </summary>
    public static PollyPolicyConfiguration SlowApiDefaults() =>
        new()
        {
            MaxRetryAttempts = 5,
            InitialRetryDelay = TimeSpan.FromSeconds(2),
            RequestTimeout = TimeSpan.FromMinutes(2),
            CircuitBreakerFailureThreshold = 10,
            CircuitBreakerDuration = TimeSpan.FromMinutes(5),
            MaxParallelization = 3,
            MaxQueuingActions = 10,
            EnableFallback = true
        };

    /// <summary>
    /// Creates a default configuration optimized for fast, reliable APIs.
    /// </summary>
    public static PollyPolicyConfiguration FastApiDefaults() =>
        new()
        {
            MaxRetryAttempts = 2,
            InitialRetryDelay = TimeSpan.FromMilliseconds(500),
            RequestTimeout = TimeSpan.FromSeconds(10),
            CircuitBreakerFailureThreshold = 3,
            CircuitBreakerDuration = TimeSpan.FromSeconds(30),
            MaxParallelization = 20,
            MaxQueuingActions = 50,
            EnableFallback = false
        };
}