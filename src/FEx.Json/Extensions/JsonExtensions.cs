using FEx.Agnostics.Abstractions.Logging;
using FEx.Json.Abstractions;
using FEx.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace FEx.Json.Extensions;

/// <summary>
/// Newtonsoft.Json helpers: <c>ToJson</c>, <c>FromJson</c> and the <see cref="JToken" />, stream and file helpers
/// built on them. They are the Newtonsoft.Json path by design: they always serialize with Newtonsoft.Json and the
/// process-wide <see cref="JsonConvert.DefaultSettings" />, whichever <see cref="IFExJsonSerializer" /> module an
/// application registers, and their <see cref="JsonSerializerSettings" /> and <see cref="Formatting" /> parameters
/// are Newtonsoft types. FEx.SecureStorage, FEx.Encryption and <c>SecureNotifyPropertyChanged</c> rely on that and
/// stay on them on purpose. Library-neutral code takes <see cref="IFExJsonSerializer" /> instead.
/// </summary>
public static class JsonExtensions
{
    public static string NullString { get; } = "\"null\"";

    public static JsonSerializerSettings? DefaultSettings => JsonConvert.DefaultSettings?.Invoke();

    private static JsonSerializerSettings DefaultSettingsInstance { get; set; }

    static JsonExtensions()
    {
        DefaultSettingsInstance = new()
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        ((List<JsonConverter>)DefaultSettingsInstance.Converters).AddRange([
            ParseStringToDoubleConverter.Singleton, new VersionConverter()
        ]);

        JsonConvert.DefaultSettings = () => DefaultSettingsInstance;
    }

    public static void Initialize()
    {
    }

    public static void ConfigureDefaultSettings(Action<JsonSerializerSettings> configuration)
    {
        // DefaultSettings is backed by DefaultSettingsInstance via the static ctor lambda,
        // so it is non-null in practice; fall back to the current instance defensively.
        DefaultSettingsInstance = DefaultSettings ?? DefaultSettingsInstance;
        configuration(DefaultSettingsInstance);
    }

    /// <summary>
    /// Serializes <paramref name="self" /> with Newtonsoft.Json, never through <see cref="IFExJsonSerializer" />.
    /// </summary>
    public static string ToJson(this object self, JsonSerializerSettings? settings, Formatting formatting) =>
        JsonConvert.SerializeObject(self, formatting, settings ?? DefaultSettings);

    public static string ToJson(this object self) => self.ToJson(null, Formatting.None);

    public static string ToJson(this object self, JsonSerializerSettings? settings) =>
        self.ToJson(settings, Formatting.None);

    public static string ToJson(this object self, Formatting formatting) => self.ToJson(null, formatting);

    /// <summary>
    /// Deserializes <paramref name="json" /> with Newtonsoft.Json, never through <see cref="IFExJsonSerializer" />.
    /// </summary>
    public static T? FromJson<T>(this string json, JsonSerializerSettings? settings, T? fallback)
    {
        var effectiveSettings = settings ?? DefaultSettings;

        try
        {
            return json is null || json == NullString
                ? fallback
                : JsonConvert.DeserializeObject<T>(json, effectiveSettings);
        }
        catch (Exception ex)
        {
            // Newtonsoft (and any converter) can quote the offending value in the exception message, and
            // FEx.SecureStorage deserializes decrypted secrets here, so neither the exception nor the input is
            // logged - only the target type, the redacted JSON path and the exception type. The caller gets the
            // exception.
            FExStaticLogger.Error(
                $"Could not deserialize JSON into {typeof(T).FullName} at path '{ex.GetJsonPath(typeof(T), effectiveSettings) ?? "unknown"}' ({ex.GetType().FullName}).");

            throw;
        }
    }

    /// <summary>
    /// The JSON path a Newtonsoft.Json reader or serialization exception points at, with its data redacted so it is
    /// safe to log, unlike the exception message, which can quote the offending value. The path is walked against
    /// the contracts of <paramref name="targetType" />: dictionary keys become <c>[*]</c>, array indices and declared
    /// property names stay, and from the first segment the contracts cannot vouch for (a <c>JToken</c> or
    /// <c>object</c> member, a converter, an unknown name) every remaining segment becomes <c>[*]</c>. A converter
    /// counts whether it comes from an attribute, from the settings or from Newtonsoft.Json's built-in ones (DataTable,
    /// DataSet, XmlNode, ...): the walk uses the contract resolver and the converters of the serializer
    /// <see cref="JsonConvert" /> builds for <paramref name="settings" />.
    /// </summary>
    /// <param name="exception">The exception the deserialization threw.</param>
    /// <param name="targetType">The type that was being deserialized.</param>
    /// <param name="settings">
    /// The settings the deserialization used; null means <see cref="JsonSerializer.CreateDefault()" />, which applies
    /// <see cref="JsonConvert.DefaultSettings" />.
    /// </param>
    /// <returns>
    /// The redacted path (empty when Newtonsoft.Json recorded none), or null when <paramref name="exception" /> is
    /// neither a <see cref="JsonReaderException" /> nor a <see cref="JsonSerializationException" />.
    /// </returns>
    public static string? GetJsonPath(this Exception exception,
                                      Type targetType,
                                      JsonSerializerSettings? settings = null)
    {
        var path = exception switch
        {
            JsonReaderException readerException => readerException.Path ?? string.Empty,
            JsonSerializationException serializationException => serializationException.Path ?? string.Empty,
            _ => null
        };

        return path is null
            ? null
            : JsonPathRedaction.Redact(path, targetType, settings);
    }

