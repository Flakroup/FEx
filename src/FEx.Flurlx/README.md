# FEx.Flurlx - Resilient HTTP Client Module

## Overview

`FEx.Flurlx` provides a modern, resilient HTTP client infrastructure built on **Flurl.Http** and **Polly**. It delivers comprehensive failure handling, automatic retries, circuit breaking, request throttling, and graceful degradation for API integrations.

## Key Features

- **🔄 Automatic Retries** - Exponential backoff for transient failures
- **⚡ Circuit Breaker** - Prevents cascading failures when endpoints degrade
- **⏱️ Timeout Protection** - Prevents hanging requests
- **🚦 Bulkhead Isolation** - Limits concurrent requests (critical for slow APIs)
- **🛡️ Fallback Support** - Graceful degradation with cached responses
- **📝 Comprehensive Logging** - Track resilience policy activations
- **⚙️ Configurable Policies** - Fine-tune resilience per API

## FExPollyPolicyBuilder

The `FExPollyPolicyBuilder` is the heart of FEx.Flurlx's resilience strategy. It creates a comprehensive Polly policy suite that wraps HTTP requests with multiple layers of protection.

### Basic Usage

```csharp
using FEx.Flurlx.Configuration;
using FEx.Flurlx.Services;
using FEx.Logging.Abstractions.Extensions;

// Create policy builder (optionally with logger)
var logger = this.GetLogger();
var policyBuilder = new FExPollyPolicyBuilder(logger);

// Create configuration
var config = new PollyPolicyConfiguration
{
    MaxRetryAttempts = 3,
    RequestTimeout = TimeSpan.FromSeconds(30),
    MaxParallelization = 10
};

// Build the full resilience policy
var resiliencePolicy = policyBuilder.BuildFullSuitePolicy(config);

// Use with FlurlApiBase or directly with HttpClient
var response = await resiliencePolicy.ExecuteAsync(async ct =>
{
    return await flurlClient.Request("api/endpoint")
        .GetAsync(cancellationToken: ct)
        .ResponseMessage;
});
```

### Policy Configuration

#### Default Configuration

```csharp
var config = new PollyPolicyConfiguration(); // Uses sensible defaults
```

**Defaults:**
- **Retry**: 3 attempts with 1s initial delay
- **Timeout**: 30 seconds per request
- **Circuit Breaker**: Opens after 5 consecutive failures, stays open for 1 minute
- **Bulkhead**: 10 concurrent requests, 20 queued
- **Fallback**: Enabled

#### Slow API Configuration

For slow/unreliable APIs (e.g., Synology DSM, IoT devices):

```csharp
var config = PollyPolicyConfiguration.SlowApiDefaults();
```

**Slow API Defaults:**
- **Retry**: 5 attempts with 2s initial delay
- **Timeout**: 2 minutes per request
- **Circuit Breaker**: Opens after 10 consecutive failures, stays open for 5 minutes
- **Bulkhead**: 3 concurrent requests, 10 queued
- **Fallback**: Enabled

#### Fast API Configuration

For fast, reliable APIs (e.g., internal microservices):

```csharp
var config = PollyPolicyConfiguration.FastApiDefaults();
```

**Fast API Defaults:**
- **Retry**: 2 attempts with 500ms initial delay
- **Timeout**: 10 seconds per request
- **Circuit Breaker**: Opens after 3 consecutive failures, stays open for 30 seconds
- **Bulkhead**: 20 concurrent requests, 50 queued
- **Fallback**: Disabled

#### Custom Configuration

```csharp
var config = new PollyPolicyConfiguration
{
    // Retry settings
    MaxRetryAttempts = 5,
    InitialRetryDelay = TimeSpan.FromSeconds(2),
    
    // Timeout
    RequestTimeout = TimeSpan.FromMinutes(1),
    
    // Circuit Breaker
    CircuitBreakerFailureThreshold = 10,
    CircuitBreakerDuration = TimeSpan.FromMinutes(5),
    
    // Bulkhead (for slow APIs)
    MaxParallelization = 3,
    MaxQueuingActions = 10,
    
    // Fallback
    EnableFallback = true
};
```

### Understanding the Policies

#### 1. **Retry Policy**

Automatically retries failed requests with exponential backoff.

**Triggers Retry On:**
- `HttpRequestException` (network failures)
- `TimeoutRejectedException` (timeouts)
- HTTP 408 (Request Timeout)
- HTTP 429 (Too Many Requests)
- HTTP 5xx (Server Errors)

