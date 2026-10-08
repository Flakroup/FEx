using FEx.PersistentStorage.Abstractions.Enums;
using FEx.PersistentStorage.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Linq.Expressions;
using Xunit;

namespace FEx.PersistentStorage.Tests;

public sealed class CachedSubjectBaseTests
{
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    [Fact]
    public void Constructor_WithoutDefault_StartsWithNullAndKeepsReason()
    {
        using var sut = new DirectSubject(_cache);

        sut.Value.ShouldBeNull();
        sut.ClearCacheReason.ShouldBe(ClearCacheReason.IsFirstLaunchForCurrentBuild);
    }

    [Fact]
    public void Initialize_DelegatesToRetrieveFromCache()
    {
        using var sut = new DirectSubject(_cache);

        sut.Initialize();

        sut.RetrieveCalls.ShouldBe(1);
    }

    [Fact]
    public void ClearCache_WhenValueIsDefault_DoesNotDeleteFromStorage()
    {
        using var sut = new DirectSubject(_cache);

        sut.ClearCache();

        _cache.DidNotDelete();
    }

    [Fact]
    public void ClearCache_WhenValueSet_DeletesFromStorageAndRestoresDefault()
    {
        using var sut = new DirectSubject(_cache);
        sut.Publish("x");

        sut.ClearCache();

        sut.Value.ShouldBeNull();
        _cache.Received(1).Delete((Expression<Func<CachedItem, bool>>?)null);
    }
}
