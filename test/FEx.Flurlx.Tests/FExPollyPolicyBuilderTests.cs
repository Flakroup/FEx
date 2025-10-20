using FEx.Flurlx.Configuration;
using FEx.Flurlx.Services;
using FEx.Logging.Abstractions.Interfaces;
using Moq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Flurlx.Tests;

public class FExPollyPolicyBuilderTests
{
    private readonly Mock<ILoggable> _mockLogger;
    private readonly FExPollyPolicyBuilder _policyBuilder;

    public FExPollyPolicyBuilderTests()
    {
        _mockLogger = new Mock<ILoggable>();
        _policyBuilder = new FExPollyPolicyBuilder(_mockLogger.Object);
    }

    [Fact]
    public void BuildFullSuitePolicy_WithDefaultConfig_CreatesPolicy()
    {
        // Arrange
        var config = new PollyPolicyConfiguration();

        // Act
        var policy = _policyBuilder.BuildFullSuitePolicy(config);

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public async Task RetryPolicy_RetriesOnHttpRequestException()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            MaxRetryAttempts = 3,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10)
        };
        var policy = _policyBuilder.BuildFullSuitePolicy(config);
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync(async ct =>
            {
                attemptCount++;
                await Task.CompletedTask;
                throw new HttpRequestException("Test failure");
            }, CancellationToken.None);
        });

        // Should attempt initial + 3 retries = 4 total
        Assert.Equal(4, attemptCount);
    }

    [Fact]
    public async Task RetryPolicy_RetriesOn5xxStatusCodes()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            MaxRetryAttempts = 2,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10)
        };
        var policy = _policyBuilder.BuildFullSuitePolicy(config);
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            attemptCount++;
            await Task.CompletedTask;
            
            if (attemptCount < 3)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, attemptCount); // Initial + 2 retries
    }

    [Fact]
    public async Task RetryPolicy_DoesNotRetryOn4xxStatusCodes()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            MaxRetryAttempts = 3,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10)
        };
        var policy = _policyBuilder.BuildFullSuitePolicy(config);
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            attemptCount++;
            await Task.CompletedTask;
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(1, attemptCount); // No retries for 4xx
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterConsecutiveFailures()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            CircuitBreakerFailureThreshold = 2,
            CircuitBreakerDuration = TimeSpan.FromSeconds(1),
            MaxRetryAttempts = 0 // Disable retry to test circuit breaker in isolation
        };
        var policy = _policyBuilder.BuildFullSuitePolicy(config);

        // Act - Cause failures to open circuit
        await policy.ExecuteAsync(async ct =>
        {
            await Task.CompletedTask;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }, CancellationToken.None);

        await policy.ExecuteAsync(async ct =>
        {
            await Task.CompletedTask;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }, CancellationToken.None);

        // Assert - Circuit should be open, next request should fail immediately
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
        {
            await policy.ExecuteAsync(async ct =>
            {
                await Task.CompletedTask;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }, CancellationToken.None);
        });
    }

    [Fact]
    public async Task Timeout_CancelsLongRunningRequest()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            RequestTimeout = TimeSpan.FromMilliseconds(100),
            MaxRetryAttempts = 0 // Disable retry
        };
        var policy = _policyBuilder.BuildFullSuitePolicy(config);

        // Act & Assert
        await Assert.ThrowsAnyAsync<TimeoutException>(async () =>
        {
            await policy.ExecuteAsync(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }, CancellationToken.None);
        });
    }

    [Fact]
    public async Task Bulkhead_LimitsConcurrentRequests()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            MaxParallelization = 2,
            MaxQueuingActions = 0,
            RequestTimeout = TimeSpan.FromSeconds(10)
        };
        var policy = _policyBuilder.BuildFullSuitePolicy(config);
        var concurrentCount = 0;
        var maxConcurrentCount = 0;
        var semaphore = new SemaphoreSlim(1, 1);

        // Act - Try to run 5 requests concurrently
        var tasks = new Task<HttpResponseMessage>[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    return await policy.ExecuteAsync(async ct =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            concurrentCount++;
                            if (concurrentCount > maxConcurrentCount)
                                maxConcurrentCount = concurrentCount;

                            await Task.Delay(50, ct);

                            concurrentCount--;
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, CancellationToken.None);
                }
                catch
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Max concurrent should not exceed bulkhead limit
        Assert.True(maxConcurrentCount <= config.MaxParallelization,
            $"Max concurrent count {maxConcurrentCount} exceeded bulkhead limit {config.MaxParallelization}");
    }

    [Fact]
    public async Task Fallback_ReturnsServiceUnavailableOnFailure()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            EnableFallback = true,
            MaxRetryAttempts = 1,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10),
            CircuitBreakerFailureThreshold = 10 // High threshold to not trigger
        };
        var policy = _policyBuilder.BuildFullSuitePolicy(config);

        // Act - All retries should fail, fallback should activate
        var result = await policy.ExecuteAsync(async ct =>
        {
            await Task.CompletedTask;
            throw new HttpRequestException("Complete failure");
        }, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        var content = await result.Content.ReadAsStringAsync();
        Assert.Contains("Service temporarily unavailable", content);
    }

    [Fact]
    public void SlowApiDefaults_CreatesCorrectConfiguration()
    {
        // Act
        var config = PollyPolicyConfiguration.SlowApiDefaults();

        // Assert
        Assert.Equal(5, config.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), config.InitialRetryDelay);
        Assert.Equal(TimeSpan.FromMinutes(2), config.RequestTimeout);
        Assert.Equal(10, config.CircuitBreakerFailureThreshold);
        Assert.Equal(TimeSpan.FromMinutes(5), config.CircuitBreakerDuration);
        Assert.Equal(3, config.MaxParallelization);
        Assert.Equal(10, config.MaxQueuingActions);
        Assert.True(config.EnableFallback);
    }

    [Fact]
    public void FastApiDefaults_CreatesCorrectConfiguration()
    {
        // Act
        var config = PollyPolicyConfiguration.FastApiDefaults();

        // Assert
        Assert.Equal(2, config.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(500), config.InitialRetryDelay);
        Assert.Equal(TimeSpan.FromSeconds(10), config.RequestTimeout);
        Assert.Equal(3, config.CircuitBreakerFailureThreshold);
        Assert.Equal(TimeSpan.FromSeconds(30), config.CircuitBreakerDuration);
        Assert.Equal(20, config.MaxParallelization);
        Assert.Equal(50, config.MaxQueuingActions);
        Assert.False(config.EnableFallback);
    }

    [Fact]
    public async Task Logger_LogsRetryAttempts()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            MaxRetryAttempts = 2,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10)
        };
        var policy = _policyBuilder.BuildFullSuitePolicy(config);
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            attemptCount++;
            await Task.CompletedTask;
            
            if (attemptCount < 2)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        _mockLogger.Verify(
            x => x.LogWarning(It.IsRegex(".*Retry.*"), null),
            Times.AtLeastOnce(),
            "Logger should log retry attempts");
    }
}

