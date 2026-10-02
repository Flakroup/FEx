using FEx.AspNetCorex.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.AspNetCorex;

/// <summary>
/// The default <see cref="IIdempotencyStore"/>: an in-process memory cache. Enough for an app whose writes
/// are cheap to repeat - but it forgets everything on restart, and a deploy or a crash is exactly when a
/// client's outbox retries. An app where a repeated write costs something real (money, a booking) supplies
/// its own store over storage that outlives the process.
/// </summary>
/// <remarks>
/// Each entry's <see cref="ICacheEntry.Size" /> is its body length in bytes, so setting
/// <see cref="MemoryCacheOptions.SizeLimit" /> on the cache caps the total the store can hold.
/// </remarks>
public sealed class MemoryCacheIdempotencyStore : IIdempotencyStore
{
    private readonly IMemoryCache _cache;

    public MemoryCacheIdempotencyStore(IMemoryCache cache) => _cache = cache;

    public Task<IdempotentResponse?> TryGetAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cache.TryGetValue(key, out IdempotentResponse? cached) ? cached : null);

    public Task SetAsync(
        string key,
        IdempotentResponse response,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        // Size lets a host bound the whole cache with MemoryCacheOptions.SizeLimit (and without it a size-limited
        // cache refuses the entry outright). One unit per byte; an empty body still counts as one entry.
        _cache.Set(key,
            response,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lifetime,
                Size = Math.Max(1, response.Body.Length)
            });

        return Task.CompletedTask;
    }
}
