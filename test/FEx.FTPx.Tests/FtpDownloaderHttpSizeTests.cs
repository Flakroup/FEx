using FEx.Agnostics.Abstractions.Enums;
using FEx.FTPx;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.FTPx.Tests;

/// <summary>The HTTP branch of <see cref="FtpDownloader.CalculateSizeAsync" />, served by a fake handler (no network).</summary>
public sealed class FtpDownloaderHttpSizeTests
{
    private static readonly Uri Url = new("http://stub/file.bin");

    [Fact]
    public async Task CalculateSizeAsync_Http_ReturnsTheContentLength()
    {
        using var client = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[2048]) }));

        var size = await FtpDownloader.CalculateSizeAsync(Url,
            false,
            LengthType.Bytes,
            client: client,
            cancellationToken: TestContext.Current.CancellationToken);

        size.ShouldBe(2048);
    }

    [Fact]
    public async Task CalculateSizeAsync_Http_WithoutContentLength_ReportsZeroNotNegative()
    {
        // chunked: a response without a Content-Length header
        using var client = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new UnknownLengthContent() }));

        var size = await FtpDownloader.CalculateSizeAsync(Url,
            false,
            LengthType.Bytes,
            client: client,
            cancellationToken: TestContext.Current.CancellationToken);

        size.ShouldBe(0);
    }

    private sealed class UnknownLengthContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(System.IO.Stream stream, TransportContext? context) => Task.CompletedTask;

        protected override bool TryComputeLength(out long length)
        {
            length = -1;

            return false;
        }
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
