using FEx.Offline.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
/// request that DID land (but whose response was lost) never double-executes, plus every replay header
/// the host registered. A 2xx removes the entry; a 4xx other than 401 removes it too (the server understood
/// and rejected it - retrying forever cannot fix a validation error) and reports it; a 401, a transport
/// failure or a 5xx keeps the entry, records the error and
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
    private readonly KeyValuePair<string, string>[] _replayHeaders;

    public HttpOutbox(IKeyValueStore store, TimeProvider time, JsonSerializerOptions? json = null)
        : this(store, time, json, new Dictionary<string, string>())
    {
    }

    /// <summary>
    /// An outbox whose every replay also carries <paramref name="replayHeaders" /> - for a server that refuses
    /// a write lacking a header the host's own client always sends (a marker forcing a CORS preflight, say).
    /// Without it such a write replays bare, the server answers 4xx, and the flush drops it as rejected.
    /// <para>
    /// Supplied at replay time, never persisted with the entry. The value is the host's to assert on the
    /// request the flush sends now, not a fact captured from the one that failed - so a write parked before
    /// the header was registered, by this version or an older one, still replays with it, the stored entry
    /// keeps the shape every version reads, and nothing new is written to the store. The cost: a header
    /// whose value differs per request cannot be carried this way.
    /// </para>
    /// <para>
    /// Never register a credential. The headers ride on every replay to whatever URL the entry holds, an
    /// absolute one included, and a malformed value is echoed in the exception this constructor throws.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="replayHeaders" /> is null.</exception>
    /// <exception cref="ArgumentException">A header is <see cref="IdempotencyHeader" />, which the outbox
    /// sends itself from each entry's id, or a value is blank - to a server checking for a marker, an empty
    /// one is no marker at all.</exception>
    /// <exception cref="FormatException">A name or value is not a valid request header, or a value is not
    /// ASCII.</exception>
    /// <exception cref="InvalidOperationException">A name is a content header (e.g. <c>Content-Type</c>),
    /// which belongs to the body rather than the request.</exception>
    public HttpOutbox(IKeyValueStore store,
                      TimeProvider time,
                      JsonSerializerOptions? json,
                      IReadOnlyDictionary<string, string> replayHeaders)
    {
        ArgumentNullException.ThrowIfNull(replayHeaders);

        _store = store;
        _time = time;
        _json = json ?? JsonSerializerOptions.Web;
        _replayHeaders = [.. replayHeaders];

        // Refused here rather than on the first flush. A header .NET refuses throws out of FlushAsync on every
        // attempt. A non-ASCII value passes Headers.Add and can be refused by the transport instead -
        // SocketsHttpHandler refuses any, a browser's fetch whatever is not Latin-1 - and the flush reads a
        // transport refusal as "still offline", so the first entry holds the queue forever. Either way a
        // misconfigured host would find out only once a write was already parked.
        using HttpRequestMessage probe = new();

        foreach (var (name, value) in _replayHeaders)
        {
            if (string.Equals(name, IdempotencyHeader, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"'{IdempotencyHeader}' is sent by the outbox itself, from each entry's id.",
                    nameof(replayHeaders));

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Replay header '{name}' has no value.", nameof(replayHeaders));

            if (!value.All(char.IsAscii))
                throw new FormatException($"Replay header '{name}' has a non-ASCII value, which a transport may refuse at send time.");

            probe.Headers.Add(name, value);
        }
    }

    public Task<OutboxEntry> EnqueueAsync(string method, string url, string? jsonBody) =>
        EnqueueAsync(Guid.NewGuid(), method, url, jsonBody);

    /// <summary>
    /// Queue a write under a caller-chosen <paramref name="id" /> (the Idempotency-Key). Use this when the
    /// same request was already attempted online under that key: enqueuing it with the SAME key lets the
    /// server-side idempotency cache recognise a request that DID land but whose response was lost, so the
    /// replay never double-executes. The parameterless overload keeps the fresh-key behaviour for
    /// writes made purely offline (no online attempt to reconcile with).
    /// </summary>
    public async Task<OutboxEntry> EnqueueAsync(Guid id, string method, string url, string? jsonBody)
    {
        OutboxEntry entry = new(id, method, url, jsonBody, _time.GetUtcNow(), 0, null);
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

            foreach (var (name, value) in _replayHeaders)
                request.Headers.Add(name, value);

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

                // 401 means the session is not enough - once the user signs in again the unchanged write can
                // land, so it stays queued like a 5xx. Every other 4xx, 403 included, is the server refusing this
                // request itself (validation, permission, conflict): a retry cannot succeed, so drop and report it.
                if ((int)response.StatusCode is >= 400 and < 500 && response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    await RemoveAsync(entry);
                    rejected++;
                    lastError = $"HTTP {(int)response.StatusCode}: {Truncate(body)}";

                    continue;
                }

                // Server-side trouble (5xx) or a session to renew (401): worth retrying later, not worth hammering now.
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
        } // coverage-exclude: the foreach body's closing brace: every branch inside ends in continue or break, so nothing falls through to it

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