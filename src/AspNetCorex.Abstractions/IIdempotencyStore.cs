using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.AspNetCorex.Abstractions;

/// <summary>
/// Where the idempotent-POST middleware keeps the responses it replays. Deliberately an abstraction rather
/// than a fixed cache: the in-memory default loses everything the moment the process restarts, which is
/// exactly the window in which a client's offline outbox retries - so an app whose writes move money backs
/// this with storage that outlives the process.
/// </summary>
/// <remarks>
/// Resolved per request, not per application: an implementation over a database needs a request-scoped
/// connection, so the middleware takes it as an <c>InvokeAsync</c> parameter.
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>The stored response for <paramref name="key"/>, or null when there is none (or it expired).</summary>
    Task<IdempotentResponse?> TryGetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Stores <paramref name="response"/> under <paramref name="key"/> for <paramref name="lifetime"/>.</summary>
    Task SetAsync(string key, IdempotentResponse response, TimeSpan lifetime, CancellationToken cancellationToken = default);
}
