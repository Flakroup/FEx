using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FEx.EFCore.Tests;

public sealed class CacheFlushesTests
{
    [Fact]
    public void Track_ReturnsTheSameInstance()
    {
        var dictionary = new StubCacheTasks(new());

        new CacheFlushes().Track(dictionary).ShouldBeSameAs(dictionary);
    }

    [Fact]
    public async Task FlushAsync_WaitsForEveryTrackedDictionary_InOrder()
    {
        var order = new List<string>();
        var sut = new CacheFlushes();
        sut.Track(new StubCacheTasks(order, "first"));
        sut.Track(new StubCacheTasks(order, "second"));

        await sut.FlushAsync();

        order.ShouldBe(["first", "second"]);
    }

    [Fact]
    public async Task FlushAsync_LetsAFailedWaitFailTheTest()
    {
        var sut = new CacheFlushes();
        sut.Track(new StubCacheTasks(new(), failWith: new InvalidOperationException("boom")));

        var thrown = await Should.ThrowAsync<InvalidOperationException>(sut.FlushAsync());

        thrown.Message.ShouldBe("boom");
    }

    private sealed class StubCacheTasks(List<string> order, string name = "", Exception? failWith = null) : IHasCacheTasks
    {
        public Task WaitForCacheTasksAsync()
        {
            order.Add(name);

            return failWith is null ? Task.CompletedTask : Task.FromException(failWith);
        }
    }
}
