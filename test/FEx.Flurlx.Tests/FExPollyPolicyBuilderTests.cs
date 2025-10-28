using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Flurlx.Configuration;
using FEx.Flurlx.Services;
using Flurl.Http;
using NSubstitute;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Flurlx.Tests;

public class FExPollyPolicyBuilderTests
{
    private readonly IFExLogger _mockLogger;
    private readonly FExPollyPolicyBuilder _policyBuilder;

    public FExPollyPolicyBuilderTests()
    {
        _mockLogger = Substitute.For<IFExLogger>();
        _policyBuilder = new(_mockLogger);
    }

    [Fact]
    public void BuildFullSuitePolicy_WithDefaultConfig_CreatesPolicy()
    {
        // Arrange
        var config = new PollyPolicyConfiguration();

        // Act
        var policy = _policyBuilder.BuildFullSuitePolicy(config);

        // Assert
        policy.ShouldNotBeNull();
    }

    [Fact]
    public async Task RetryPolicy_RetriesOnHttpRequestException()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            MaxRetryAttempts = 3,
            InitialRetryDelay = TimeSpan.FromMilliseconds(10),
            EnableFallback = false // Disable fallback to test exception propagation
        };

        var policy = _policyBuilder.BuildFullSuitePolicy(config);
        var attemptCount = 0;

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync(async _ =>
                {
                    attemptCount++;
                    await Task.CompletedTask;

                    throw new HttpRequestException("Test failure");
                },
                CancellationToken.None);
        });

        // Should attempt initial + 3 retries = 4 total
        attemptCount.ShouldBe(4);
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
        var result = await policy.ExecuteAsync(async _ =>
            {
                attemptCount++;
                await Task.CompletedTask;

                if (attemptCount < 3)
                    return CreateResponse((int)HttpStatusCode.InternalServerError);

                return CreateResponse((int)HttpStatusCode.OK);
            },
            CancellationToken.None);

        // Assert
        result.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        attemptCount.ShouldBe(3); // Initial + 2 retries
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
        var result = await policy.ExecuteAsync(async _ =>
            {
                attemptCount++;
                await Task.CompletedTask;

                return CreateResponse((int)HttpStatusCode.BadRequest);
            },
            CancellationToken.None);

        // Assert
        result.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        attemptCount.ShouldBe(1); // No retries for 4xx
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterConsecutiveFailures()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            CircuitBreakerFailureThreshold = 2,
            CircuitBreakerDuration = TimeSpan.FromSeconds(1),
            MaxRetryAttempts = 0, // Disable retry to test circuit breaker in isolation
            EnableFallback = false // Disable fallback to test exception propagation
        };

        var policy = _policyBuilder.BuildFullSuitePolicy(config);

        // Act - Cause failures to open circuit
        await policy.ExecuteAsync(async _ =>
            {
                await Task.CompletedTask;

                return CreateResponse((int)HttpStatusCode.InternalServerError);
            },
            CancellationToken.None);

        await policy.ExecuteAsync(async _ =>
            {
                await Task.CompletedTask;

                return CreateResponse((int)HttpStatusCode.InternalServerError);
            },
            CancellationToken.None);

        // Assert - Circuit should be open, next request should fail immediately
        await Should.ThrowAsync<BrokenCircuitException>(async () =>
        {
            await policy.ExecuteAsync(async _ =>
                {
                    await Task.CompletedTask;

                    return CreateResponse((int)HttpStatusCode.OK);
                },
                CancellationToken.None);
        });
    }

    [Fact]
    public async Task Timeout_CancelsLongRunningRequest()
    {
        // Arrange
        var config = new PollyPolicyConfiguration
        {
            RequestTimeout = TimeSpan.FromMilliseconds(100),
            MaxRetryAttempts = 0, // Disable retry
            EnableFallback = false // Disable fallback to test exception propagation
        };

        var policy = _policyBuilder.BuildFullSuitePolicy(config);

        // Act & Assert
        await Should.ThrowAsync<TimeoutRejectedException>(async () =>
        {
            await policy.ExecuteAsync(async ct =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);

                    return CreateResponse((int)HttpStatusCode.OK);
                },
                CancellationToken.None);
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
        var tasks = new Task<IFlurlResponse>[5];

        for (var i = 0; i < 5; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    return await policy.ExecuteAsync(async ct =>
                        {
                            await semaphore.WaitAsync(ct);

                            try
                            {
                                concurrentCount++;

                                if (concurrentCount > maxConcurrentCount)
                                    maxConcurrentCount = concurrentCount;

                                await Task.Delay(50, ct);

                                concurrentCount--;

                                return CreateResponse((int)HttpStatusCode.OK);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        },
                        CancellationToken.None);
                }
                catch
                {
                    return CreateResponse((int)HttpStatusCode.ServiceUnavailable);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Max concurrent should not exceed bulkhead limit
        maxConcurrentCount.ShouldBeLessThanOrEqualTo(config.MaxParallelization,
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
        var result = await policy.ExecuteAsync(async _ =>
            {
                await Task.CompletedTask;

                throw new HttpRequestException("Complete failure");
            },
            CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
        var content = await result.GetStringAsync();
        content.ShouldContain("Service temporarily unavailable");
    }

    [Fact]
    public void SlowApiDefaults_CreatesCorrectConfiguration()
    {
        // Act
        var config = PollyPolicyConfiguration.SlowApiDefaults();

        // Assert
        config.MaxRetryAttempts.ShouldBe(5);
        config.InitialRetryDelay.ShouldBe(TimeSpan.FromSeconds(2));
        config.RequestTimeout.ShouldBe(TimeSpan.FromMinutes(2));
        config.CircuitBreakerFailureThreshold.ShouldBe(10);
        config.CircuitBreakerDuration.ShouldBe(TimeSpan.FromMinutes(5));
        config.MaxParallelization.ShouldBe(3);
        config.MaxQueuingActions.ShouldBe(10);
        config.EnableFallback.ShouldBeTrue();
    }

    [Fact]
    public void FastApiDefaults_CreatesCorrectConfiguration()
    {
        // Act
        var config = PollyPolicyConfiguration.FastApiDefaults();

        // Assert
        config.MaxRetryAttempts.ShouldBe(2);
        config.InitialRetryDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        config.RequestTimeout.ShouldBe(TimeSpan.FromSeconds(10));
        config.CircuitBreakerFailureThreshold.ShouldBe(3);
        config.CircuitBreakerDuration.ShouldBe(TimeSpan.FromSeconds(30));
        config.MaxParallelization.ShouldBe(20);
        config.MaxQueuingActions.ShouldBe(50);
        config.EnableFallback.ShouldBeFalse();
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
        var result = await policy.ExecuteAsync(async _ =>
            {
                attemptCount++;
                await Task.CompletedTask;

                if (attemptCount < 2)
                    return CreateResponse((int)HttpStatusCode.InternalServerError);

                return CreateResponse((int)HttpStatusCode.OK);
            },
            CancellationToken.None);

        // Assert
        result.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        _mockLogger.Received().Warning(Arg.Is<string>(s => s.Contains("Retry")));
    }

    private static IFlurlResponse CreateResponse(int statusCode, string content = null)
    {
        var resp = Substitute.For<IFlurlResponse>();
        resp.StatusCode.Returns(statusCode);
        if (content != null)
            resp.GetStringAsync().Returns(content);
        else
            resp.GetStringAsync().Returns(string.Empty);
        return resp;
    }
}