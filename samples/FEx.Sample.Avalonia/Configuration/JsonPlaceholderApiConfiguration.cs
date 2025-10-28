using FEx.Flurlx.Abstractions.Interfaces;
using FEx.Flurlx.Configuration;
using Flurl;
using System;

namespace FEx.Sample.Avalonia.Configuration;

/// <summary>
/// Configuration for JSONPlaceholder API client with Polly resilience.
/// </summary>
public class JsonPlaceholderApiConfiguration : IApiConfiguration
{
    public Url BaseUrl => new("https://jsonplaceholder.typicode.com");
    public string ClientName => "JsonPlaceholder";
    public bool IgnoreSSLErrors => false;

    public PollyPolicyConfiguration PollyConfig =>
        new()
        {
            MaxRetryAttempts = 3,
            InitialRetryDelay = TimeSpan.FromSeconds(1),
            RequestTimeout = TimeSpan.FromSeconds(10),
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerDuration = TimeSpan.FromSeconds(15),
            MaxParallelization = 10
        };
}