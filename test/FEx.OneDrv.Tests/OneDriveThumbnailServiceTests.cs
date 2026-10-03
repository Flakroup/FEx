using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace FEx.OneDrv.Tests;

// IDISP001/IDISP004: the HttpClient and handlers live for the duration of one test.
#pragma warning disable IDISP001, IDISP004, IDISP006

public sealed class OneDriveThumbnailServiceTests : IDisposable
{
    private const string ThumbsJson =
        """{"value":[{"id":"0","small":{"url":"https://t/small"},"medium":{"url":"https://t/medium"},"large":{"url":"https://t/large"}}]}""";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "FEx.OneDrv.Tests", Guid.NewGuid().ToString("N"));
    private readonly FakeGraph _graph = new(_ => FakeGraph.Json(ThumbsJson));
    private readonly FakeGraph _cdn = new(_ =>
    {
        var response = new HttpResponseMessage
        {
            Content = new ByteArrayContent([1, 2, 3])
        };

        response.Content.Headers.ContentType = new("image/png");

        return response;
    });

    private OneDriveThumbnailService Create() =>
        new(_graph.CreateCache(),
            new()
            {
                ClientId = "x",
                TokenCachePath = Path.Combine(_dir, "token.bin")
            },
            Substitute.For<IFExLogger>(),
            new(_cdn));

    private string[] CachedFiles() => Directory.GetFiles(Path.Combine(_dir, "thumbnails"));

    [Fact]
    public async Task GetThumbnailAsync_RequestedSize_DownloadsThatSize()
    {
        await Create().GetThumbnailAsync("item", ThumbnailSize.Large, TestContext.Current.CancellationToken);

        _cdn.Requests.ShouldBe(["https://t/large"]);
    }

    [Fact]
    public async Task GetThumbnailAsync_WithoutSize_DefaultsToMedium()
    {
        await Create().GetThumbnailAsync("item", TestContext.Current.CancellationToken);

        _cdn.Requests.ShouldBe(["https://t/medium"]);
    }

    [Fact]
    public async Task GetThumbnailAsync_GraphIdWithIllegalChars_UsesSafeFileNameAndRealContentType()
    {
        await Create().GetThumbnailAsync("ab/c+d==", TestContext.Current.CancellationToken);

        var name = Path.GetFileName(CachedFiles().Single());
        name.ShouldBe(OneDriveThumbnailService.GetCacheStem("ab/c+d==", ThumbnailSize.Medium) + ".png");
        name.ShouldMatch("^[0-9a-f]{64}\\.medium\\.png$");
    }

    [Fact]
    public async Task GetThumbnailAsync_SecondCall_IsServedFromDiskCache()
    {
        var service = Create();

        var first = await service.GetThumbnailAsync("item", TestContext.Current.CancellationToken);
        var second = await service.GetThumbnailAsync("item", TestContext.Current.CancellationToken);

        second.ShouldBe(first);
        _cdn.Requests.Count.ShouldBe(1);
        _graph.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetThumbnailAsync_DifferentSizes_AreCachedSeparately()
    {
        var service = Create();

        await service.GetThumbnailAsync("item", ThumbnailSize.Small, TestContext.Current.CancellationToken);
        await service.GetThumbnailAsync("item", ThumbnailSize.Large, TestContext.Current.CancellationToken);

        CachedFiles().Length.ShouldBe(2);
    }

    [Fact]
    public async Task GetThumbnailAsync_WildcardItemId_NeverMatchesAnotherItemsCache()
    {
        var service = Create();
        await service.GetThumbnailAsync("other-item", TestContext.Current.CancellationToken);
        _graph.Requests.Count.ShouldBe(1);

        await service.GetThumbnailAsync("*", TestContext.Current.CancellationToken);

        _graph.Requests.Count.ShouldBe(2);
        CachedFiles().Length.ShouldBe(2);
    }

    [Fact]
    public async Task GetThumbnailAsync_EmptyCacheFile_IsTreatedAsMissAndRedownloaded()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "thumbnails"));
        var stale = Path.Combine(_dir, "thumbnails", OneDriveThumbnailService.GetCacheStem("item", ThumbnailSize.Medium) + ".png");
        await File.WriteAllBytesAsync(stale, [], TestContext.Current.CancellationToken);

        var bytes = await Create().GetThumbnailAsync("item", TestContext.Current.CancellationToken);

        bytes.ShouldBe([1, 2, 3]);
        (await File.ReadAllBytesAsync(stale, TestContext.Current.CancellationToken)).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task GetThumbnailAsync_LeftoverTempFile_IsNotServed()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "thumbnails"));
        var temp = Path.Combine(_dir, "thumbnails", OneDriveThumbnailService.GetCacheStem("item", ThumbnailSize.Medium) + ".png.tmp");
        await File.WriteAllBytesAsync(temp, [9, 9], TestContext.Current.CancellationToken);

        var bytes = await Create().GetThumbnailAsync("item", TestContext.Current.CancellationToken);

        bytes.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task GetThumbnailAsync_AfterDownload_LeavesNoTempFile()
    {
        await Create().GetThumbnailAsync("item", TestContext.Current.CancellationToken);

        CachedFiles().ShouldAllBe(f => !f.EndsWith(".tmp"));
    }

    [Fact]
    public async Task GetThumbnailAsync_CacheWriteFails_StillReturnsBytesAndLeavesNoTempFile()
    {
        // A directory at the final cache path makes the move fail on every OS.
        var blocked = Path.Combine(_dir, "thumbnails", OneDriveThumbnailService.GetCacheStem("item", ThumbnailSize.Medium) + ".png");
        Directory.CreateDirectory(blocked);

        var bytes = await Create().GetThumbnailAsync("item", TestContext.Current.CancellationToken);

        bytes.ShouldBe([1, 2, 3]);
        Directory.GetFiles(Path.Combine(_dir, "thumbnails")).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetThumbnailAsync_ConcurrentCallsForSameItem_BothSucceed()
    {
        var service = Create();

        var results = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => service.GetThumbnailAsync("item", TestContext.Current.CancellationToken)));

        results.ShouldAllBe(r => r != null && r.Length == 3);
        CachedFiles().ShouldAllBe(f => !f.EndsWith(".tmp"));
    }

    [Fact]
    public async Task GetThumbnailAsync_SizeMissingInGraphResponse_ReturnsNullWithoutDownload()
    {
        var graph = new FakeGraph(_ => FakeGraph.Json("""{"value":[{"id":"0","medium":{"url":"https://t/medium"}}]}"""));
        var service = new OneDriveThumbnailService(graph.CreateCache(),
            new()
            {
                ClientId = "x",
                TokenCachePath = Path.Combine(_dir, "token.bin")
            },
            Substitute.For<IFExLogger>(),
            new(_cdn));

        var bytes = await service.GetThumbnailAsync("item", ThumbnailSize.Small, TestContext.Current.CancellationToken);

        bytes.ShouldBeNull();
        _cdn.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("image/jpeg", "jpg")]
    [InlineData("image/JPG", "jpg")]
    [InlineData("image/png", "png")]
    [InlineData("image/gif", "gif")]
    [InlineData("image/webp", "webp")]
    [InlineData("image/bmp", "bmp")]
    [InlineData("application/octet-stream", "bin")]
    [InlineData(null, "bin")]
    public void GetExtension_MapsContentType(string? mediaType, string expected) =>
        OneDriveThumbnailService.GetExtension(mediaType).ShouldBe(expected);

    [Fact]
    public void GetDefaultCacheDir_IsPerUser_NotTheSharedTempDirectory()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Assert.SkipWhen(string.IsNullOrEmpty(local), "No per-user LocalApplicationData on this host.");

        OneDriveThumbnailService.GetDefaultCacheDir().ShouldStartWith(local);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }
}
