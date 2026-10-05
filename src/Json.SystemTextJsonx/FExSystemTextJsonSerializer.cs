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
/// <see cref="JsonSerializerOptions.GetTypeInfo" />, so it is trimming and Native AOT safe whenever the options carry a
/// source-generated resolver; with no resolver the reflection one is used where the application allows it.
/// </summary>
public sealed class FExSystemTextJsonSerializer : IFExJsonSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <param name="options">The options to serialize with; a source-generated <c>JsonSerializerContext</c> is passed
    /// as its <c>Options</c> or as the <see cref="JsonSerializerOptions.TypeInfoResolver" /> of the options.</param>
    public FExSystemTextJsonSerializer(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
        if (utf8Json is null)
            throw new ArgumentNullException(nameof(utf8Json));

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
        if (utf8Json is null)
            throw new ArgumentNullException(nameof(utf8Json));

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
        if (utf8Json is null)
            throw new ArgumentNullException(nameof(utf8Json));

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
        if (utf8Json is null)
            throw new ArgumentNullException(nameof(utf8Json));

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
    /// The exceptions System.Text.Json documents for a payload or type it cannot handle: <see cref="JsonException" />
    /// (malformed or unbindable JSON) and <see cref="NotSupportedException" /> (no converter or metadata for a type).
    /// </summary>
    private static bool IsLibraryFailure(Exception ex) => ex is JsonException or NotSupportedException;

    private JsonTypeInfo<T> GetTypeInfo<T>() => (JsonTypeInfo<T>)_options.GetTypeInfo(typeof(T));
}
