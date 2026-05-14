using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv;
using Microsoft.Graph.Models.ODataErrors;
using NSubstitute;
using Shouldly;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.OneDrv.Tests;

public sealed class GraphResiliencePipelineTests
{
    private readonly IFExLogger _logger;

    public GraphResiliencePipelineTests()
    {
        _logger = Substitute.For<IFExLogger>();
    }

    [Fact]
    public async Task Pipeline_RetriesOn429RateLimit_UntilSuccess()
    {
        var pipeline = GraphResiliencePipeline.Create(_logger);
        var attemptCount = 0;

        await pipeline.ExecuteAsync(async _ =>
        {
            attemptCount++;
            await Task.CompletedTask;
            if (attemptCount < 3)
                throw new ODataError { ResponseStatusCode = 429 };
        }, CancellationToken.None);

        attemptCount.ShouldBe(3);
        _logger.Received(2).Warning(Arg.Any<string>());
    }

    [Fact]
    public async Task Pipeline_RetriesOn503Transient_UntilSuccess()
    {
        var pipeline = GraphResiliencePipeline.Create(_logger);
        var attemptCount = 0;

        await pipeline.ExecuteAsync(async _ =>
        {
            attemptCount++;
            await Task.CompletedTask;
            if (attemptCount < 2)
                throw new ODataError { ResponseStatusCode = 503 };
        }, CancellationToken.None);

        attemptCount.ShouldBe(2);
    }

    [Fact]
    public async Task Pipeline_RetriesOnHttpRequestException()
    {
        var pipeline = GraphResiliencePipeline.Create(_logger);
        var attemptCount = 0;

        await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await pipeline.ExecuteAsync(async _ =>
            {
                attemptCount++;
                await Task.CompletedTask;
                throw new HttpRequestException("transient network failure");
            }, CancellationToken.None);
        });

        attemptCount.ShouldBe(4);
    }

    [Fact]
    public async Task Pipeline_DoesNotRetryOn4xxOtherThan429()
    {
        var pipeline = GraphResiliencePipeline.Create(_logger);
        var attemptCount = 0;

        await Should.ThrowAsync<ODataError>(async () =>
        {
            await pipeline.ExecuteAsync(async _ =>
            {
                attemptCount++;
                await Task.CompletedTask;
                throw new ODataError { ResponseStatusCode = 404 };
            }, CancellationToken.None);
        });

        attemptCount.ShouldBe(1);
    }

    [Fact]
    public async Task Pipeline_DoesNotRetryOnUnrelatedException()
    {
        var pipeline = GraphResiliencePipeline.Create(_logger);
        var attemptCount = 0;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await pipeline.ExecuteAsync(async _ =>
            {
                attemptCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException("not transient");
            }, CancellationToken.None);
        });

        attemptCount.ShouldBe(1);
    }

    [Fact]
    public async Task Pipeline_LogsWarningOnEachRetry()
    {
        var pipeline = GraphResiliencePipeline.Create(_logger);
        var attemptCount = 0;

        await pipeline.ExecuteAsync(async _ =>
        {
            attemptCount++;
            await Task.CompletedTask;
            if (attemptCount < 3)
                throw new ODataError { ResponseStatusCode = 429 };
        }, CancellationToken.None);

        _logger.Received(2).Warning(Arg.Is<string>(s => s.Contains("Graph API retry")));
    }
}
