using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using FEx.Downloader.Enums;
using NSubstitute;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Downloader.Tests;

public sealed class DownloadRangeTests : IDisposable
{
    private static readonly Uri Url = new("http://stub/file.bin");
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly byte[] _data = [.. Enumerable.Range(0, 100).Select(x => (byte)x)];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public DownloadRangeTests()
    {
        Directory.CreateDirectory(_dir);

        // No UI main thread exists in a test host; run property-change notifications inline.
        var dispatcher = Substitute.For<IFExDispatcher>();
        dispatcher.When(d => d.InvokeOnMainThread(Arg.Any<Action>(), Arg.Any<object?>()))
            .Do(call => call.Arg<Action>()());
        FExCoreStatics.Configure(dispatcherFactory: () => dispatcher);
    }

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public async Task DoDownloadAsync_RequestsTheRangeAndAssemblesTheBytes()
    {
        var handler = new FakeHttpHandler(Partial);
        using var client = new HttpClient(handler);
        using var range = NewRange(client, CancellationToken.None);

        await range.DoDownloadAsync(0);

        range.DState.ShouldBe(DownloadState.Finished);
        handler.Requests.Select(x => x.Headers.Range!.ToString()).ShouldBe(["bytes=0-99"]);

        var assembled = range.Chunks.Values.OrderBy(x => x.From).SelectMany(x => File.ReadAllBytes(x.File.FullName));
        assembled.ShouldBe(_data);
    }

    [Fact]
    public async Task DoDownloadAsync_ServerIgnoringTheRange_FailsInsteadOfStoringTheWholeBody()
    {
        using var client = new HttpClient(new FakeHttpHandler(_ => new(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(_data)
        }));

        using var range = NewRange(client, CancellationToken.None);

        await range.DoDownloadAsync(0);

        range.DState.ShouldBe(DownloadState.Failed);
        range.Chunks.Values.ShouldAllBe(x => x.Size == 0);
    }

    [Fact(Timeout = 10_000)]
    public async Task DoDownloadAsync_NonSuccessStatus_FailsWithoutWaitingForConnectivity()
    {
        var handler = new FakeHttpHandler(_ => new(HttpStatusCode.InternalServerError));
        using var client = new HttpClient(handler);
        using var range = NewRange(client, TestContext.Current.CancellationToken);

        await range.DoDownloadAsync(0);

        range.DState.ShouldBe(DownloadState.Failed);
        range.Chunks.Values.ShouldAllBe(x => x.Size == 0);
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact(Timeout = 10_000)]
    public async Task DoDownloadAsync_CancelledWhileConnecting_StopsTheDownload()
    {
        using var cts = new CancellationTokenSource();
        var requestSeen = new TaskCompletionSource();

        using var client = new HttpClient(new FakeHttpHandler(async (_, token) =>
        {
            requestSeen.SetResult();
            await Task.Delay(Timeout.Infinite, token);

            return new(HttpStatusCode.OK);
        }));

        using var range = NewRange(client, cts.Token);

        var download = range.DoDownloadAsync(0);
        await requestSeen.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cts.CancelAsync();
        await download;

        range.DState.ShouldBe(DownloadState.Cancelled);
    }

    private DownloadRange NewRange(HttpClient client, CancellationToken token) =>
        new(0, 99, new(_dir), Url, null, 40, Path.Combine(_dir, "file.bin"), 100, null, null, token, client);

    private HttpResponseMessage Partial(HttpRequestMessage request)
    {
        var range = request.Headers.Range!.Ranges.Single();
        var from = (int)range.From!.Value;
        var to = (int)range.To!.Value;
        var content = new ByteArrayContent(_data[from..(to + 1)]);
        content.Headers.ContentRange = new(from, to, _data.Length);

        return new(HttpStatusCode.PartialContent)
        {
            Content = content
        };
    }
}
