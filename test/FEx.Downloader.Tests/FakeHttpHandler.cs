using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Downloader.Tests;

/// <summary>Fakes the network: answers every request through <paramref name="respond" /> and records it.</summary>
internal sealed class FakeHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    public ConcurrentQueue<HttpRequestMessage> Requests { get; } = [];

    public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : this((request, _) => Task.FromResult(respond(request)))
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);

        return respond(request, cancellationToken);
    }
}
