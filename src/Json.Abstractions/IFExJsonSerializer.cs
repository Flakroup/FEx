using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Json.Abstractions;

/// <summary>
/// A JSON serializer that does not tie its caller to a JSON library. FEx ships two interchangeable implementations:
/// <c>FExNewtonsoftJsonSerializer</c> in FEx.Json (Newtonsoft.Json) and <c>FExSystemTextJsonSerializer</c> in
/// FEx.Json.SystemTextJsonx (System.Text.Json, trimming and Native AOT friendly).
/// </summary>
/// <remarks>
/// <para>The contract every implementation honours:</para>
/// <list type="bullet">
///   <item>Text is UTF-8; streams are read and written as UTF-8 without a byte order mark and are left open.</item>
///   <item>A <see langword="null" /> value serializes to <c>null</c>, and the payload <c>null</c> deserializes to
///   <see langword="null" /> (or throws <see cref="FExJsonException" /> for a non-nullable value type).</item>
///   <item>A <see langword="null" /> string, stream or type throws <see cref="ArgumentNullException" />. A value that is
///   not an instance of the given input type (a <see langword="null" /> value for a non-nullable value type included),
///   and a stream that cannot be read or written, throw <see cref="ArgumentException" />; they are checked before the
///   library runs, so an exception the caller's stream raises later is not wrapped either.</item>
///   <item>An empty or whitespace-only payload, a payload that is not well-formed JSON or not valid UTF-8, a payload with
///   content after the top-level value, and a payload that cannot be bound to the requested type (<c>null</c> for a
///   non-nullable value type included) throw <see cref="FExJsonException" />. So does a type the implementation cannot
///   (de)serialize with its configuration. The library's own exception, when it raised one, is the
///   <see cref="Exception.InnerException" />, so callers never catch a library type. The message carries the library's
///   message, which for Newtonsoft.Json can quote payload values: do not log it where the payload is sensitive.</item>
///   <item>Cancellation throws <see cref="OperationCanceledException" />, which is not wrapped.</item>
/// </list>
/// <para>What stays implementation-defined, because the two libraries differ and are configured through their own
/// settings: property naming and casing, grammar leniency beyond RFC 8259 (comments, single quotes, trailing commas),
/// and whether members only present on the runtime type of a value are written. Text produced by one implementation
/// round-trips through the same implementation; it is not promised to be byte-identical across implementations.</para>
/// </remarks>
public interface IFExJsonSerializer
{
    /// <summary>Serializes <paramref name="value" /> as <typeparamref name="T" /> to a JSON string.</summary>
    /// <exception cref="FExJsonException">The value cannot be serialized.</exception>
    string Serialize<T>(T value);

    /// <summary>Serializes <paramref name="value" /> as <paramref name="inputType" /> to a JSON string.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="inputType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="value" /> is not an instance of <paramref name="inputType" />.</exception>
    /// <exception cref="FExJsonException">The value cannot be serialized.</exception>
    string Serialize(object? value, Type inputType);

    /// <summary>Deserializes <paramref name="json" /> to <typeparamref name="T" />.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="json" /> is <see langword="null" />.</exception>
    /// <exception cref="FExJsonException">The payload is empty, malformed or cannot be bound to <typeparamref name="T" />.</exception>
    T? Deserialize<T>(string json);

    /// <summary>Deserializes <paramref name="json" /> to <paramref name="returnType" />.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="json" /> or <paramref name="returnType" /> is <see langword="null" />.</exception>
    /// <exception cref="FExJsonException">The payload is empty, malformed or cannot be bound to <paramref name="returnType" />.</exception>
    object? Deserialize(string json, Type returnType);

    /// <summary>Serializes <paramref name="value" /> as <typeparamref name="T" /> to <paramref name="utf8Json" />.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="utf8Json" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="utf8Json" /> cannot be written.</exception>
    /// <exception cref="FExJsonException">The value cannot be serialized.</exception>
    Task SerializeAsync<T>(Stream utf8Json, T value, CancellationToken cancellationToken = default);

    /// <summary>Serializes <paramref name="value" /> as <paramref name="inputType" /> to <paramref name="utf8Json" />.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="utf8Json" /> or <paramref name="inputType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="utf8Json" /> cannot be written, or <paramref name="value" /> is not an instance of <paramref name="inputType" />.</exception>
    /// <exception cref="FExJsonException">The value cannot be serialized.</exception>
    Task SerializeAsync(Stream utf8Json, object? value, Type inputType, CancellationToken cancellationToken = default);

    /// <summary>Deserializes the JSON in <paramref name="utf8Json" /> to <typeparamref name="T" />.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="utf8Json" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="utf8Json" /> cannot be read.</exception>
    /// <exception cref="FExJsonException">The payload is empty, malformed or cannot be bound to <typeparamref name="T" />.</exception>
    Task<T?> DeserializeAsync<T>(Stream utf8Json, CancellationToken cancellationToken = default);

    /// <summary>Deserializes the JSON in <paramref name="utf8Json" /> to <paramref name="returnType" />.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="utf8Json" /> or <paramref name="returnType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="utf8Json" /> cannot be read.</exception>
    /// <exception cref="FExJsonException">The payload is empty, malformed or cannot be bound to <paramref name="returnType" />.</exception>
    Task<object?> DeserializeAsync(Stream utf8Json, Type returnType, CancellationToken cancellationToken = default);
}
