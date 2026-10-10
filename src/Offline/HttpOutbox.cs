using FEx.Offline.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FEx.Offline;

/// <summary>
/// One queued write. <see cref="Id" /> doubles as the Idempotency-Key the server replays on.
/// <see cref="Attempts" /> counts every failed replay, a dropped connection included;
/// <see cref="ServerFailures" /> counts only the 5xx answers other than 502, 503 and 504, which the outbox's
/// <c>maxServerFailures</c> argument caps (<see cref="HttpOutbox.DefaultMaxServerFailures" /> by default), and
/// <see cref="UnavailableAnswers" /> counts the 408, 502, 503 and 504 ones and the redirected answers (followed or not), which the
/// <c>maxUnavailableAnswers</c> argument caps (<see cref="HttpOutbox.DefaultMaxUnavailableAnswers" /> by default).
/// </summary>
public sealed record OutboxEntry(
    Guid Id,
    string Method,
    string Url,
    string? JsonBody,
    DateTimeOffset CreatedAtUtc,
    int Attempts,
    string? LastError,
    int ServerFailures = 0,
    int UnavailableAnswers = 0);

/// <summary>
/// A write the outbox gave up on and keeps for the consumer to read: the server rejected it with a 4xx, or
/// answered 5xx, 408, a gateway error or a redirect until the retry limit. It is never replayed. <see cref="StatusCode" /> and
/// <see cref="ResponseBody" /> (truncated) are the server's last answer.
/// </summary>
public sealed record DeadOutboxEntry(
    Guid Id,
    string Method,
    string Url,
    string? JsonBody,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset DeadAtUtc,
    int ServerFailures,
    int StatusCode,
    string ResponseBody);

/// <summary>
/// What a flush did: how many writes landed, how many it moved to the dead-letter list, what remains in the
/// queue.
/// </summary>
public sealed record OutboxFlushResult(int Sent, int DeadLettered, int Remaining, string? LastError);

/// <summary>
/// Store-and-forward queue for writes made while offline. Enqueue persists the request; flush
/// replays the queue in order, sending each entry's id as the Idempotency-Key header so a retry of a
/// request that DID land (but whose response was lost) never double-executes, plus every replay header
/// the host registered. A 2xx removes the entry, and so does a 409 whose
/// <c>Idempotency-Original-Status</c> header carries a 2xx status (the write completed on an earlier attempt, only its
/// response was too large for the server to replay; a browser client on another origin only sees that header when the
/// server lists it in <c>Access-Control-Expose-Headers</c>). A 4xx other than 401, 408 and 429 moves it to the dead-letter
/// list (the server understood and rejected it - retrying forever cannot fix a validation error) and the flush
/// goes on. A 5xx other than 502, 503 and 504 keeps the entry, counts a server failure on it and stops the flush (the server is sick -
/// hammering the rest of the queue would not help); at the limit of server failures the entry moves to the
/// dead-letter list instead and the flush goes on, so one write the server keeps refusing cannot block the
/// ones behind it. A 408, 502, 503 or 504 keeps the entry, records the error and stops the flush too, and counts as an
/// unavailable answer on a separate counter with a far higher limit: such an answer says the origin was unreachable, not that the
/// write is wrong, so a short outage buries nothing, yet a write the origin answers that way on every flush is
/// dead-lettered at that limit instead of blocking the queue forever. A 401, 429 or a transport failure keeps the entry,
/// records the error and stops the flush as well, but is never counted: the session needs renewing, the server is
/// throttling or the network is down, none of which says anything about the write. An answer that arrives after a
/// redirect the client followed (the final request's method or path is not the queued one - a login page standing in
/// for an expired session, say), or a 3xx the client did not follow, is never a delivery, whatever its status: it keeps
/// the entry like a 401 and counts as an unavailable answer, so a redirect that never ends is dead-lettered at that limit.
/// A write that really lands behind a redirect (a 303 after a POST, a 307 to another path) is replayed until that limit,
/// so an API that redirects on success should answer 2xx instead. Dead entries are listed with
/// <see cref="ListDeadAsync" /> and deleted with <see cref="RemoveDeadAsync" />.
/// </summary>
public sealed class HttpOutbox
{
    /// <summary>
    /// The header this outbox sends and the header a server-side idempotency middleware (e.g.
    /// <c>FEx.AspNetCorex.IdempotencyMiddleware.HeaderName</c>) must replay on - this project cannot
    /// reference ASP.NET Core, so the two sides keep their own copy of the same header name.
    /// </summary>
    public const string IdempotencyHeader = "Idempotency-Key";

