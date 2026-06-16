using FEx.Agnostics.Abstractions.Interfaces;
using Microsoft.Graph.Models.ODataErrors;
using Polly;
using System;
using System.Net.Http;

namespace FEx.OneDrv;

internal static class GraphResiliencePipeline
{
    internal static ResiliencePipeline Create(IFExLogger logger) =>
        new ResiliencePipelineBuilder().AddRetry(new()
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => new(
                    args.Outcome.Exception is ODataError e
                    && (e.ResponseStatusCode == 429 || e.ResponseStatusCode == 503)
                    || args.Outcome.Exception is HttpRequestException),
                OnRetry = args =>
                {
                    logger.Warning(
                        $"Graph API retry {args.AttemptNumber + 1}, delay {(int)args.RetryDelay.TotalMilliseconds}ms: {args.Outcome.Exception?.Message}");

                    return default;
                }
            })
            .Build();
}