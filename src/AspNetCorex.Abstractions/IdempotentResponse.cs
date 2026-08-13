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
}
