using FEx.Core.Abstractions.Interfaces;
using FEx.Webx;
using NSubstitute;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Webx.Tests;

public sealed class HasInternetConnectionGateTests
{
    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                                     CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);

            return new(HttpStatusCode.NoContent);
        }
    }

    private sealed class NoContentHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
    }

    [Fact]
    public async Task CheckAsync_ReachableUrl_ReturnsTrue()
    {
        using var client = new HttpClient(new NoContentHandler());
        var gate = new HasInternetConnectionGate(Substitute.For<IExceptionHandler>(), client);

        (await gate.CheckAsync(CancellationToken.None)).ShouldBeTrue();
    }

    [Fact]
    public async Task CheckAsync_CancelledToken_ReturnsFalsePromptlyOnBlackHole()
    {
        using var client = new HttpClient(new HangingHandler());
        var gate = new HasInternetConnectionGate(Substitute.For<IExceptionHandler>(), client);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var check = gate.CheckAsync(new Uri("http://blackhole.invalid/"), cts.Token);

        (await check.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task CheckAsync_NoTokenButTimeoutElapsed_ReturnsFalsePromptlyOnBlackHole()
    {
        using var client = new HttpClient(new HangingHandler());
        var gate = new HasInternetConnectionGate(Substitute.For<IExceptionHandler>(), client)
        {
            Timeout = TimeSpan.FromMilliseconds(100)
        };

        (await gate.CheckAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