    // Same reason as IdempotencyHeader: a copy of FEx.AspNetCorex.IdempotencyMiddleware.OriginalStatusHeaderName,
    // which this project cannot reference. HttpOutboxIdempotencyTests runs the outbox against the real middleware.
    private const string OriginalStatusHeader = "Idempotency-Original-Status";

    /// <summary>
    /// How many server failures an entry takes before it moves to the dead-letter list. A server failure is a 5xx answer
    /// other than 502, 503 and 504. 408, 502, 503 and 504 count on a separate counter, see <see cref="DefaultMaxUnavailableAnswers" />;
    /// 401 and 429, like a transport failure, never count, because they say nothing about the write itself.
    /// </summary>
    public const int DefaultMaxServerFailures = 10;

    /// <summary>
    /// How many 408, 502, 503, 504 or followed-redirect answers an entry takes before it moves to the dead-letter list. They say
    /// the origin was unreachable or overloaded, or the session needs renewing, not that the write is wrong, so the limit is far above <see cref="DefaultMaxServerFailures" />:
    /// an outage of that many flushes buries nothing a healthy origin would refuse, while a write that always gets such an
    /// answer still stops blocking the queue behind it.
    /// </summary>
    public const int DefaultMaxUnavailableAnswers = 100;

    private const string KeyPrefix = "outbox:";

    // Deliberately not "outbox:" plus a suffix: ListAsync and CountAsync read by that prefix, which must never reach a dead entry.
    private const string DeadKeyPrefix = "outbox-dead:";

    private readonly IKeyValueStore _store;
    private readonly TimeProvider _time;
    private readonly JsonSerializerOptions _json;
    private readonly KeyValuePair<string, string>[] _replayHeaders;
    private readonly int _maxServerFailures;
    private readonly int _maxUnavailableAnswers;

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxServerFailures" /> or <paramref name="maxUnavailableAnswers" /> is below 1.</exception>
    public HttpOutbox(IKeyValueStore store,
                      TimeProvider time,
                      JsonSerializerOptions? json = null,
                      int maxServerFailures = DefaultMaxServerFailures,
                      int maxUnavailableAnswers = DefaultMaxUnavailableAnswers)
        : this(store, time, json, new Dictionary<string, string>(), maxServerFailures, maxUnavailableAnswers)
    {
    }

