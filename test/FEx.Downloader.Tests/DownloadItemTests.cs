using FEx.Downloader.Enums;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Downloader.Tests;

public sealed class DownloadItemTests : IDisposable
{
    private static readonly Uri Url = new("http://stub/file.bin");
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public DownloadItemTests()
    {
        Directory.CreateDirectory(_dir);
        TestHost.ConfigureInlineDispatcher();
    }

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public async Task DownloadFileAsync_SmallFile_WritesTheBody()
    {
        var server = new FakeServer(Bytes(1000)) { AcceptRanges = false };
        using var client = server.CreateClient();
        using var item = await NewItemAsync(client, 1000, TestContext.Current.CancellationToken);

        (await item.DownloadFileAsync()).ShouldBeTrue();

        (await ReadOutputAsync()).ShouldBe(server.Data);
    }

    [Fact]
    public async Task DownloadFileAsync_ServerWithoutRanges_StreamsTheWholeFile()
    {
        var server = new FakeServer(Bytes(DownloadItem.BufferLength + 5)) { AcceptRanges = false };
        using var client = server.CreateClient();
        using var item = await NewItemAsync(client, server.Data.Length, TestContext.Current.CancellationToken);

        (await item.DownloadFileAsync()).ShouldBeTrue();

        (await ReadOutputAsync()).ShouldBe(server.Data);
        server.Handler.Requests.ShouldAllBe(x => x.Headers.Range == null);
    }

    [Fact]
    public async Task DownloadFileAsync_ServerWithRanges_DownloadsInParallelAndAssemblesTheFile()
    {
        var server = new FakeServer(Bytes(3 * DownloadItem.BufferLength + 123));
        using var client = server.CreateClient();
        using var item = await NewItemAsync(client, server.Data.Length, TestContext.Current.CancellationToken);

        (await item.DownloadFileAsync()).ShouldBeTrue();

        item.IsSpeededUp.ShouldBeTrue();
        (await ReadOutputAsync()).ShouldBe(server.Data);
    }

    [Fact]
    public async Task DownloadFileAsync_ManyRanges_NeverExceedsTheParallelismCap()
    {
        var server = new FakeServer(Bytes(32 * DownloadItem.BufferLength)) { Latency = TimeSpan.FromMilliseconds(100) };
        using var client = server.CreateClient();
        using var item = await NewItemAsync(client, server.Data.Length, TestContext.Current.CancellationToken);

        (await item.DownloadFileAsync()).ShouldBeTrue();

        item.ParallelRanges.ShouldBeLessThanOrEqualTo(DownloadItem.MaxParallelRanges);
        server.PeakInFlight.ShouldBeLessThanOrEqualTo(DownloadItem.MaxParallelRanges);
        server.PeakInFlight.ShouldBeGreaterThan(1);
    }

    [Fact(Timeout = 30_000)]
    public async Task DownloadFileAsync_RangesAnswered403_FailsAfterOneRoundWithoutRetrying()
    {
        var server = new FakeServer(Bytes(4 * DownloadItem.BufferLength))
        {
            Override = request => FakeServer.IsRangeChunk(request)
                ? new(HttpStatusCode.Forbidden)
                : null
        };

        using var client = server.CreateClient();
        using var item = await NewItemAsync(client, server.Data.Length, TestContext.Current.CancellationToken);

        (await item.DownloadFileAsync()).ShouldBeFalse();

        item.DState.ShouldBe(DownloadState.Failed);
        // one request per range (4 ranges of 1 MB), none repeated
        server.Handler.Requests.Count(FakeServer.IsRangeChunk).ShouldBe(4);
    }

    [Fact(Timeout = 30_000)]
    public async Task DownloadFileAsync_RangesAnswered503_RetriesWithBackoffThenFails()
    {
        var server = new FakeServer(Bytes(4 * DownloadItem.BufferLength))
        {
            Override = request => FakeServer.IsRangeChunk(request)
                ? new(HttpStatusCode.ServiceUnavailable)
                : null
        };

        using var client = server.CreateClient();
        using var item = await NewItemAsync(client, server.Data.Length, TestContext.Current.CancellationToken);
        item.MaxRetries = 2;

        (await item.DownloadFileAsync()).ShouldBeFalse();

        item.DState.ShouldBe(DownloadState.Failed);
        // the first attempt plus two retries, 4 ranges each
        server.Handler.Requests.Count(FakeServer.IsRangeChunk).ShouldBe(3 * 4);
    }

    [Fact(Timeout = 30_000)]
    public async Task DownloadFileAsync_RangesAnswered503WithRetryAfter_WaitsAsAsked()
    {
        var failures = 4;
        var server = new FakeServer(Bytes(4 * DownloadItem.BufferLength));
        var serverWithOverride = new FakeServer(server.Data)
        {
            Override = request =>
            {
                if (!FakeServer.IsRangeChunk(request)
                    || Interlocked.Decrement(ref failures) < 0)
                    return null;

                var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                response.Headers.RetryAfter = new(TimeSpan.FromSeconds(1));

                return response;
            }
        };

        using var client = serverWithOverride.CreateClient();
        using var item = await NewItemAsync(client, server.Data.Length, TestContext.Current.CancellationToken);
        var watch = Stopwatch.StartNew();

        (await item.DownloadFileAsync()).ShouldBeTrue();

        watch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(900));
        (await ReadOutputAsync()).ShouldBe(server.Data);
    }

    [Fact(Timeout = 30_000)]
    public async Task DownloadFileAsync_RateLimitedFirstRequest_RetriesAndSucceeds()
    {
        var calls = 0;
        var server = new FakeServer(Bytes(500))
        {
            AcceptRanges = false,
            Override = _ => Interlocked.Increment(ref calls) == 1
                ? new((HttpStatusCode)429)
                : null
        };

        using var client = server.CreateClient();
        using var item = await NewItemAsync(client, 500, TestContext.Current.CancellationToken);

        (await item.DownloadFileAsync()).ShouldBeTrue();

        server.RequestCount.ShouldBe(2);
        (await ReadOutputAsync()).ShouldBe(server.Data);
    }

    [Fact(Timeout = 30_000)]
    public async Task DownloadFileAsync_NotFound_FailsWithoutRetrying()
    {
        var server = new FakeServer(Bytes(500)) { Override = _ => new(HttpStatusCode.NotFound) };
        using var client = server.CreateClient();
        using var item = await NewItemAsync(client, 500, TestContext.Current.CancellationToken);

        (await item.DownloadFileAsync()).ShouldBeFalse();

        item.DState.ShouldBe(DownloadState.Failed);
        server.RequestCount.ShouldBe(1);
    }

    private static byte[] Bytes(long length) => [.. Enumerable.Range(0, (int)length).Select(x => (byte)(x % 251))];

    private Task<byte[]> ReadOutputAsync() =>
        File.ReadAllBytesAsync(Path.Combine(_dir, "out.bin"), TestContext.Current.CancellationToken);

    private async Task<DownloadItem> NewItemAsync(HttpClient client, long dataLength, CancellationToken token)
    {
        var item = await DownloadItem.CreateAsync(Url,
            Path.Combine(_dir, "out.bin"),
            false,
            null,
            4,
            dataLength,
            null,
            token);

        item.HttpClient = client;
        item.RetryBaseDelay = TimeSpan.FromMilliseconds(1);
        item.SetTemporaryCacheDirectory(Path.Combine(_dir, "cache"));

        return item;
    }
}
