using FEx.Offline.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FEx.Offline;

/// <summary>One queued write. <see cref="Id" /> doubles as the Idempotency-Key the server replays on.</summary>
public sealed record OutboxEntry(
    Guid Id,
    string Method,
    string Url,
    string? JsonBody,
    DateTimeOffset CreatedAtUtc,
    int Attempts,
    string? LastError);

/// <summary>What a flush did: how many writes landed, how many the server rejected outright, what remains.</summary>
public sealed record OutboxFlushResult(int Sent, int Rejected, int Remaining, string? LastError);

/// <summary>
/// Store-and-forward queue for writes made while offline. Enqueue persists the request; flush
/// replays the queue in order, sending each entry's id as the Idempotency-Key header so a retry of a
/// request that DID land (but whose response was lost) never double-executes. A 2xx removes the
/// entry; a 4xx removes it too (the server understood and rejected it - retrying forever cannot fix a
/// validation error) and reports it; a transport failure or 5xx keeps the entry, records the error and
/// stops the flush (the network is down or the server is sick - hammering the rest of the queue would
/// not help).
/// </summary>
public sealed class HttpOutbox
{
    /// <summary>
    /// The header this outbox sends and the header a server-side idempotency middleware (e.g.
    /// <c>FEx.AspNetCorex.IdempotencyMiddleware.HeaderName</c>) must replay on - this project cannot
    /// reference ASP.NET Core, so the two sides keep their own copy of the same header name.
    /// </summary>
    public const string IdempotencyHeader = "Idempotency-Key";

    private const string KeyPrefix = "outbox:";

    private readonly IKeyValueStore _store;
    private readonly TimeProvider _time;
    private readonly JsonSerializerOptions _json;

    public HttpOutbox(IKeyValueStore store, TimeProvider time, JsonSerializerOptions? json = null)
    {
        _store = store;
        _time = time;
        _json = json ?? JsonSerializerOptions.Web;
    }

    public async Task<OutboxEntry> EnqueueAsync(string method, string url, string? jsonBody)
    {
        OutboxEntry entry = new(Guid.NewGuid(), method, url, jsonBody, _time.GetUtcNow(), 0, null);
        await SaveAsync(entry);

        return entry;
    }

    public async Task<int> CountAsync() => (await _store.GetKeysAsync(KeyPrefix)).Count;

    public async Task<IReadOnlyList<OutboxEntry>> ListAsync()
    {
        // The key embeds the zero-padded creation ticks, so sorting keys replays in enqueue order.
        var keys = await _store.GetKeysAsync(KeyPrefix);
        List<OutboxEntry> entries = [];

        foreach (var key in keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (await _store.GetAsync(key) is { } stored)
                entries.Add(JsonSerializer.Deserialize<OutboxEntry>(stored, _json)
                            ?? throw new InvalidOperationException($"Outbox entry '{key}' stored as JSON null."));
        }

        return entries;
    }

    public async Task<OutboxFlushResult> FlushAsync(HttpClient http)
    {
        var sent = 0;
        var rejected = 0;
        string? lastError = null;

        foreach (var entry in await ListAsync())
        {
            using HttpRequestMessage request = new(HttpMethod.Parse(entry.Method), entry.Url);

            if (entry.JsonBody is not null)
                request.Content = new StringContent(entry.JsonBody, Encoding.UTF8, "application/json");

            request.Headers.Add(IdempotencyHeader, entry.Id.ToString());

            try
            {
                using var response = await http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await RemoveAsync(entry);
                    sent++;

                    continue;
                }

                if ((int)response.StatusCode is >= 400 and < 500)
                {
                    // The server understood and said no (validation, auth, conflict). Retrying an
                    // unchanged request cannot succeed - drop it and surface the rejection.
                    var body = await response.Content.ReadAsStringAsync();
                    await RemoveAsync(entry);
                    rejected++;
                    lastError = $"HTTP {(int)response.StatusCode}: {Truncate(body)}";

                    continue;
                }

                // Server-side trouble (5xx): worth retrying later, not worth hammering now.
                var error = $"HTTP {(int)response.StatusCode}";

                await SaveAsync(entry with
                {
                    Attempts = entry.Attempts + 1,
                    LastError = error
                });

                lastError = error;

                break;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                // Still offline: keep the entry (with the error on record) and stop - the rest of
                // the queue would fail the same way.
                await SaveAsync(entry with
                {
                    Attempts = entry.Attempts + 1,
                    LastError = e.Message
                });

                lastError = e.Message;

                break;
            }
        }

        return new(sent, rejected, await CountAsync(), lastError);
    }

    private static string KeyFor(OutboxEntry entry) => $"{KeyPrefix}{entry.CreatedAtUtc.UtcTicks:D19}:{entry.Id:N}";

    private static string Truncate(string value) =>
        value.Length <= 200
            ? value
            : value[..200];

    private Task SaveAsync(OutboxEntry entry) => _store.SetAsync(KeyFor(entry), JsonSerializer.Serialize(entry, _json));

    private Task RemoveAsync(OutboxEntry entry) => _store.RemoveAsync(KeyFor(entry));
}