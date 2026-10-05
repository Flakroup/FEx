using FEx.Json.Abstractions;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Json;

/// <summary>
/// <see cref="IFExJsonSerializer" /> on top of Newtonsoft.Json and the given <see cref="JsonSerializerSettings" />
/// (<see cref="FExJsonModule" /> passes the FEx defaults with <see cref="Resolvers.DIContractResolver" />).
/// Newtonsoft.Json has no asynchronous reader, so the stream overloads buffer the payload in memory.
/// </summary>
public sealed class FExNewtonsoftJsonSerializer : IFExJsonSerializer
{
    private const int CopyBufferSize = 81920;

    private static readonly UTF8Encoding Utf8 = new(false);

    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private readonly JsonSerializerSettings _settings;

    public FExNewtonsoftJsonSerializer(JsonSerializerSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public string Serialize<T>(T value) => Serialize(value, typeof(T));

    public string Serialize(object? value, Type inputType)
    {
        FExJsonGuard.EnsureAssignable(value, inputType);

        try
        {
            return JsonConvert.SerializeObject(value, inputType, _settings);
        }
        catch (JsonException ex)
        {
            throw FExJsonException.SerializationFailed(ex);
        }
    }

    public T? Deserialize<T>(string json)
    {
        FExJsonGuard.EnsurePayload(json);

        try
        {
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }
        catch (JsonException ex)
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
            return JsonConvert.DeserializeObject(json, returnType, _settings);
        }
        catch (JsonException ex)
        {
            throw FExJsonException.DeserializationFailed(ex);
        }
    }

    public Task SerializeAsync<T>(Stream utf8Json, T value, CancellationToken cancellationToken = default) =>
        SerializeAsync(utf8Json, value, typeof(T), cancellationToken);

    public async Task SerializeAsync(Stream utf8Json, object? value, Type inputType,
                                     CancellationToken cancellationToken = default)
    {
        if (utf8Json is null)
            throw new ArgumentNullException(nameof(utf8Json));

        var bytes = Utf8.GetBytes(Serialize(value, inputType));
        await utf8Json.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        await utf8Json.FlushAsync(cancellationToken);
    }

    public async Task<T?> DeserializeAsync<T>(Stream utf8Json, CancellationToken cancellationToken = default) =>
        Deserialize<T>(await ReadToEndAsync(utf8Json, cancellationToken));

    public async Task<object?> DeserializeAsync(Stream utf8Json, Type returnType,
                                                CancellationToken cancellationToken = default)
    {
        if (returnType is null)
            throw new ArgumentNullException(nameof(returnType));

        return Deserialize(await ReadToEndAsync(utf8Json, cancellationToken), returnType);
    }

    private static async Task<string> ReadToEndAsync(Stream utf8Json, CancellationToken cancellationToken)
    {
        if (utf8Json is null)
            throw new ArgumentNullException(nameof(utf8Json));

        using var buffer = new MemoryStream();
        await utf8Json.CopyToAsync(buffer, CopyBufferSize, cancellationToken);

        var bytes = buffer.GetBuffer();
        var length = (int)buffer.Length;
        var start = HasUtf8Bom(bytes, length) ? Utf8Bom.Length : 0;

        return Utf8.GetString(bytes, start, length - start);
    }

    // A leading UTF-8 byte order mark is skipped, as System.Text.Json does.
    private static bool HasUtf8Bom(byte[] bytes, int length) =>
        length >= Utf8Bom.Length && bytes[0] == Utf8Bom[0] && bytes[1] == Utf8Bom[1] && bytes[2] == Utf8Bom[2];
}
