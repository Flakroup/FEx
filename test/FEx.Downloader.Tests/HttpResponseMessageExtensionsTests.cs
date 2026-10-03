using FEx.Agnostics.Abstractions.Models;
using FEx.Downloader.Extensions;
using Shouldly;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Downloader.Tests;

public sealed class HttpResponseMessageExtensionsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public HttpResponseMessageExtensionsTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public async Task DownloadToFileAsync_WritesBodyAndReportsProgress()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3, 4])
        };

        var path = Path.Combine(_dir, "sub", "ok.bin");
        long lastReceived = 0;
        long? lastTotal = null;

        await response.DownloadToFileAsync(path,
            (received, total) =>
            {
                lastReceived = received;
                lastTotal = total;
            },
            Ct);

        (await File.ReadAllBytesAsync(path, Ct)).ShouldBe([1, 2, 3, 4]);
        lastReceived.ShouldBe(4);
        lastTotal.ShouldBe(4);
    }

    [Fact]
    public async Task DownloadToFileAsync_NonSuccessStatus_ThrowsAndLeavesNoFile()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("<html>not found</html>")
        };

        var path = Path.Combine(_dir, "missing.bin");

        var ex = await Should.ThrowAsync<HttpStatusException>(() => response.DownloadToFileAsync(path, null, Ct));

        ex.ResponseStatusCode.ShouldBe(HttpStatusCode.NotFound);
        File.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public async Task DownloadToFileAsync_TruncatedBody_ThrowsAndKeepsTheExistingFile()
    {
        var path = Path.Combine(_dir, "keep.bin");
        await File.WriteAllBytesAsync(path, [9, 9], Ct);

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };

        response.Content.Headers.ContentLength = 10;

        await Should.ThrowAsync<IOException>(() => response.DownloadToFileAsync(path, null, Ct));

        (await File.ReadAllBytesAsync(path, Ct)).ShouldBe([9, 9]);
        Directory.GetFiles(_dir).ShouldBe([path]);
    }

    [Fact]
    public async Task DownloadToFileAsync_Cancelled_StopsAndLeavesNoFile()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };

        var path = Path.Combine(_dir, "cancelled.bin");

        await Should.ThrowAsync<OperationCanceledException>(() => response.DownloadToFileAsync(path, null, cts.Token));

        Directory.GetFiles(_dir).ShouldBeEmpty();
    }
}
