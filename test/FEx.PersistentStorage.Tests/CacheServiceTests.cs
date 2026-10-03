using FEx.Agnostics.Abstractions.Interfaces;
using FEx.PersistentStorage.Abstractions;
using LiteDB;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using Xunit;

namespace FEx.PersistentStorage.Tests;

public sealed class CacheServiceTests
{
    private readonly ILocalStorageService _storage = Substitute.For<ILocalStorageService>();
    private readonly IFExLogger _logger = Substitute.For<IFExLogger>();
    private readonly CacheService _sut;

    public CacheServiceTests() => _sut = new CacheService(_storage, _logger);

    [Fact]
    public void Update_WhenStorageThrows_RethrowsAndDeletesNothing()
    {
        var item = new Item();
        _storage.Update(item).Throws(new IOException("disk busy"));

        Should.Throw<IOException>(() => _sut.Update(item));

        AssertNothingDeleted();
    }

    [Fact]
    public void Upsert_Single_WhenStorageThrows_RethrowsAndDeletesNothing()
    {
        var item = new Item();
        _storage.Upsert(item, null).Throws(new IOException("disk busy"));

        Should.Throw<IOException>(() => _sut.Upsert(item, null));

        AssertNothingDeleted();
    }

    [Fact]
    public void Upsert_Collection_WhenStorageThrows_RethrowsAndDeletesNothing()
    {
        var items = new[] { new Item(), new Item() };
        _storage.When(s => s.Upsert(items, null)).Do(_ => throw new IOException("disk busy"));

        Should.Throw<IOException>(() => _sut.Upsert(items, null));

        AssertNothingDeleted();
    }

    [Fact]
    public void FirstOrDefault_WhenStorageThrows_ReturnsDefaultAndDeletesNothing()
    {
        _storage.FirstOrDefault<Item>(null).Throws(new IOException("corrupt"));

        _sut.FirstOrDefault<Item>(null).ShouldBeNull();

        AssertNothingDeleted();
    }

    [Fact]
    public void Get_WhenStorageThrows_ReturnsEmptyAndDeletesNothing()
    {
        _storage.When(x => x.GetAll<Item>(null)).Do(_ => throw new IOException("corrupt"));

        _sut.Get<Item>(null).ShouldBeEmpty();

        AssertNothingDeleted();
    }

    private void AssertNothingDeleted()
    {
        _logger.Received(1).Error(Arg.Any<IOException>(), Arg.Any<string?>());
        _storage.DidNotReceiveWithAnyArgs().DeleteAll(default(Expression<Func<Item, bool>>));
        _storage.DidNotReceiveWithAnyArgs().Insert(default(Item)!);
        _storage.DidNotReceiveWithAnyArgs().Insert(default(IEnumerable<Item>)!);
    }

    private sealed class Item : ICacheableItem
    {
        public ObjectId LocalStorageId { get; } = ObjectId.NewObjectId();
    }
}
