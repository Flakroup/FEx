using FEx.Offline.Abstractions;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FEx.Offline;

/// <summary>What a cached read returned: the value, whether it came from cache, and how old it is.</summary>
public sealed record CachedResult<T>(T Value, DateTimeOffset? AsOfUtc, bool FromCache);

/// <summary>
/// Network-first read cache. A successful fetch is stored with its timestamp and returned fresh; when
/// the fetch fails with a transport error the last stored snapshot is served instead, stamped so the
/// UI can show an honest "as of HH:MM" banner. No snapshot to fall back on - the failure propagates.
/// </summary>
public sealed class OfflineReadCache
{
    private const string KeyPrefix = "cache:";

    private readonly IKeyValueStore _store;
    private readonly TimeProvider _time;
    private readonly JsonSerializerOptions _json;

    public OfflineReadCache(IKeyValueStore store, TimeProvider time, JsonSerializerOptions? json = null)
    {
        _store = store;
        _time = time;
        _json = json ?? JsonSerializerOptions.Web;
    }

    public async Task<CachedResult<T>> GetAsync<T>(string key, Func<Task<T>> fetch)
    {
        try
        {
            var fresh = await fetch();
            Envelope<T> envelope = new(_time.GetUtcNow(), fresh);
            await _store.SetAsync(KeyPrefix + key, JsonSerializer.Serialize(envelope, _json));

            return new(fresh, envelope.SavedAtUtc, false);
        }
        catch (Exception fetchError) when (IsTransportFailure(fetchError))
        {
            var stored = await _store.GetAsync(KeyPrefix + key);

            if (stored is null)
                throw;

            Envelope<T>? envelope;

            try
            {
                envelope = JsonSerializer.Deserialize<Envelope<T>>(stored, _json);
            }
            catch (JsonException)
            {
                envelope = null;
            }

            if (envelope is null)
                throw;

            return new(envelope.Payload, envelope.SavedAtUtc, true);
        }
    }

    // Only a transport-level failure means "offline". A response the server actually produced
    // (4xx/5xx surfaced as an API exception) must propagate - hiding it behind stale data would
    // mask real errors.
    private static bool IsTransportFailure(Exception e) => e is HttpRequestException or TaskCanceledException;

    private sealed record Envelope<T>(DateTimeOffset SavedAtUtc, T Payload);
}