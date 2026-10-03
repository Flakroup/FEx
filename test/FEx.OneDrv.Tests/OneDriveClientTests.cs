using FEx.Agnostics.Abstractions.Interfaces;
using NSubstitute;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.OneDrv.Tests;

// IDISP001: the fake handler lives for the duration of one test.
#pragma warning disable IDISP001

public sealed class OneDriveClientTests
{
    private const string Next = "https://graph.microsoft.com/v1.0/drives/drive-1/items/root/children?$skiptoken=2";

    private static FakeGraph TwoPages() =>
        new(request => request.RequestUri!.Query.Contains("skiptoken")
            ? FakeGraph.Json(FakeGraph.Page([FakeGraph.File("c"), FakeGraph.Folder("d2")]))
            : FakeGraph.Json(FakeGraph.Page([FakeGraph.File("a"), FakeGraph.Folder("d1")], Next)));

    [Fact]
    public async Task ListFilesAsync_FollowsPagesAndReturnsOnlyFiles()
    {
        var client = new OneDriveClient(TwoPages().CreateCache(), Substitute.For<IFExLogger>());

        var files = await client.ListFilesAsync("root", TestContext.Current.CancellationToken);

        files.Count.ShouldBe(2);
        files[0].Id.ShouldBe("a");
        files[1].Id.ShouldBe("c");
    }

    [Fact]
    public async Task ListFoldersAsync_FollowsPagesAndReturnsOnlyFolders()
    {
        var client = new OneDriveClient(TwoPages().CreateCache(), Substitute.For<IFExLogger>());

        var folders = await client.ListFoldersAsync("root", TestContext.Current.CancellationToken);

        folders.Count.ShouldBe(2);
        folders[0].Id.ShouldBe("d1");
        folders[1].Id.ShouldBe("d2");
    }

    [Fact]
    public async Task GetFileAsync_OnSuccess_LogsLikeItsSiblings()
    {
        var logger = Substitute.For<IFExLogger>();
        var graph = new FakeGraph(_ => FakeGraph.Json(FakeGraph.File("a")));
        var client = new OneDriveClient(graph.CreateCache(), logger);

        var file = await client.GetFileAsync("a", TestContext.Current.CancellationToken);

        file.Id.ShouldBe("a");
        logger.Received(1).Information(Arg.Is<string>(s => s.Contains("Retrieved file a")));
    }
}