    public static T? FromJson<T>(this string json) => json.FromJson<T>(null, default);

    public static T? FromJson<T>(this string json, JsonSerializerSettings? settings) =>
        json.FromJson<T>(settings, default);

    public static object? FromJson(this string json, JsonSerializerSettings? settings) =>
        // DefaultSettings is backed by the static ctor and non-null; this overload needs non-null settings.
        JsonConvert.DeserializeObject(json, settings ?? DefaultSettings!);

    public static object? FromJson(this string json) => json.FromJson(null);

    public static object? DeserializeFromStream(this Stream stream, JsonSerializerSettings? settings)
    {
        var serializer = JsonSerializer.Create(settings ?? DefaultSettings);

        using var sr = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(sr);

        return serializer.Deserialize(jsonTextReader);
    }

    public static object? DeserializeFromStream(this Stream stream) => stream.DeserializeFromStream(null);

    public static T? DeserializeFromStream<T>(this Stream stream, JsonSerializerSettings? settings)
    {
        var serializer = JsonSerializer.Create(settings ?? DefaultSettings);

        using var sr = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(sr);

        return serializer.Deserialize<T>(jsonTextReader);
    }

    public static T? DeserializeFromStream<T>(this Stream stream) => stream.DeserializeFromStream<T>(null);

    /// <summary>
    /// Reformats the json.
    /// </summary>
    /// <param name="json">The json.</param>
    /// <returns>
    /// System.String
    /// </returns>
    public static string ReformatJson(this string json)
    {
        var obj = JsonConvert.DeserializeObject(json);

        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }

    /// <summary>
    /// Deserializes the token.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="jToken">The j token.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>
    /// T
    /// </returns>
    public static T? DeserializeToken<T>(this JToken jToken, JsonSerializerSettings? settings) =>
        jToken.ToString().FromJson<T>(settings);

    public static T? DeserializeToken<T>(this JToken jToken) => jToken.DeserializeToken<T>(null);

    [return: NotNullIfNotNull(nameof(jsonValue))]
    public static string? TrimJsonString(this string? jsonValue)
    {
        jsonValue = jsonValue?.Trim();

        if (jsonValue?.StartsWith("\"{") ?? false)
            jsonValue = Regex.Unescape(jsonValue).Trim('"');

        return jsonValue;
    }

    public static void PrettyPrintFile(string orgPath, string destPath)
    {
        using var file = File.OpenText(orgPath);
        using var reader = new JsonTextReader(file);
        using var destFile = File.OpenWrite(destPath);
        destFile.SetLength(0);
        using var destFileWriter = new StreamWriter(destFile);
        using var destWriter = new JsonTextWriter(destFileWriter);
        destWriter.Formatting = Formatting.Indented;
        var obj = (JObject)JToken.ReadFrom(reader);
        obj.WriteTo(destWriter);
    }

    public static string PrettyPrintJson(this string json,
                                         JsonLoadSettings? loadSettings,
                                         JsonSerializerSettings? saveSettings,
                                         Formatting formatting)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            return JToken.Parse(json, loadSettings).ToJson(saveSettings, formatting);
        }
        catch (JsonException)
        {
            // Content is not valid JSON (e.g. a plain-text error body like "Service Unavailable"
            // or a bare string). Pretty-printing must be total - return the content unchanged.
            return json;
        }
    }

    public static string PrettyPrintJson(this string json) => json.PrettyPrintJson(null, null, Formatting.Indented);

    public static string PrettyPrintJson(this string json, JsonLoadSettings loadSettings) =>
        json.PrettyPrintJson(loadSettings, null, Formatting.Indented);

    public static string PrettyPrintJson(this string json,
                                         JsonLoadSettings loadSettings,
                                         JsonSerializerSettings saveSettings) =>
        json.PrettyPrintJson(loadSettings, saveSettings, Formatting.Indented);

    public static T? DeserializeFromFile<T>(this FileInfo file, JsonSerializerSettings? settings)
    {
        using var fStream = file.OpenRead();

        return fStream.DeserializeFromStream<T>(settings);
    }

    public static T? DeserializeFromFile<T>(this FileInfo file) => file.DeserializeFromFile<T>(null);

    public static void SerializeToFile(this FileInfo file,
                                       object self,
                                       JsonSerializerSettings? settings,
                                       Formatting formatting) =>
        File.WriteAllText(file.FullName, self.ToJson(settings, formatting));

    public static void SerializeToFile(this FileInfo file, object self) =>
        file.SerializeToFile(self, null, Formatting.None);

    public static void SerializeToFile(this FileInfo file, object self, JsonSerializerSettings settings) =>
        file.SerializeToFile(self, settings, Formatting.None);
}