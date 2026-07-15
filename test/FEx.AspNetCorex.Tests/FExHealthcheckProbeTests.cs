using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AspNetCorex.Tests;

/// <summary>The container HEALTHCHECK probe: maps the endpoint's outcome to an exit code.</summary>
public sealed class FExHealthcheckProbeTests
{
    [Theory]
    [InlineData("--healthcheck", true)]
    [InlineData("", false)]
    public void IsProbe_MatchesOnlyTheSingleHealthcheckArgument(string arg, bool expected) =>
        FExHealthcheckProbe.IsProbe(arg.Length == 0
                ? []
                : [arg])
            .ShouldBe(expected);

    [Fact]
    public async Task SuccessfulResponse_ReturnsZero()
    {
        using StubHandler handler = new(HttpStatusCode.OK);

        var code = await FExHealthcheckProbe.ProbeAsync(new("http://localhost/health"),
            TimeSpan.FromSeconds(3),
            handler);

        code.ShouldBe(0);
    }

    [Fact]
    public async Task NonSuccessResponse_ReturnsOne()
    {
        using StubHandler handler = new(HttpStatusCode.ServiceUnavailable);

        var code = await FExHealthcheckProbe.ProbeAsync(new("http://localhost/health"),
            TimeSpan.FromSeconds(3),
            handler);

        code.ShouldBe(1);
    }

    [Fact]
    public async Task ConnectionFailure_ReturnsOne()
    {
        using ThrowingHandler handler = new();

        var code = await FExHealthcheckProbe.ProbeAsync(new("http://localhost/health"),
            TimeSpan.FromSeconds(3),
            handler);

        code.ShouldBe(1);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;

        public StubHandler(HttpStatusCode status)
        {
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_status));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken) =>
            throw new HttpRequestException("connection refused");
    }
}