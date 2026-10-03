using FEx.Agnostics.Abstractions.Interfaces;
using FEx.OneDrv.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
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
    private readonly FakeGraph _cdn = new(request =>
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
        name.ShouldBe("ab_c+d==.medium.png");
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

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }
}
