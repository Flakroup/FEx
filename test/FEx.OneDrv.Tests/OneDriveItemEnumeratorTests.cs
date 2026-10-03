using FEx.Agnostics.Abstractions.Interfaces;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.OneDrv.Tests;

// IDISP001: the fake handler lives for the duration of one test.
#pragma warning disable IDISP001

public sealed class OneDriveItemEnumeratorTests
{
    private const string Next = "https://graph.microsoft.com/v1.0/drives/drive-1/items/root/children?$skiptoken=2";

    private static Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> TwoPages() =>
        request => request.RequestUri!.Query.Contains("skiptoken")
            ? FakeGraph.Json(FakeGraph.Page([FakeGraph.File("c")]))
            : FakeGraph.Json(FakeGraph.Page([FakeGraph.File("a"), FakeGraph.Folder("dir"), FakeGraph.File("b")], Next));

    private static OneDriveItemEnumerator Create(FakeGraph graph) =>
        new(graph.CreateCache(), Substitute.For<IFExLogger>());

    [Fact]
    public async Task EnumerateFilesAsync_YieldsFilesFromAllPages_SkippingFolders()
    {
        var graph = new FakeGraph(request => TwoPages()(request));

        var ids = new List<string>();

        await foreach (var file in Create(graph).EnumerateFilesAsync("root", TestContext.Current.CancellationToken))
            ids.Add(file.Id);

        ids.ShouldBe(["a", "b", "c"]);
        graph.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task EnumerateFilesAsync_IsLazy_DoesNotFetchNextPageBeforeFirstPageIsConsumed()
    {
        var graph = new FakeGraph(request => TwoPages()(request));

        await using var enumerator = Create(graph).EnumerateFilesAsync("root", TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        enumerator.Current.Id.ShouldBe("a");
        graph.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task EnumerateFilesAsync_CancelledBetweenItemsOfOnePage_StopsBeforeNextItem()
    {
        var graph = new FakeGraph(_ => FakeGraph.Json(FakeGraph.Page([FakeGraph.File("a"), FakeGraph.File("b")])));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var seen = new List<string>();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var file in Create(graph).EnumerateFilesAsync("root", cts.Token))
            {
                seen.Add(file.Id);
                await cts.CancelAsync();
            }
        });

        seen.ShouldBe(["a"]);
    }

    [Fact]
    public async Task EnumerateFilesAsync_TransientFailureOnSecondPage_RetriesThatPageWithoutDuplicates()
    {
        var failed = false;

        var graph = new FakeGraph(request =>
        {
            if (!request.RequestUri!.Query.Contains("skiptoken"))
                return FakeGraph.Json(FakeGraph.Page([FakeGraph.File("a")], Next));

            if (failed)
                return FakeGraph.Json(FakeGraph.Page([FakeGraph.File("b")]));

            failed = true;

            return FakeGraph.Json("""{"error":{"code":"serviceNotAvailable","message":"busy"}}""",
                HttpStatusCode.ServiceUnavailable);
        });

        var ids = await Create(graph).EnumerateFilesAsync("root", TestContext.Current.CancellationToken)
            .Select(f => f.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        ids.ShouldBe(["a", "b"]);
    }
}
