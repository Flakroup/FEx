using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace FEx.PersistentStorage.Tests;

public sealed class CollectionCachedSubjectTests
{
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    [Fact]
    public void Value_InitiallyEmpty_AndExposesClearCacheReason()
    {
        using var sut = new CollectionSubject(_cache);

        sut.Value.ShouldBeEmpty();
        sut.ClearCacheReason.ShouldBe(ClearCacheReason.LogOut);
    }

    [Fact]
    public void Initialize_WhenCacheEmpty_LeavesValueEmpty()
    {
        using var sut = new CollectionSubject(_cache);
        _cache.Get<CachedItem>(null).Returns(Array.Empty<CachedItem>());

        sut.Initialize();

        sut.Value.ShouldBeEmpty();
    }

    [Fact]
    public void Initialize_WhenCacheHasItems_PublishesConvertedModels()
    {
        using var sut = new CollectionSubject(_cache);
        var recorder = new Recorder<IReadOnlyCollection<DisposableModel>>();
        using var subscription = sut.Subscribe(recorder);
        _cache.Get<CachedItem>(null).Returns([new CachedItem("a"), new CachedItem("b")]);

        sut.Initialize();

        sut.Value.Select(static m => m.Name).ShouldBe(["a", "b"]);
        recorder.Values.Count.ShouldBe(2);
    }

    [Fact]
    public void Initialize_WhenValueAlreadyPopulated_DoesNotReadCache()
    {
        using var sut = new CollectionSubject(_cache);
        sut.OnNext([Models.Create("live")]);
        _cache.ClearReceivedCalls();

        sut.Initialize();

        sut.Value.Select(static m => m.Name).ShouldBe(["live"]);
        _cache.DidNotReceive().Get(Arg.Any<Expression<Func<CachedItem, bool>>?>());
    }

    [Fact]
    public void Initialize_WhenAllCachedItemsConvertToNull_LeavesValueEmpty()
    {
        using var sut = new CollectionSubject(_cache, static _ => null);
        _cache.Get<CachedItem>(null).Returns([new CachedItem("a")]);

        sut.Initialize();

        sut.Value.ShouldBeEmpty();
    }

    [Fact]
    public void Initialize_SkipsItemsThatConvertToNull()
    {
        using var sut = new CollectionSubject(_cache, static c => c.Name == "bad" ? null : Models.Create(c.Name));
        _cache.Get<CachedItem>(null).Returns([new CachedItem("bad"), new CachedItem("good")]);

        sut.Initialize();

        sut.Value.Select(static m => m.Name).ShouldBe(["good"]);
    }

    [Fact]
    public void OnNext_ReplacesCacheAndPublishes()
    {
        using var sut = new CollectionSubject(_cache);
        var recorder = new Recorder<IReadOnlyCollection<DisposableModel>>();
        using var subscription = sut.Subscribe(recorder);

        sut.OnNext([Models.Create("a"), Models.Create("b")]);

        _cache.Replaced().Select(static i => i.Name).ShouldBe(["a", "b"]);
        sut.Value.Select(static m => m.Name).ShouldBe(["a", "b"]);
        recorder.Values.Count.ShouldBe(2);
    }

    [Fact]
    public void OnNext_DisposesPreviousDisposableItems()
    {
        using var sut = new CollectionSubject(_cache);
        var old = Models.Create("old");
        sut.OnNext([old]);

        sut.OnNext([Models.Create("new")]);

        old.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public void OnNext_EmptyWhileEmpty_IsIgnored()
    {
        using var sut = new CollectionSubject(_cache);
        var recorder = new Recorder<IReadOnlyCollection<DisposableModel>>();
        using var subscription = sut.Subscribe(recorder);

        sut.OnNext([]);

        _cache.ReceivedCalls().ShouldBeEmpty();
        recorder.Values.Count.ShouldBe(1);
    }

    [Fact]
    public void OnNext_EmptyWhilePopulated_ClearsCacheDisposesItemsAndRestoresDefault()
    {
        using var sut = new CollectionSubject(_cache);
        var item = Models.Create("a");
        sut.OnNext([item]);

        sut.OnNext([]);

        item.IsDisposed.ShouldBeTrue();
        sut.Value.ShouldBeEmpty();
        _cache.Received(1).Delete((Expression<Func<CachedItem, bool>>?)null);
    }

    [Fact]
    public void OnNext_NullWhilePopulated_ClearsCache()
    {
        using var sut = new CollectionSubject(_cache);
        sut.OnNext([Models.Create("a")]);

        sut.OnNext(null!);

        sut.Value.ShouldBeEmpty();
        _cache.Received(1).Delete((Expression<Func<CachedItem, bool>>?)null);
    }

    [Fact]
    public void OnNext_NullWhileEmpty_DoesNotTouchStorage()
    {
        using var sut = new CollectionSubject(_cache);

        sut.OnNext(null!);

        _cache.DidNotDelete();
    }

    [Fact]
    public void ClearCache_WhenEmpty_DoesNothing()
    {
        using var sut = new CollectionSubject(_cache);

        sut.ClearCache();

        _cache.DidNotDelete();
    }
}
