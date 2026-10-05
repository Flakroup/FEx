using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Enums;
using FEx.PersistentStorage.Rx.Subjects;
using LiteDB;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace FEx.PersistentStorage.Tests;

public sealed class CachedItem(string name) : ICacheableItem
{
    public ObjectId LocalStorageId { get; } = ObjectId.NewObjectId();
    public string Name { get; } = name;
}

internal sealed record PlainModel(string Name);

internal sealed class DisposableModel(string name) : IDisposable
{
    public string Name { get; } = name;
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

internal static class Models
{
    // Ownership of the created model is handed to the subject under test, which disposes it.
    public static DisposableModel Create(string name) => new(name);
}

internal sealed class Recorder<T> : IObserver<T>
{
    public List<T> Values { get; } = [];

    public void OnNext(T value) => Values.Add(value);
    public void OnError(Exception error) { }
    public void OnCompleted() { }
}

internal sealed class SingleSubject(ICacheService cache, PlainModel? defaultValue = null)
    : SingleCachedSubject<PlainModel?, CachedItem>(cache, ClearCacheReason.LogOut, defaultValue!)
{
    protected override CachedItem ConvertModelToCachedData(PlainModel? data) => new(data!.Name);

    protected override PlainModel? ConvertCachedDataToModel(CachedItem cachedData) => new(cachedData.Name);
}

internal sealed class DefaultSingleSubject(ICacheService cache)
    : SingleCachedSubject<DisposableModel?, CachedItem>(cache, ClearCacheReason.ApplicationLaunched)
{
    protected override CachedItem ConvertModelToCachedData(DisposableModel? data) => new(data!.Name);

    protected override DisposableModel? ConvertCachedDataToModel(CachedItem cachedData) => new(cachedData.Name);
}

internal sealed class CollectionSubject(ICacheService cache, Func<CachedItem, DisposableModel?>? convert = null)
    : CollectionCachedSubject<DisposableModel, CachedItem>(cache, ClearCacheReason.LogOut)
{
    protected override CachedItem ConvertModelToCachedData(DisposableModel data) => new(data.Name);

    protected override DisposableModel ConvertCachedDataToModel(CachedItem cachedData) =>
        (convert ?? (static c => new DisposableModel(c.Name)))(cachedData)!;
}

internal sealed class AggregatedSubject(ICacheService cache)
    : AggregatedCollectionCachedSubject<DisposableModel, CachedItem>(cache, ClearCacheReason.LogOut)
{
    protected override CachedItem ConvertModelToCachedData(DisposableModel data) => new(data.Name);

    protected override DisposableModel ConvertCachedDataToModel(CachedItem cachedData) => new(cachedData.Name);
}

internal sealed class EnhancedSubject(ICacheService cache)
    : EnhancedCollectionCachedSubject<PlainModel, CachedItem, string>(cache, ClearCacheReason.LogOut)
{
    public int EnhancementCalls { get; private set; }

    protected override string GetEnhancement(List<CachedItem> cachedData)
    {
        EnhancementCalls++;

        return string.Join("+", cachedData.Select(static c => c.Name));
    }

    protected override PlainModel ConvertCachedDataToModel(CachedItem cachedData, string enhancement) =>
        new($"{cachedData.Name}|{enhancement}");

    protected override CachedItem ConvertModelToCachedData(PlainModel data) => new(data.Name);

    protected override PlainModel ConvertCachedDataToModel(CachedItem cachedData) =>
        throw new NotSupportedException();
}

internal static class CacheServiceFake
{
    public static IReadOnlyList<CachedItem> Replaced(this ICacheService cache) =>
        cache.ReceivedCalls()
            .Where(static c => c.GetMethodInfo().Name == nameof(ICacheService.ReplaceWith))
            .Select(static c => c.GetArguments()[0])
            .SelectMany(static a => a is CachedItem single ? [single] : ((IEnumerable<CachedItem>)a!).ToList())
            .ToList();

    public static void DidNotDelete(this ICacheService cache) =>
        cache.DidNotReceive().Delete((Expression<Func<CachedItem, bool>>?)null);
}

internal sealed class DirectSubject(ICacheService cache)
    : CachedSubjectBase<string?, string, CachedItem>(cache, ClearCacheReason.IsFirstLaunchForCurrentBuild)
{
    public int RetrieveCalls { get; private set; }

    public void Publish(string value) => SynchronizedOnNext(value);

    protected override void RetrieveFromCache() => RetrieveCalls++;

    protected override void DisposeCurrentData()
    {
    }

    protected override CachedItem ConvertModelToCachedData(string data) => new(data);

    protected override string ConvertCachedDataToModel(CachedItem cachedData) => cachedData.Name;
}
