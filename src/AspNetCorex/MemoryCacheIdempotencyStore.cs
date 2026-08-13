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
        _cache.Set(key, response, lifetime);

        return Task.CompletedTask;
    }
}