    /// <summary>
    /// An outbox whose every replay also carries <paramref name="replayHeaders" /> - for a server that refuses
    /// a write lacking a header the host's own client always sends (a marker forcing a CORS preflight, say).
    /// Without it such a write replays bare, the server answers 4xx, and the flush dead-letters it.
    /// <para>
    /// Supplied at replay time, never persisted with the entry. The value is the host's to assert on the
    /// request the flush sends now, not a fact captured from the one that failed - so a write parked before
    /// the header was registered, by this version or an older one, still replays with it, the stored entry
    /// keeps the shape every version reads, and nothing new is written to the store. The cost: a header
    /// whose value differs per request cannot be carried this way.
    /// </para>
    /// <para>
    /// Never register a credential. The headers ride on every replay to whatever URL the entry holds, an
    /// absolute one included, and on to wherever a redirect the client follows leads (an HttpClient drops only
    /// Authorization on another origin), and a malformed value is echoed in the exception this constructor throws.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="replayHeaders" /> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxServerFailures" /> or <paramref name="maxUnavailableAnswers" /> is below 1.</exception>
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
                      IReadOnlyDictionary<string, string> replayHeaders,
                      int maxServerFailures = DefaultMaxServerFailures,
                      int maxUnavailableAnswers = DefaultMaxUnavailableAnswers)
    {
        ArgumentNullException.ThrowIfNull(replayHeaders);

        if (maxServerFailures < 1)
            throw new ArgumentOutOfRangeException(nameof(maxServerFailures), maxServerFailures,
                "An entry needs at least one server failure to be dead-lettered.");

        if (maxUnavailableAnswers < 1)
            throw new ArgumentOutOfRangeException(nameof(maxUnavailableAnswers), maxUnavailableAnswers,
                "An entry needs at least one unavailable answer to be dead-lettered.");

        _store = store;
        _time = time;
        _json = json ?? JsonSerializerOptions.Web;
        _replayHeaders = [.. replayHeaders];
        _maxServerFailures = maxServerFailures;
        _maxUnavailableAnswers = maxUnavailableAnswers;

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

    /// <summary>How many writes wait for a replay. Dead entries are not among them.</summary>
    public async Task<int> CountAsync() => (await _store.GetKeysAsync(KeyPrefix)).Count;

    /// <summary>The writes waiting for a replay, in enqueue order. Dead entries are not among them.</summary>
    public async Task<IReadOnlyList<OutboxEntry>> ListAsync() =>
        await LoadAsync(KeyPrefix, stored => JsonSerializer.Deserialize<OutboxEntry>(stored, _json));

    /// <summary>The writes the outbox gave up on, oldest first. They are never replayed.</summary>
    public async Task<IReadOnlyList<DeadOutboxEntry>> ListDeadAsync() =>
        await LoadAsync(DeadKeyPrefix, stored => JsonSerializer.Deserialize(stored, DeadOutboxEntryJsonContext.Default.DeadOutboxEntry));

    /// <summary>Delete one dead entry. False when no dead entry has this id.</summary>
    public async Task<bool> RemoveDeadAsync(Guid id)
    {
        var suffix = $":{id:N}";
        var removed = false;

        foreach (var key in await _store.GetKeysAsync(DeadKeyPrefix))
        {
            if (!key.EndsWith(suffix, StringComparison.Ordinal))
                continue;

            await _store.RemoveAsync(key);
            removed = true;
        }

        return removed;
    }

    public async Task<OutboxFlushResult> FlushAsync(HttpClient http)
    {
        var sent = 0;
        var deadLettered = 0;
        string? lastError = null;

        foreach (var entry in await ListAsync())
        {
            using HttpRequestMessage request = new(HttpMethod.Parse(entry.Method), entry.Url);

            if (entry.JsonBody is not null)
                request.Content = new StringContent(entry.JsonBody, Encoding.UTF8, "application/json");

            foreach (var (name, value) in _replayHeaders)
                request.Headers.Add(name, value);

            request.Headers.Add(IdempotencyHeader, entry.Id.ToString());

            // Taken before the send: the client follows a redirect by rewriting this very request, so afterwards it
            // can no longer tell what was queued.
            var queuedMethod = request.Method;
            var queuedUri = request.RequestUri switch
            {
                null => http.BaseAddress,
                { IsAbsoluteUri: false } relative when http.BaseAddress is { } baseAddress => new Uri(baseAddress, relative),
                var uri => uri
            };

            try
            {
                using var response = await http.SendAsync(request);

                var status = (int)response.StatusCode;

                // A client that follows redirects hands back the final answer: a login page's 200 for a write whose session
                // expired is not a delivery, so a redirected answer is judged before its status. A 3xx that reaches this point
                // is a redirect nobody followed (a client that does not, or one that gave up after too many hops).
                var redirected = status is >= 300 and < 400 || IsFollowedRedirect(queuedMethod, queuedUri, response);

                if (!redirected && (response.IsSuccessStatusCode || IsCompletedEarlier(response)))
                {
                    await RemoveAsync(entry);

                    // A stale dead copy of this very write (the flush died between burying it and removing the live
                    // entry) must not tell the consumer that a write which landed has failed.
                    await _store.RemoveAsync(DeadKeyFor(entry));
                    sent++;

                    continue;
                }

                // Only a 5xx other than 502/503/504 says the server refused the write itself. A 408/502/503/504 (timeout, failing
                // gateway, temporary overload) says the origin was unreachable: it counts on its own, much higher limit, so a
                // short outage buries nothing but a write that always meets it cannot block the queue for good. So does a
                // followed redirect (the session is not enough, like a 401, but one that never ends must not block either). A 401,
                // a 429 (throttling) and a dropped connection say nothing about the write and never count - a device offline
                // for a week must not lose its writes.
                var unavailable = redirected
                                  || response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.BadGateway
                                      or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;
                var countsAsServerFailure = status >= 500 && !unavailable;
                var serverFailures = entry.ServerFailures + (countsAsServerFailure ? 1 : 0);
                var unavailableAnswers = entry.UnavailableAnswers + (unavailable ? 1 : 0);

                // 401 means the session is not enough - once the user signs in again the unchanged write can
                // land, and 408/429 are transient by definition, so these stay queued like a 5xx. Every other
                // 4xx, 403 included, is the server refusing this request itself (validation, permission,
                // conflict): a retry cannot succeed, so it is dead-lettered for the consumer to read.
                var rejected = status is >= 400 and < 500 && !redirected && !IsTransientClientError(response.StatusCode);

                if (rejected
                    || countsAsServerFailure && serverFailures >= _maxServerFailures
                    || unavailable && unavailableAnswers >= _maxUnavailableAnswers)
                {
                    var body = await ReadBodyAsync(response);

                    await DeadLetterAsync(entry with { ServerFailures = serverFailures }, status, body);

                    deadLettered++;
                    lastError = $"HTTP {status}: {body}";

                    continue;
                }

                // Server-side trouble (5xx), a session to renew (401) or throttling/overload/gateway failure (408/429/502/503/504): worth retrying later,
                // not worth hammering now - stopping the flush here also spares a rate-limited server the rest of the queue.
                // The redirect target is left out of the error: a Location can carry a token or a signature, and LastError is stored.
                var error = redirected ? $"HTTP {status} after a redirect" : $"HTTP {status}";

                await SaveAsync(entry with
                {
                    Attempts = entry.Attempts + 1,
                    ServerFailures = serverFailures,
                    UnavailableAnswers = unavailableAnswers,
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

        return new(sent, deadLettered, await CountAsync(), lastError);
    }

    // The server's idempotency layer answers 409 + the original 2xx status when a replayed write had already completed
    // but its response was too large to store: the write landed, so it is delivered, not rejected.
    private static bool IsCompletedEarlier(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.Conflict
        && response.Headers.TryGetValues(OriginalStatusHeader, out var values)
        && int.TryParse(values.FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var original)
        && original is >= 200 and < 300;

    // A followed redirect leaves the final request with another method (a 302 turns a POST into a GET) or another path. Scheme,
    // host, port, query and fragment are deliberately not compared: a handler in the client pipeline that routes or hedges
    // across hosts, or stamps a query-string key, rewrites them on every request, and each delivered 2xx would then look
    // undelivered. The ceiling: a handler that rewrites the path is still taken for a redirect, and so is a redirect that
    // changes only the host or the query (a write that landed there is replayed; the idempotency key makes that safe).
    // A response with no request attached (a hand-built one) is never a redirect.
    private static bool IsFollowedRedirect(HttpMethod queuedMethod, Uri? queuedUri, HttpResponseMessage response) =>
        response.RequestMessage is { } final
        && (final.Method != queuedMethod || !string.Equals(PathOf(final.RequestUri), PathOf(queuedUri), StringComparison.Ordinal));

    private static string? PathOf(Uri? uri) => uri is { IsAbsoluteUri: true } ? uri.AbsolutePath : null;

    private static bool IsTransientClientError(HttpStatusCode status) =>
        status is HttpStatusCode.Unauthorized or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;

    private static string KeyFor(OutboxEntry entry) => $"{KeyPrefix}{entry.CreatedAtUtc.UtcTicks:D19}:{entry.Id:N}";

    private static string DeadKeyFor(OutboxEntry entry) => $"{DeadKeyPrefix}{entry.CreatedAtUtc.UtcTicks:D19}:{entry.Id:N}";

    private static string Truncate(string value) =>
        value.Length <= 200
            ? value
            : value[..200];

    // A server (or a proxy) can send a Content-Type whose charset .NET cannot decode, and reading the body then
    // throws: out of the flush, before the entry could be counted or buried, so the same write would block the queue
    // forever. (A connection that drops mid-body never gets here: SendAsync buffers the body and fails as a transport error.)
    private static async Task<string> ReadBodyAsync(HttpResponseMessage response)
    {
        try
        {
            return Truncate(await response.Content.ReadAsStringAsync());
        }
        catch (InvalidOperationException)
        {
            return "";
        }
    }

    private async Task<List<T>> LoadAsync<T>(string prefix, Func<string, T?> deserialize)
    {
        // The key embeds the zero-padded creation ticks, so sorting keys replays in enqueue order.
        var keys = await _store.GetKeysAsync(prefix);
        List<T> entries = [];

        foreach (var key in keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (await _store.GetAsync(key) is { } stored)
                entries.Add(deserialize(stored)
                            ?? throw new InvalidOperationException($"Outbox entry '{key}' stored as JSON null."));
        }

        return entries;
    }

    // The dead copy is written with FEx's own metadata, not the consumer's options: a source-generated resolver that lists
    // OutboxEntry but not DeadOutboxEntry would throw here, before the live entry is removed, on every flush.
    // The dead copy lands before the live one goes: a crash in between replays the write once more under the same
    // Idempotency-Key and buries it again, instead of losing it.
    private async Task DeadLetterAsync(OutboxEntry entry, int status, string body)
    {
        DeadOutboxEntry dead = new(entry.Id, entry.Method, entry.Url, entry.JsonBody, entry.CreatedAtUtc,
            _time.GetUtcNow(), entry.ServerFailures, status, body);

        await _store.SetAsync(DeadKeyFor(entry), JsonSerializer.Serialize(dead, DeadOutboxEntryJsonContext.Default.DeadOutboxEntry));
        await RemoveAsync(entry);
    }

    private Task SaveAsync(OutboxEntry entry) => _store.SetAsync(KeyFor(entry), JsonSerializer.Serialize(entry, _json));

    private Task RemoveAsync(OutboxEntry entry) => _store.RemoveAsync(KeyFor(entry));
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DeadOutboxEntry))]
internal sealed partial class DeadOutboxEntryJsonContext : JsonSerializerContext;
