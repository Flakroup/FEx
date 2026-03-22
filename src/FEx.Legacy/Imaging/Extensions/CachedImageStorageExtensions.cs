using FEx.Agnostics.Abstractions.Models;
using FEx.Legacy.Imaging.Abstractions.Interfaces;
using System;
using System.Threading.Tasks;

namespace FEx.Legacy.Imaging.Extensions;

public static class CachedImageStorageExtensions
{
    public static bool EntryCacheShouldBePrepared(this ICachedImageStorage storage, IIndexEntryBase entry) =>
        storage.EntryCacheShouldBePrepared(entry, false);

    public static Task<IIndexEntryBase> GetEntryBaseAsync(this ICachedImageStorage storage, Uri fileUrl) =>
        storage.GetEntryBaseAsync(fileUrl, true, null);

    public static Task<IIndexEntryBase> PrepareCacheAndGetEntryBaseAsync(this ICachedImageStorage storage, Uri fileUrl) =>
        storage.PrepareCacheAndGetEntryBaseAsync(fileUrl, null, false);

    public static Task<bool> PrepareCacheEntryAsync(this ICachedImageStorage storage, IIndexEntryBase entry) =>
        storage.PrepareCacheEntryAsync(entry, null, false, null, null, null);

    public static Task CacheAllAsync(this ICachedImageStorage storage) =>
        storage.CacheAllAsync(null);
}
