namespace FEx.AspNetCorex.Abstractions;

/// <summary>
/// A completed response, captured so a retry of the same write can be answered without executing it again.
/// Carries only what the middleware replays - status, content type and body - and nothing that would make it
/// specific to one transport or one storage engine.
/// </summary>
public sealed class IdempotentResponse
{
    public int StatusCode { get; init; }

    public string ContentType { get; init; } = "application/json";

    public byte[] Body { get; init; } = [];

    /// <summary>
    /// True when the request completed (with <see cref="StatusCode" />) but its body was too large to keep:
    /// <see cref="Body" /> is empty, and a retry is refused instead of being answered with a replay - or
    /// executed a second time.
    /// </summary>
    public bool BodyNotStored { get; init; }
}
