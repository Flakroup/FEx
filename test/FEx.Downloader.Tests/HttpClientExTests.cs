using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using FEx.Downloader.Clients;
using FEx.Downloader.Enums;
using NSubstitute;
using Shouldly;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Downloader.Tests;

public sealed class HttpClientExTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public HttpClientExTests()
    {
        Directory.CreateDirectory(_dir);
        TestHost.ConfigureInlineDispatcher();
    }

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public async Task DoDownloadAsync_WithoutContentLength_WritesBody()
    {
        using var client = new HttpClientEx(new StubHandler(_ => Chunked("hello chunked")));
        var path = Path.Combine(_dir, "chunked.txt");

        using var response = await client.GetAsync(new Uri("http://stub/f"), HttpCompletionOption.ResponseHeadersRead, Ct);
        response.Content.Headers.ContentLength.ShouldBeNull();

        await client.DoDownloadAsync(path, response, false);

        (await File.ReadAllTextAsync(path, Ct)).ShouldBe("hello chunked");
    }

    [Fact]
    public async Task DoDownloadAsync_WithoutContentLength_TruncatesLongerExistingFile()
    {
        using var client = new HttpClientEx(new StubHandler(_ => Chunked("new")));
        var path = Path.Combine(_dir, "existing.txt");
        await File.WriteAllTextAsync(path, "a much longer previous content", Ct);

        using var response = await client.GetAsync(new Uri("http://stub/f"), HttpCompletionOption.ResponseHeadersRead, Ct);
        await client.DoDownloadAsync(path, response, false);

        (await File.ReadAllTextAsync(path, Ct)).ShouldBe("new");
    }

    [Fact]
    public async Task DownloadFileAsync_On429_StopsAfterMaxRetryAttempts()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage((HttpStatusCode)429));
        using var client = new HttpClientEx(handler)
        {
            MaxRetryAttempts = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1),
        };

        await Should.ThrowAsync<HttpRequestException>(() =>
            client.DownloadFileAsync(new Uri("http://stub/f"), Path.Combine(_dir, "x.bin"), false));

        handler.Calls.ShouldBe(4); // initial attempt + 3 retries
    }

    [Fact]
    public async Task DownloadFileAsync_On429ThenSuccess_Downloads()
    {
        var handler = new StubHandler(n => n < 2
            ? new HttpResponseMessage((HttpStatusCode)429)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });
        using var client = new HttpClientEx(handler) { RetryBaseDelay = TimeSpan.FromMilliseconds(1) };
        var path = Path.Combine(_dir, "ok.txt");

        await client.DownloadFileAsync(new Uri("http://stub/f"), path, false);

        (await File.ReadAllTextAsync(path, Ct)).ShouldBe("ok");
        handler.Calls.ShouldBe(3);
    }

    [Fact]
    public void ComputeRetryDelay_GrowsExponentiallyAndIsCapped()
    {
        var baseDelay = TimeSpan.FromMilliseconds(100);

        HttpClientEx.ComputeRetryDelay(baseDelay, 0).ShouldBe(TimeSpan.FromMilliseconds(100));
        HttpClientEx.ComputeRetryDelay(baseDelay, 1).ShouldBe(TimeSpan.FromMilliseconds(200));
        HttpClientEx.ComputeRetryDelay(baseDelay, 2).ShouldBe(TimeSpan.FromMilliseconds(400));
        HttpClientEx.ComputeRetryDelay(baseDelay, 50).ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void ComputeRetryDelay_WithZeroBaseDelay_NeverOverflowsToNaN() =>
        HttpClientEx.ComputeRetryDelay(TimeSpan.Zero, 1024).ShouldBe(TimeSpan.Zero);

    [Fact]
    public async Task DoDownloadAsync_WhenStreamFailsMidway_KeepsExistingFileAndLeavesNoTempFile()
    {
        using var client = new HttpClientEx(new StubHandler(_ => Respond(new FailingStream(Encoding.UTF8.GetBytes("NEW-PARTIAL")))));
        var path = Path.Combine(_dir, "cached.bin");
        await File.WriteAllTextAsync(path, "OLD-GOOD-CACHED-IMAGE-CONTENT", Ct);

        using var response = await client.GetAsync(new Uri("http://stub/f"), HttpCompletionOption.ResponseHeadersRead, Ct);
        await Should.ThrowAsync<IOException>(() => client.DoDownloadAsync(path, response, false));

        (await File.ReadAllTextAsync(path, Ct)).ShouldBe("OLD-GOOD-CACHED-IMAGE-CONTENT");
        Directory.GetFiles(_dir).ShouldBe([path]);
    }

    [Fact]
    public async Task DoDownloadAsync_WithZeroContentLength_ReplacesExistingFileWithEmptyOne()
    {
        using var client = new HttpClientEx(new StubHandler(_ => Respond(new MemoryStream([]))));
        var path = Path.Combine(_dir, "empty.bin");
        await File.WriteAllTextAsync(path, "previous", Ct);

        using var response = await client.GetAsync(new Uri("http://stub/f"), HttpCompletionOption.ResponseHeadersRead, Ct);
        response.Content.Headers.ContentLength.ShouldBe(0);

        await client.DoDownloadAsync(path, response, false);

        new FileInfo(path).Length.ShouldBe(0);
    }

    [Fact]
    public async Task DoDownloadAsync_WithoutContentLength_StopsAtMaxDownloadBytes()
    {
        using var client = new HttpClientEx(new StubHandler(_ => Chunked(new string('x', 100)))) { MaxDownloadBytes = 10 };
        var path = Path.Combine(_dir, "huge.bin");

        using var response = await client.GetAsync(new Uri("http://stub/f"), HttpCompletionOption.ResponseHeadersRead, Ct);
        await Should.ThrowAsync<IOException>(() => client.DoDownloadAsync(path, response, false));

        Directory.GetFiles(_dir).ShouldBeEmpty();
    }

    [Fact]
    public async Task DownloadFileAsync_On429_HonoursCancellationDuringBackoff()
    {
        using var cts = new CancellationTokenSource();
        var handler = new StubHandler(_ =>
        {
            cts.Cancel();

            return new HttpResponseMessage((HttpStatusCode)429);
        });
        using var client = new HttpClientEx(handler, true, cts) { RetryBaseDelay = TimeSpan.FromMinutes(5) };

        await Should.ThrowAsync<OperationCanceledException>(() =>
            client.DownloadFileAsync(new Uri("http://stub/f"), Path.Combine(_dir, "c.bin"), false));

        handler.Calls.ShouldBe(1);
        client.DState.ShouldBe(DownloadState.Failed);
    }

    private static HttpResponseMessage Chunked(string body) =>
        Respond(new NonSeekableStream(Encoding.UTF8.GetBytes(body)));

    // A non-seekable stream makes StreamContent omit Content-Length (chunked); MemoryStream keeps it.
#pragma warning disable IDISP004 // ownership passes to the returned message
    private static HttpResponseMessage Respond(Stream body) =>
        new(HttpStatusCode.OK) { Content = new StreamContent(body) };
#pragma warning restore IDISP004

    private sealed class StubHandler(Func<int, HttpResponseMessage> respond) : HttpClientHandler
    {
        private int _calls;

        public int Calls => _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(respond(Interlocked.Increment(ref _calls) - 1));
    }

    private sealed class FailingStream : MemoryStream
    {
        private readonly int _size;

        public FailingStream(byte[] data)
            : base(data, false) => _size = data.Length;

        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Position < _size ? base.ReadAsync(buffer, cancellationToken) : throw new IOException("connection reset");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Position < _size
                ? base.ReadAsync(buffer, offset, count, cancellationToken)
                : throw new IOException("connection reset");
    }

    private sealed class NonSeekableStream(byte[] data) : MemoryStream(data, false)
    {
        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();
    }
}
