using FEx.PersistentStorage.Abstractions;
using NSubstitute;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FEx.PersistentStorage.Tests;

public sealed class AggregatedCollectionCachedSubjectTests
{
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    [Fact]
    public void OnNext_AppendsToCurrentValueAndCachesOnlyNewItems()
    {
        using var sut = new AggregatedSubject(_cache);
        var recorder = new Recorder<IReadOnlyCollection<DisposableModel>>();
        using var subscription = sut.Subscribe(recorder);

        sut.OnNext([Models.Create("a")]);
        sut.OnNext([Models.Create("b"), Models.Create("c")]);

        sut.Value.Select(static m => m.Name).ShouldBe(["a", "b", "c"]);
        recorder.Values.Last().Select(static m => m.Name).ShouldBe(["a", "b", "c"]);
        _cache.Received(2).Add(Arg.Any<IEnumerable<CachedItem>>());
        _cache.DidNotReceiveWithAnyArgs().ReplaceWith(default(IEnumerable<CachedItem>)!);
    }

    [Fact]
    public void OnNext_StoresConvertedItems()
    {
        using var sut = new AggregatedSubject(_cache);
        IReadOnlyList<string>? stored = null;
        _cache.When(c => c.Add(Arg.Any<IEnumerable<CachedItem>>()))
            .Do(ci => stored = ci.Arg<IEnumerable<CachedItem>>().Select(static i => i.Name).ToList());

        sut.OnNext([Models.Create("x"), Models.Create("y")]);

        stored.ShouldBe(["x", "y"]);
    }

    [Fact]
    public void OnNext_DoesNotDisposeExistingItems()
    {
        using var sut = new AggregatedSubject(_cache);
        var first = Models.Create("a");
        sut.OnNext([first]);

        sut.OnNext([Models.Create("b")]);

        first.IsDisposed.ShouldBeFalse();
    }

    [Fact]
    public void OnReset_ReplacesCacheAndValueAndDisposesPreviousItems()
    {
        using var sut = new AggregatedSubject(_cache);
        var old = Models.Create("old");
        sut.OnNext([old]);
        var recorder = new Recorder<IReadOnlyCollection<DisposableModel>>();
        using var subscription = sut.Subscribe(recorder);

        sut.OnReset([Models.Create("n1"), Models.Create("n2")]);

        old.IsDisposed.ShouldBeTrue();
        sut.Value.Select(static m => m.Name).ShouldBe(["n1", "n2"]);
        _cache.Replaced().Select(static i => i.Name).ShouldBe(["n1", "n2"]);
        recorder.Values.Count.ShouldBe(2);
    }

    [Fact]
    public void OnReset_WithEmptyCollection_EmptiesValueAndCache()
    {
        using var sut = new AggregatedSubject(_cache);
        var old = Models.Create("old");
        sut.OnNext([old]);

        sut.OnReset([]);

        old.IsDisposed.ShouldBeTrue();
        sut.Value.ShouldBeEmpty();
        _cache.Replaced().ShouldBeEmpty();
        _cache.Received(1).ReplaceWith(Arg.Any<IEnumerable<CachedItem>>());
    }

    [Fact]
    public void Initialize_RestoresPersistedItemsLikeRegularCollection()
    {
        using var sut = new AggregatedSubject(_cache);
        _cache.Get<CachedItem>(null).Returns([new CachedItem("p")]);

        sut.Initialize();

        sut.Value.Select(static m => m.Name).ShouldBe(["p"]);
    }
}
