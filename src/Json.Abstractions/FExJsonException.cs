using System;

namespace FEx.Json.Abstractions;

/// <summary>
/// Thrown by every <see cref="IFExJsonSerializer" /> implementation when a value cannot be serialized or a payload
/// cannot be deserialized. <see cref="Exception.InnerException" /> holds the JSON library's own exception when the
/// library raised one; an empty payload is rejected before the library is called and has none.
/// </summary>
public sealed class FExJsonException : Exception
{
    public FExJsonException(string message)
        : base(message)
    {
    }

    public FExJsonException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The exception for an empty or whitespace-only payload, shared so both implementations word it alike.</summary>
    public static FExJsonException EmptyPayload() => new("The JSON payload is empty.");

    /// <summary>Wraps a JSON library exception raised while serializing.</summary>
    public static FExJsonException SerializationFailed(Exception innerException) =>
        new($"JSON serialization failed: {innerException.Message}", innerException);

    /// <summary>Wraps a JSON library exception raised while deserializing.</summary>
    public static FExJsonException DeserializationFailed(Exception innerException) =>
        new($"JSON deserialization failed: {innerException.Message}", innerException);
}
