using FEx.PersistentStorage.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FEx.PersistentStorage.Tests;

public sealed class EnhancedCollectionCachedSubjectTests
{
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    [Fact]
    public void Initialize_ConvertsEveryItemUsingSharedEnhancement()
    {
        using var sut = new EnhancedSubject(_cache);
        var recorder = new Recorder<IReadOnlyCollection<PlainModel>>();
        using var subscription = sut.Subscribe(recorder);
        _cache.Get<CachedItem>(null).Returns([new CachedItem("a"), new CachedItem("b")]);

        sut.Initialize();

        sut.Value.Select(static m => m.Name).ShouldBe(["a|a+b", "b|a+b"]);
        sut.EnhancementCalls.ShouldBe(1);
        recorder.Values.Count.ShouldBe(2);
    }

    [Fact]
    public void Initialize_WhenCacheEmpty_DoesNotComputeEnhancement()
    {
        using var sut = new EnhancedSubject(_cache);
        _cache.Get<CachedItem>(null).Returns(Array.Empty<CachedItem>());

        sut.Initialize();

        sut.Value.ShouldBeEmpty();
        sut.EnhancementCalls.ShouldBe(0);
    }

    [Fact]
    public void Initialize_WhenValueAlreadyPopulated_DoesNotReadCache()
    {
        using var sut = new EnhancedSubject(_cache);
        sut.OnNext([new PlainModel("live")]);
        _cache.ClearReceivedCalls();

        sut.Initialize();

        sut.Value.Select(static m => m.Name).ShouldBe(["live"]);
        sut.EnhancementCalls.ShouldBe(0);
        _cache.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void OnNext_StoresConvertedItemsInCache()
    {
        using var sut = new EnhancedSubject(_cache);

        sut.OnNext([new PlainModel("a")]);

        _cache.Replaced().Select(static i => i.Name).ShouldBe(["a"]);
        sut.Value.Select(static m => m.Name).ShouldBe(["a"]);
    }
}