**Retry Delay Formula:**
```
delay = 2^retryAttempt * InitialRetryDelay
```

**Example:**
- Attempt 1: Wait 2 seconds
- Attempt 2: Wait 4 seconds
- Attempt 3: Wait 8 seconds

#### 2. **Circuit Breaker Policy**

Prevents overwhelming a failing endpoint by "opening the circuit" after too many failures.

**States:**
- **Closed** (Normal): All requests pass through
- **Open** (Tripped): All requests fail immediately
- **Half-Open** (Testing): Single test request allowed

**Example:**
After 5 consecutive failures (default), circuit opens for 1 minute. No requests are sent during this time. After 1 minute, one test request is allowed. If it succeeds, circuit closes. If it fails, circuit opens for another minute.

#### 3. **Timeout Policy**

Prevents requests from hanging indefinitely.

**Strategy:** Pessimistic timeout (hard cancellation)

**Example:**
If `RequestTimeout = 30s`, any request taking longer than 30 seconds is cancelled.

#### 4. **Bulkhead Policy**

Limits concurrent requests to prevent overwhelming slow APIs.

**Parameters:**
- `MaxParallelization`: Maximum concurrent requests
- `MaxQueuingActions`: Maximum queued requests waiting for execution

**Example:**
With `MaxParallelization = 3` and `MaxQueuingActions = 10`:
- First 3 requests execute immediately
- Next 10 requests wait in queue
- 14th request is rejected immediately (BulkheadRejectedException)

**Critical for Slow APIs:** Prevents 100s of concurrent requests to a slow endpoint (e.g., Synology DSM).

#### 5. **Fallback Policy**

Provides graceful degradation when all other policies fail.

**Default Behavior:**
Returns HTTP 503 (Service Unavailable) with error message.

**Future Enhancement:**
Cache integration to return stale data when API is unavailable.

### Policy Wrapping Order

Policies are applied in a specific order (innermost to outermost):

```
Fallback
  └─ Retry
      └─ Circuit Breaker
          └─ Timeout
              └─ Bulkhead
                  └─ HTTP Request
```

**Why This Order?**
1. **Bulkhead** (innermost): Limits concurrent execution first
2. **Timeout**: Cancels individual request attempts
3. **Circuit Breaker**: Prevents retrying a known-bad endpoint
4. **Retry**: Handles transient failures
5. **Fallback** (outermost): Last resort when everything fails

### Using with FlurlApiBase

The typical pattern is to inject the policy into your API client:

```csharp
public class MySynoApi : FlurlApiBase
{
    public MySynoApi(
        IFlurlClient flurlClient,
        IAsyncPolicy<HttpResponseMessage> resiliencePolicy)
        : base(flurlClient, resiliencePolicy)
    {
    }

    public async Task<MyData> GetDataAsync(CancellationToken ct = default)
    {
        // Automatically protected by all Polly policies
        return await GetResponseAsync<MyData>(
            "api/data",
            func: req => req.SetQueryParam("filter", "active"),
            method: RequestMethod.GET,
            cancellationToken: ct);
    }
}
```

### Logging

When a logger is provided, `FExPollyPolicyBuilder` logs all resilience events:

**Retry:**
```
[FExPolly] Retry 1/3 after 2.0s due to status code: InternalServerError
```

**Circuit Breaker:**
```
[FExPolly] Circuit breaker OPENED for 60s after 5 failures
[FExPolly] Circuit breaker HALF-OPEN - testing endpoint
[FExPolly] Circuit breaker CLOSED - endpoint recovered
```

**Timeout:**
```
[FExPolly] Request timed out after 30.0s
```

**Bulkhead:**
```
[FExPolly] Bulkhead rejected request - 10 concurrent + 20 queued limit reached
```

**Fallback:**
```
[FExPolly] Fallback activated due to: Connection refused
```

### Real-World Example: Synology DSM API

Synology DSM APIs are notoriously slow and unreliable. Here's a production-ready configuration:

```csharp
public class SynologyApiConfiguration : IApiConfiguration
{
    public Url BaseUrl => "https://my-synology:5001";
    public string ClientName => "SynologyDSM";
    public bool IgnoreSSLErrors => true; // Self-signed cert
    
    public PollyPolicyConfiguration PollyConfig => new()
    {
        // Generous retries (Synology can be slow)
        MaxRetryAttempts = 5,
        InitialRetryDelay = TimeSpan.FromSeconds(3),

        // Long timeout (file operations take time)
        RequestTimeout = TimeSpan.FromMinutes(5),

        // Persistent circuit breaker (don't hammer the NAS)
        CircuitBreakerFailureThreshold = 10,
        CircuitBreakerDuration = TimeSpan.FromMinutes(10),

        // Strict concurrency (NAS has limited resources)
        MaxParallelization = 2,
        MaxQueuingActions = 5,

        // Enable fallback to return cached file list
        EnableFallback = true
    };
}
```

