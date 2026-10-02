namespace FEx.AspNetCorex;

/// <summary>Settings for <see cref="IdempotencyMiddleware" />; configure through <c>AddIdempotency(options => ...)</c>.</summary>
public sealed class IdempotencyOptions
{
    /// <summary>The default for <see cref="MaxStoredResponseBytes" />: 1 MiB.</summary>
    public const long DefaultMaxStoredResponseBytes = 1024 * 1024;

    /// <summary>
    /// The largest response body, in bytes, that is held in memory and stored for replay. A larger response
    /// still reaches the client in full - it streams through once it outgrows this - but it is not stored,
    /// so a retry with the same key executes the handler again. Bounds the memory one request can pin and,
    /// with the default store, the size of each cache entry.
    /// </summary>
    public long MaxStoredResponseBytes { get; set; } = DefaultMaxStoredResponseBytes;
}
