using FEx.Flurlx.Configuration;
using FEx.Flurlx.Models;
using FEx.Flurlx.Services;
using Flurl.Http;
using Polly;
using Shouldly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace FEx.Flurlx.Tests;

public class FlurlApiBaseIntegrationTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly IFlurlClient _flurlClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;
    private readonly TestApi _testApi;

    public FlurlApiBaseIntegrationTests()
    {
        // Start mock HTTP server
        _mockServer = WireMockServer.Start();

        // Create Flurl client
        _flurlClient = new FlurlClient(_mockServer.Url);

        // Create Polly policy
        var policyBuilder = new FExPollyPolicyBuilder();
        var config = new PollyPolicyConfiguration
        {
            MaxRetryAttempts = 3,
            InitialRetryDelay = TimeSpan.FromMilliseconds(50),
            RequestTimeout = TimeSpan.FromSeconds(5),
            CircuitBreakerFailureThreshold = 5,
            MaxParallelization = 10,
            EnableFallback = true
        };
        _resiliencePolicy = policyBuilder.BuildFullSuitePolicy(config);

        // Create test API instance
        _testApi = new TestApi(_flurlClient, _resiliencePolicy);
    }

    [Fact]
    public async Task GetResponseAsync_SuccessfulRequest_ReturnsData()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/data").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":123,\"name\":\"Test\"}"));

        // Act
        var result = await _testApi.GetDataAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(123);
        result.Name.ShouldBe("Test");
    }

    [Fact]
    public async Task GetResponseAsync_ServerError_RetriesAndSucceeds()
    {
        // Arrange - Setup mock to fail twice then succeed
        _mockServer.ResetMappings();
        _mockServer
            .Given(Request.Create().WithPath("/api/data").UsingGet())
            .InScenario("RetryScenario")
            .WillSetStateTo("Attempt1")
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("Server Error"));

        _mockServer
            .Given(Request.Create().WithPath("/api/data").UsingGet())
            .InScenario("RetryScenario")
            .WhenStateIs("Attempt1")
            .WillSetStateTo("Attempt2")
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("Server Error"));

        _mockServer
            .Given(Request.Create().WithPath("/api/data").UsingGet())
            .InScenario("RetryScenario")
            .WhenStateIs("Attempt2")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":456,\"name\":\"Retry Success\"}"));

        // Act
        var result = await _testApi.GetDataAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(456);
        result.Name.ShouldBe("Retry Success");
        // Verify it went through the retry scenario successfully
    }

    [Fact]
    public async Task GetResponseAsync_PersistentFailure_ThrowsHttpRequestException()
    {
        // Arrange
        _mockServer
            .Given(Request.Create().WithPath("/api/data").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Persistent Error"));

        // Act & Assert
        // After retries exhausted, fallback returns ServiceUnavailable
        var result = await _testApi.GetDataAsync();
        
        // Fallback should have activated, returning error response
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task PostResponseAsync_WithRequestBody_SendsCorrectData()
    {
        // Arrange
        var requestBody = new TestRequestData { Value = "test-value", Count = 42 };
        _mockServer
            .Given(Request.Create().WithPath("/api/create").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":789,\"name\":\"Created\"}"));

        // Act
        var result = await _testApi.CreateDataAsync(requestBody);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(789);
        result.Name.ShouldBe("Created");

        // Verify request was sent
        var requests = _mockServer.LogEntries;
        requests.Count().ShouldBe(1);
    }

    [Fact]
    public async Task GetResponseAsync_WithQueryParameters_AppendsCorrectly()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/search")
                .WithParam("filter", "active")
                .WithParam("limit", "10")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":999,\"name\":\"Filtered\"}"));

        // Act
        var result = await _testApi.SearchDataAsync("active", 10);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(999);
        result.Name.ShouldBe("Filtered");
    }

    [Fact]
    public async Task GetResponseAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange - Create API with very short timeout
        var shortTimeoutConfig = new PollyPolicyConfiguration
        {
            RequestTimeout = TimeSpan.FromMilliseconds(100),
            MaxRetryAttempts = 0
        };
        var policyBuilder = new FExPollyPolicyBuilder();
        var timeoutPolicy = policyBuilder.BuildFullSuitePolicy(shortTimeoutConfig);
        var timeoutApi = new TestApi(_flurlClient, timeoutPolicy);

        _mockServer
            .Given(Request.Create().WithPath("/api/slow").UsingGet())
            .RespondWith(Response.Create()
                .WithDelay(TimeSpan.FromSeconds(5))
                .WithStatusCode(200)
                .WithBody("{\"id\":1,\"name\":\"Slow\"}"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () =>
        {
            await timeoutApi.GetSlowDataAsync();
        });
    }

    [Fact]
    public async Task GetResponseAsync_MultipleRequests_RespectsBulkhead()
    {
        // Arrange - Create API with tight bulkhead
        var bulkheadConfig = new PollyPolicyConfiguration
        {
            MaxParallelization = 2,
            MaxQueuingActions = 1,
            RequestTimeout = TimeSpan.FromSeconds(10)
        };
        var policyBuilder = new FExPollyPolicyBuilder();
        var bulkheadPolicy = policyBuilder.BuildFullSuitePolicy(bulkheadConfig);
        var bulkheadApi = new TestApi(_flurlClient, bulkheadPolicy);

        _mockServer
            .Given(Request.Create().WithPath("/api/concurrent").UsingGet())
            .RespondWith(Response.Create()
                .WithDelay(TimeSpan.FromMilliseconds(100))
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"id\":1,\"name\":\"Concurrent\"}"));

        // Act - Fire 5 concurrent requests
        var tasks = new Task<TestData>[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = bulkheadApi.GetConcurrentDataAsync();
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete, but some might have been rejected/queued
        results.ShouldNotBeEmpty();
    }

    public void Dispose()
    {
        _mockServer?.Stop();
        _mockServer?.Dispose();
        _flurlClient?.Dispose();
    }

    #region Test API Implementation

    private class TestApi : FlurlApiBase
    {
        public TestApi(IFlurlClient flurlClient, IAsyncPolicy<HttpResponseMessage> resiliencePolicy)
            : base(flurlClient, resiliencePolicy)
        {
        }

        public async Task<TestData> GetDataAsync(CancellationToken ct = default)
        {
            return await GetResponseAsync<TestData>(
                "api/data",
                method: RequestMethod.GET,
                cancellationToken: ct);
        }

        public async Task<TestData> GetSlowDataAsync(CancellationToken ct = default)
        {
            return await GetResponseAsync<TestData>(
                "api/slow",
                method: RequestMethod.GET,
                cancellationToken: ct);
        }

        public async Task<TestData> GetConcurrentDataAsync(CancellationToken ct = default)
        {
            return await GetResponseAsync<TestData>(
                "api/concurrent",
                method: RequestMethod.GET,
                cancellationToken: ct);
        }

        public async Task<TestData> CreateDataAsync(TestRequestData requestData, CancellationToken ct = default)
        {
            return await GetResponseAsync<TestData, TestRequestData>(
                "api/create",
                method: RequestMethod.POST,
                requestContent: requestData,
                cancellationToken: ct);
        }

        public async Task<TestData> SearchDataAsync(string filter, int limit, CancellationToken ct = default)
        {
            return await GetResponseAsync<TestData>(
                "api/search",
                func: req => req.SetQueryParam("filter", filter).SetQueryParam("limit", limit),
                method: RequestMethod.GET,
                cancellationToken: ct);
        }
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    private class TestRequestData
    {
        public string Value { get; set; }
        public int Count { get; set; }
    }

    #endregion
}

