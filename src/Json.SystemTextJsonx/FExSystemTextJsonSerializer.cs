using FEx.Json.Abstractions;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Json.SystemTextJsonx;

/// <summary>
/// <see cref="IFExJsonSerializer" /> on top of System.Text.Json. Every call goes through
/// <see cref="JsonSerializerOptions.GetTypeInfo" />, so it needs no reflection of its own: with a source-generated
/// resolver it is trimming and Native AOT safe. The options must carry a
/// <see cref="JsonSerializerOptions.TypeInfoResolver" />; System.Text.Json adds no reflection fallback on this path.
/// </summary>
public sealed class FExSystemTextJsonSerializer : IFExJsonSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <param name="options">The options to serialize with; a source-generated <c>JsonSerializerContext</c> is passed
    /// as its <c>Options</c> or as the <see cref="JsonSerializerOptions.TypeInfoResolver" /> of the options.</param>
    /// <exception cref="ArgumentException"><paramref name="options" /> has no <see cref="JsonSerializerOptions.TypeInfoResolver" />.</exception>
    public FExSystemTextJsonSerializer(JsonSerializerOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        // Without a resolver every call would fail; fail here, where the misconfiguration is.
        if (options.TypeInfoResolver is null)
            throw new ArgumentException(
                "The options have no TypeInfoResolver: use FExSystemTextJsonOptions.CreateDefault, or a source-generated JsonSerializerContext.",
                nameof(options));

        _options = options;
    }

    public string Serialize<T>(T value)
    {
        try
        {
            return JsonSerializer.Serialize(value, GetTypeInfo<T>());
        }
        catch (Exception ex) when (IsLibraryFailure(ex))
        {
            throw FExJsonException.SerializationFailed(ex);
        }
    }

    public string Serialize(object? value, Type inputType)
    {
        FExJsonGuard.EnsureAssignable(value, inputType);

        try
        {
            return JsonSerializer.Serialize(value, _options.GetTypeInfo(inputType));
        }
        catch (Exception ex) when (IsLibraryFailure(ex))
        {
            throw FExJsonException.SerializationFailed(ex);
        }
    }

    public T? Deserialize<T>(string json)
    {
        FExJsonGuard.EnsurePayload(json);

        try
        {
            return JsonSerializer.Deserialize(json, GetTypeInfo<T>());
        }
        catch (Exception ex) when (IsLibraryFailure(ex))
        {
            throw FExJsonException.DeserializationFailed(ex);
        }
    }

    public object? Deserialize(string json, Type returnType)
    {
        if (returnType is null)
            throw new ArgumentNullException(nameof(returnType));

        FExJsonGuard.EnsurePayload(json);

        try
        {
            return JsonSerializer.Deserialize(json, _options.GetTypeInfo(returnType));
        }
        catch (Exception ex) when (IsLibraryFailure(ex))
        {
            throw FExJsonException.DeserializationFailed(ex);
        }
    }

    public async Task SerializeAsync<T>(Stream utf8Json, T value, CancellationToken cancellationToken = default)
    {
        FExJsonGuard.EnsureWritable(utf8Json);

        try
        {
            await JsonSerializer.SerializeAsync(utf8Json, value, GetTypeInfo<T>(), cancellationToken);
        }
        catch (Exception ex) when (IsLibraryFailure(ex))
        {
            throw FExJsonException.SerializationFailed(ex);
        }
    }

    public async Task SerializeAsync(Stream utf8Json, object? value, Type inputType,
                                     CancellationToken cancellationToken = default)
    {
        FExJsonGuard.EnsureWritable(utf8Json);
        FExJsonGuard.EnsureAssignable(value, inputType);

        try
        {
            await JsonSerializer.SerializeAsync(utf8Json, value, _options.GetTypeInfo(inputType), cancellationToken);
        }
        catch (Exception ex) when (IsLibraryFailure(ex))
        {
            throw FExJsonException.SerializationFailed(ex);
        }
    }

    public async Task<T?> DeserializeAsync<T>(Stream utf8Json, CancellationToken cancellationToken = default)
    {
        FExJsonGuard.EnsureReadable(utf8Json);

        try
        {
            return await JsonSerializer.DeserializeAsync(utf8Json, GetTypeInfo<T>(), cancellationToken);
        }
        catch (Exception ex) when (IsLibraryFailure(ex))
        {
            throw FExJsonException.DeserializationFailed(ex);
        }
    }

    public async Task<object?> DeserializeAsync(Stream utf8Json, Type returnType,
                                                CancellationToken cancellationToken = default)
    {
        FExJsonGuard.EnsureReadable(utf8Json);

        if (returnType is null)
            throw new ArgumentNullException(nameof(returnType));

        try
        {
            return await JsonSerializer.DeserializeAsync(utf8Json, _options.GetTypeInfo(returnType), cancellationToken);
        }
        catch (Exception ex) when (IsLibraryFailure(ex))
        {
            throw FExJsonException.DeserializationFailed(ex);
        }
    }

    /// <summary>
    /// What System.Text.Json throws for a payload or type it cannot handle: <see cref="JsonException" /> (malformed or
    /// unbindable JSON), <see cref="NotSupportedException" /> (no converter or metadata for a type),
    /// <see cref="InvalidOperationException" /> (an invalid type contract, such as colliding property names) and
    /// <see cref="ArgumentException" /> (a string that is not valid UTF-16). The arguments and the stream's abilities are
    /// checked before the library runs, so none of these comes from the caller's own misuse;
    /// <see cref="ObjectDisposedException" /> (a disposed stream) is the caller's and stays unwrapped.
    /// </summary>
    private static bool IsLibraryFailure(Exception ex) =>
        ex is JsonException or NotSupportedException or ArgumentException
        || ex is InvalidOperationException and not ObjectDisposedException;

    private JsonTypeInfo<T> GetTypeInfo<T>() => (JsonTypeInfo<T>)_options.GetTypeInfo(typeof(T));
}
