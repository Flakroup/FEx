using FEx.Agnostics.Abstractions.Logging;
using FEx.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace FEx.Json.Extensions;

public static class JsonExtensions
{
    public static string NullString { get; } = "\"null\"";

    public static JsonSerializerSettings DefaultSettings => JsonConvert.DefaultSettings?.Invoke();

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
        DefaultSettingsInstance = DefaultSettings;
        configuration(DefaultSettingsInstance);
    }

    public static string ToJson(this object self,
                                JsonSerializerSettings settings,
                                Formatting formatting) =>
        JsonConvert.SerializeObject(self, formatting, settings ?? DefaultSettings);

    public static string ToJson(this object self) =>
        ToJson(self, null, Formatting.None);

    public static string ToJson(this object self, JsonSerializerSettings settings) =>
        ToJson(self, settings, Formatting.None);

    public static string ToJson(this object self, Formatting formatting) =>
        ToJson(self, null, formatting);

    public static T FromJson<T>(this string json, JsonSerializerSettings settings, T fallback)
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

    public static T FromJson<T>(this string json) =>
        FromJson<T>(json, null, default);

    public static T FromJson<T>(this string json, JsonSerializerSettings settings) =>
        FromJson<T>(json, settings, default);

    public static object FromJson(this string json, JsonSerializerSettings settings) =>
        JsonConvert.DeserializeObject(json, settings ?? DefaultSettings);

    public static object FromJson(this string json) =>
        FromJson(json, (JsonSerializerSettings)null);

    public static object DeserializeFromStream(this Stream stream, JsonSerializerSettings settings)
    {
        var serializer = JsonSerializer.Create(settings ?? DefaultSettings);

        using var sr = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(sr);

        return serializer.Deserialize(jsonTextReader);
    }

    public static object DeserializeFromStream(this Stream stream) =>
        DeserializeFromStream(stream, null);

    public static T DeserializeFromStream<T>(this Stream stream, JsonSerializerSettings settings)
    {
        var serializer = JsonSerializer.Create(settings ?? DefaultSettings);

        using var sr = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(sr);

        return serializer.Deserialize<T>(jsonTextReader);
    }

    public static T DeserializeFromStream<T>(this Stream stream) =>
        DeserializeFromStream<T>(stream, null);

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
    public static T DeserializeToken<T>(this JToken jToken, JsonSerializerSettings settings) =>
        jToken.ToString().FromJson<T>(settings);

    public static T DeserializeToken<T>(this JToken jToken) =>
        DeserializeToken<T>(jToken, null);

    public static string TrimJsonString(this string jsonValue)
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
                                         JsonLoadSettings loadSettings,
                                         JsonSerializerSettings saveSettings,
                                         Formatting formatting) =>
        JObject.Parse(json, loadSettings).ToJson(saveSettings, formatting);

    public static string PrettyPrintJson(this string json) =>
        PrettyPrintJson(json, null, null, Formatting.Indented);

    public static string PrettyPrintJson(this string json, JsonLoadSettings loadSettings) =>
        PrettyPrintJson(json, loadSettings, null, Formatting.Indented);

    public static string PrettyPrintJson(this string json,
                                         JsonLoadSettings loadSettings,
                                         JsonSerializerSettings saveSettings) =>
        PrettyPrintJson(json, loadSettings, saveSettings, Formatting.Indented);

    public static T DeserializeFromFile<T>(this FileInfo file, JsonSerializerSettings settings)
    {
        using var fStream = file.OpenRead();

        return fStream.DeserializeFromStream<T>(settings);
    }

    public static T DeserializeFromFile<T>(this FileInfo file) =>
        DeserializeFromFile<T>(file, null);

    public static void SerializeToFile(this FileInfo file,
                                       object self,
                                       JsonSerializerSettings settings,
                                       Formatting formatting) =>
        File.WriteAllText(file.FullName, self.ToJson(settings, formatting));

    public static void SerializeToFile(this FileInfo file, object self) =>
        SerializeToFile(file, self, null, Formatting.None);

    public static void SerializeToFile(this FileInfo file, object self, JsonSerializerSettings settings) =>
        SerializeToFile(file, self, settings, Formatting.None);
}