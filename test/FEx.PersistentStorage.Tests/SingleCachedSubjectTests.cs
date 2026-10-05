using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace FEx.PersistentStorage.Tests;

public sealed class SingleCachedSubjectTests
{
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    [Fact]
    public void Exposes_ClearCacheReason_AndDefaultPriority()
    {
        using var sut = new SingleSubject(_cache);

        sut.ClearCacheReason.ShouldBe(ClearCacheReason.LogOut);
        ((IClearCache)sut).ClearCachePriority.ShouldBe(ClearCachePriority.Default);
    }

    [Fact]
    public void Value_InitiallyNull_WhenNoDefaultGiven()
    {
        using var sut = new DefaultSingleSubject(_cache);

        sut.Value.ShouldBeNull();
        sut.ClearCacheReason.ShouldBe(ClearCacheReason.ApplicationLaunched);
    }

    [Fact]
    public void Initialize_WhenCacheEmpty_KeepsDefaultAndWritesNothing()
    {
        using var sut = new SingleSubject(_cache);
        _cache.FirstOrDefault<CachedItem>(null).Returns((CachedItem?)null);

        sut.Initialize();

        sut.Value.ShouldBeNull();
        _cache.DidNotReceiveWithAnyArgs().ReplaceWith(default(CachedItem)!);
    }

    [Fact]
    public void Initialize_WhenCacheHasItem_PublishesConvertedModel()
    {
        using var sut = new SingleSubject(_cache);
        var recorder = new Recorder<PlainModel?>();
        using var subscription = sut.Subscribe(recorder);
        _cache.FirstOrDefault<CachedItem>(null).Returns(new CachedItem("cached"));

        sut.Initialize();

        sut.Value.ShouldBe(new PlainModel("cached"));
        recorder.Values.ShouldBe([null, new PlainModel("cached")]);
    }

    [Fact]
    public void Initialize_WhenValueAlreadySet_DoesNotReadCache()
    {
        using var sut = new SingleSubject(_cache);
        sut.OnNext(new PlainModel("live"));
        _cache.ClearReceivedCalls();

        sut.Initialize();

        sut.Value.ShouldBe(new PlainModel("live"));
        _cache.DidNotReceive().FirstOrDefault(Arg.Any<Expression<Func<CachedItem, bool>>?>());
    }

    [Fact]
    public void Initialize_WhenCustomDefaultAndCacheEmpty_KeepsCustomDefault()
    {
        var fallback = new PlainModel("fallback");
        using var sut = new SingleSubject(_cache, fallback);

        sut.Initialize();

        sut.Value.ShouldBeSameAs(fallback);
    }

    [Fact]
    public void OnNext_ReplacesCacheWithConvertedItemAndPublishes()
    {
        using var sut = new SingleSubject(_cache);
        var recorder = new Recorder<PlainModel?>();
        using var subscription = sut.Subscribe(recorder);

        sut.OnNext(new PlainModel("a"));

        sut.Value.ShouldBe(new PlainModel("a"));
        _cache.Replaced().Select(static i => i.Name).ShouldBe(["a"]);
        recorder.Values.ShouldBe([null, new PlainModel("a")]);
    }

    [Fact]
    public void OnNext_WhenValueEqualToCurrent_IsIgnored()
    {
        using var sut = new SingleSubject(_cache);
        sut.OnNext(new PlainModel("a"));
        _cache.ClearReceivedCalls();
        var recorder = new Recorder<PlainModel?>();
        using var subscription = sut.Subscribe(recorder);

        sut.OnNext(new PlainModel("a"));

        _cache.ReceivedCalls().ShouldBeEmpty();
        recorder.Values.Count.ShouldBe(1);
    }

    [Fact]
    public void OnNext_WhenReplacingDisposableValue_DisposesPreviousOne()
    {
        using var sut = new DefaultSingleSubject(_cache);
        var first = Models.Create("first");
        var second = Models.Create("second");
        sut.OnNext(first);

        sut.OnNext(second);

        first.IsDisposed.ShouldBeTrue();
        second.IsDisposed.ShouldBeFalse();
        sut.Value.ShouldBeSameAs(second);
    }

    [Fact]
    public void OnNext_WhenReplacingNonDisposableValue_DoesNotFail()
    {
        using var sut = new SingleSubject(_cache);
        sut.OnNext(new PlainModel("a"));

        Should.NotThrow(() => sut.OnNext(new PlainModel("b")));

        sut.Value.ShouldBe(new PlainModel("b"));
    }

    [Fact]
    public void OnNext_Null_ClearsCacheDisposesCurrentAndResetsToDefault()
    {
        using var sut = new DefaultSingleSubject(_cache);
        var current = Models.Create("a");
        sut.OnNext(current);

        sut.OnNext(null);

        current.IsDisposed.ShouldBeTrue();
        sut.Value.ShouldBeNull();
        _cache.Received(1).Delete((Expression<Func<CachedItem, bool>>?)null);
    }

    [Fact]
    public void OnNext_Null_WhenAlreadyDefault_DoesNothing()
    {
        using var sut = new DefaultSingleSubject(_cache);

        sut.OnNext(null);

        _cache.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void ClearCache_WhenValueIsDefault_DoesNotTouchStorage()
    {
        using var sut = new SingleSubject(_cache);

        sut.ClearCache();

        _cache.DidNotDelete();
    }

    [Fact]
    public void ClearCache_WithCustomDefault_RestoresDefaultAndNotifies()
    {
        var fallback = new PlainModel("fallback");
        using var sut = new SingleSubject(_cache, fallback);
        sut.OnNext(new PlainModel("live"));
        var recorder = new Recorder<PlainModel?>();
        using var subscription = sut.Subscribe(recorder);

        sut.ClearCache();

        sut.Value.ShouldBeSameAs(fallback);
        recorder.Values.Last().ShouldBeSameAs(fallback);
        _cache.Received(1).Delete((Expression<Func<CachedItem, bool>>?)null);
    }
}