## Integration with FEx DI

The module uses StrongInject for dependency injection:

```csharp
// In your composition root
services.AddSingleton<IApiConfiguration, MySynologyApiConfiguration>();

// FlurlConfigurator automatically creates and exposes the resilience policy
services.AddSingleton<IFlurlConfigurator, FlurlConfigurator>();

// Get the policy from the configurator
var policy = serviceProvider
    .GetRequiredService<IFlurlConfigurator>()
    .GetResiliencePolicy();
```

## Performance Considerations

### Bulkhead is Your Friend

For slow APIs, **always configure Bulkhead** to prevent resource exhaustion:

```csharp
MaxParallelization = 3,  // Only 3 concurrent requests
MaxQueuingActions = 10   // Max 10 waiting requests
```

Without Bulkhead, 100 threads making slow API calls = 100 hung connections.

### Circuit Breaker Saves Resources

When an API goes down, Circuit Breaker prevents wasting time on doomed requests:

```csharp
CircuitBreakerFailureThreshold = 5,
CircuitBreakerDuration = TimeSpan.FromMinutes(1)
```

After 5 failures, requests fail immediately for 1 minute instead of timing out.

### Timeout Prevents Hangs

Always set reasonable timeouts:

```csharp
RequestTimeout = TimeSpan.FromSeconds(30)  // Don't let requests hang forever
```

## Migration from FlakEssentials.Flurl

If migrating from the old `FlakEssentials.Flurl` (AsyncWorkersService-based):

**Before:**
```csharp
public class MyApi : FlurlApiBase<MyFlurlClientService>
{
    public MyApi(MyFlurlClientService flurlClientService)
        : base(flurlClientService)
    {
    }
}
```

**After:**
```csharp
public class MyApi : FlurlApiBase
{
    public MyApi(
        IFlurlClient flurlClient,
        IAsyncPolicy<HttpResponseMessage> resiliencePolicy)
        : base(flurlClient, resiliencePolicy)
    {
    }
}
```

**Benefits:**
- ✅ No more complex worker pool management
- ✅ Industry-standard Polly policies
- ✅ Better observability with logging
- ✅ More flexible configuration
- ✅ Easier testing (mock policies)

## Testing

Mock the policy for unit tests:

```csharp
// No-op policy for testing
var testPolicy = Policy.NoOpAsync<HttpResponseMessage>();

var api = new MyApi(mockFlurlClient, testPolicy);
```

Or test resilience behavior:

```csharp
// Simulate circuit breaker
var circuitBreakerPolicy = Policy
    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .CircuitBreakerAsync(1, TimeSpan.FromSeconds(30));

// First failure opens circuit
await circuitBreakerPolicy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

// Second request fails immediately with BrokenCircuitException
await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
    await circuitBreakerPolicy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))
);
```

## Troubleshooting

### Circuit Breaker Opening Too Often

**Symptom:** Circuit keeps opening even though API is healthy

**Fix:** Increase failure threshold or reduce status codes that trigger it:

```csharp
CircuitBreakerFailureThreshold = 10  // More tolerant
```

### Bulkhead Rejecting Requests

**Symptom:** `BulkheadRejectedException` even with low load

**Fix:** Increase parallelization or queue size:

```csharp
MaxParallelization = 20,
MaxQueuingActions = 50
```

### Retries Not Happening

**Symptom:** Requests fail immediately without retries

**Fix:** Check if Circuit Breaker is open (it bypasses retry). Increase circuit duration or disable it temporarily.

### Timeouts Too Aggressive

**Symptom:** Requests timeout even when API is responding

**Fix:** Increase timeout:

```csharp
RequestTimeout = TimeSpan.FromMinutes(2)  // For slow operations
```

## See Also

- [Polly Documentation](https://www.thepollyproject.org/)
- [Flurl Documentation](https://flurl.dev/)
- FEx.Logging - For logging integration
- FEx.Json - For JSON serialization

---

**FEx.Flurlx** - Resilient HTTP client module for the FEx framework  
Built with ❤️ for production-grade API integrations
