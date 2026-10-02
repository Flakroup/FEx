using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Flurlx.Configuration;
using FEx.Flurlx.Services;
using Flurl.Http;
using NSubstitute;
using Polly;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Flurlx.Tests;

/// <summary>
/// Regression tests for #107: policy log lines must not leak secrets carried in the request URL.
/// </summary>
public sealed class FExPollyPolicyBuilderRedactionTests
{
    private const string Secret = "SECRET123";
    private const string Password = "P4ssw0rdXYZ";
    private const string QueryUrl = "https://api.example.com/v1/x?api_key=" + Secret + "&user=bob";
    private const string UserInfoUrl = "https://alice:" + Password + "@api.example.com/v1/x?token=" + Secret;

    private readonly IFExLogger _logger = Substitute.For<IFExLogger>();
    private readonly FExPollyPolicyBuilder _policyBuilder;

    public FExPollyPolicyBuilderRedactionTests()
    {
        _policyBuilder = new(_logger);
    }

    public static TheoryData<string> SecretUrls => new() { QueryUrl, UserInfoUrl };

    [Theory]
    [MemberData(nameof(SecretUrls))]
    public async Task RetryPolicy_DoesNotLogUrlSecrets(string url)
    {
        var policy = _policyBuilder.BuildFullSuitePolicy(new()
        {
            MaxRetryAttempts = 2,
            InitialRetryDelay = TimeSpan.FromMilliseconds(1),
            CircuitBreakerFailureThreshold = 100,
            EnableFallback = false
        });

        await Should.ThrowAsync<FlurlHttpException>(() => ExecuteAsync(policy, url, HttpStatusCode.InternalServerError));

        var logs = CapturedLogLines();
        logs.Count(static l => l.Contains("Retry")).ShouldBe(2);
        AssertNoSecrets(logs);
        logs.ShouldContain(static l => l.Contains("https://") && l.Contains("api.example.com/v1/x?"));
    }

    [Theory]
    [MemberData(nameof(SecretUrls))]
    public async Task RetryPolicy_DoesNotLogUrlSecrets_OnConnectionFailure(string url)
    {
        var policy = _policyBuilder.BuildFullSuitePolicy(new()
        {
            MaxRetryAttempts = 1,
            InitialRetryDelay = TimeSpan.FromMilliseconds(1),
            CircuitBreakerFailureThreshold = 100,
            EnableFallback = false
        });

        await Should.ThrowAsync<FlurlHttpException>(() => ExecuteAsync(policy,
            url,
            new HttpRequestException($"Connection refused ({url})")));

        var logs = CapturedLogLines();
        logs.ShouldContain(static l => l.Contains("Retry"));
        AssertNoSecrets(logs);
    }

    [Theory]
    [MemberData(nameof(SecretUrls))]
    public async Task CircuitBreakerPolicy_DoesNotLogUrlSecrets(string url)
    {
        var policy = _policyBuilder.BuildFullSuitePolicy(new()
        {
            MaxRetryAttempts = 3,
            InitialRetryDelay = TimeSpan.FromMilliseconds(1),
            CircuitBreakerFailureThreshold = 2,
            CircuitBreakerDuration = TimeSpan.FromMinutes(1),
            EnableFallback = false
        });

        await Should.ThrowAsync<Exception>(() => ExecuteAsync(policy, url, HttpStatusCode.ServiceUnavailable));

        var logs = CapturedLogLines();
        logs.ShouldContain(static l => l.Contains("Circuit breaker OPENED"));
        AssertNoSecrets(logs);
    }

    [Theory]
    [MemberData(nameof(SecretUrls))]
    public async Task FallbackPolicy_DoesNotLogUrlSecrets(string url)
    {
        var policy = _policyBuilder.BuildFullSuitePolicy(new()
        {
            MaxRetryAttempts = 1,
            InitialRetryDelay = TimeSpan.FromMilliseconds(1),
            CircuitBreakerFailureThreshold = 100,
            EnableFallback = true
        });

        var statusCode = await ExecuteAsync(policy, url, HttpStatusCode.InternalServerError);

        statusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
        var logs = CapturedLogLines();
        logs.ShouldContain(static l => l.Contains("Fallback activated"));
        AssertNoSecrets(logs);
    }

    [Fact]
    public async Task FallbackPolicy_KeepsSchemeHostPathAndParameterNames()
    {
        var policy = _policyBuilder.BuildFullSuitePolicy(new()
        {
            MaxRetryAttempts = 0,
            CircuitBreakerFailureThreshold = 100,
            EnableFallback = true
        });

        await ExecuteAsync(policy, UserInfoUrl + "&user=bob#frag", HttpStatusCode.InternalServerError);

        var fallbackLine = CapturedLogLines().Single(static l => l.Contains("Fallback activated"));
        fallbackLine.ShouldContain("https://***@api.example.com/v1/x?token=***&user=***#***");
        fallbackLine.ShouldNotContain("frag");
        fallbackLine.ShouldNotContain("bob");
    }

    private static async Task<int> ExecuteAsync(IAsyncPolicy<IFlurlResponse> policy,
                                                           string url,
                                                           HttpStatusCode statusCode)
    {
        using var handler = new StubHandler(_ => new(statusCode));

        return await ExecuteAsync(policy, url, handler);
    }

    private static async Task<int> ExecuteAsync(IAsyncPolicy<IFlurlResponse> policy,
                                                           string url,
                                                           Exception failure)
    {
        using var handler = new StubHandler(_ => throw failure);

        return await ExecuteAsync(policy, url, handler);
    }

    private static async Task<int> ExecuteAsync(IAsyncPolicy<IFlurlResponse> policy,
                                                           string url,
                                                           StubHandler handler)
    {
        using var httpClient = new HttpClient(handler, false);
        using var flurlClient = new FlurlClient(httpClient);

        using var response = await policy.ExecuteAsync(ct => flurlClient.Request(url).GetAsync(cancellationToken: ct),
            CancellationToken.None);

        return response.StatusCode;
    }

    private List<string> CapturedLogLines() =>
        _logger.ReceivedCalls()
            .SelectMany(static c => c.GetArguments())
            .Select(static a => a switch
            {
                string s => s,
                Exception e => e.ToString(),
                _ => null
            })
            .OfType<string>()
            .ToList();

    private static void AssertNoSecrets(List<string> logs)
    {
        logs.ShouldNotBeEmpty();

        foreach (var line in logs)
        {
            line.ShouldNotContain(Secret);
            line.ShouldNotContain(Password);
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken) =>
            Task.FromResult(_respond(request));
    }
}
