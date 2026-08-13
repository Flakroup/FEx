using FEx.Agnostics.Abstractions.Logging;
using FEx.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace FEx.Json.Extensions;

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

    public static string ToJson(this object self, JsonSerializerSettings? settings, Formatting formatting) =>
        JsonConvert.SerializeObject(self, formatting, settings ?? DefaultSettings);

    public static string ToJson(this object self) => self.ToJson(null, Formatting.None);

    public static string ToJson(this object self, JsonSerializerSettings? settings) =>
        self.ToJson(settings, Formatting.None);

    public static string ToJson(this object self, Formatting formatting) => self.ToJson(null, formatting);

    public static T? FromJson<T>(this string json, JsonSerializerSettings? settings, T? fallback)
    {
        try
        {
            return json is null || json == NullString
                ? fallback
                : JsonConvert.DeserializeObject<T>(json, settings ?? DefaultSettings);
        }
        catch (Exception ex)
        {
            FExStaticLogger.Error(ex); //todo use ExceptionHandler

            if (Debugger.IsAttached)
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "error.json"), json);

            throw;
        }
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