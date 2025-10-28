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
                                JsonSerializerSettings settings = null,
                                Formatting formatting = Formatting.None) =>
        JsonConvert.SerializeObject(self, formatting, settings ?? DefaultSettings);

    public static T FromJson<T>(this string json, JsonSerializerSettings settings = null, T fallback = default)
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

    public static object FromJson(this string json, JsonSerializerSettings settings = null) =>
        JsonConvert.DeserializeObject(json, settings ?? DefaultSettings);

    public static object DeserializeFromStream(this Stream stream, JsonSerializerSettings settings = null)
    {
        var serializer = JsonSerializer.Create(settings ?? DefaultSettings);

        using var sr = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(sr);

        return serializer.Deserialize(jsonTextReader);
    }

    public static T DeserializeFromStream<T>(this Stream stream, JsonSerializerSettings settings = null)
    {
        var serializer = JsonSerializer.Create(settings ?? DefaultSettings);

        using var sr = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(sr);

        return serializer.Deserialize<T>(jsonTextReader);
    }

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
    public static T DeserializeToken<T>(this JToken jToken, JsonSerializerSettings settings = null) =>
        jToken.ToString().FromJson<T>(settings);

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
                                         JsonLoadSettings loadSettings = null,
                                         JsonSerializerSettings saveSettings = null,
                                         Formatting formatting = Formatting.Indented) =>
        JObject.Parse(json, loadSettings).ToJson(saveSettings, formatting);

    public static T DeserializeFromFile<T>(this FileInfo file, JsonSerializerSettings settings = null)
    {
        using var fStream = file.OpenRead();

        return fStream.DeserializeFromStream<T>(settings);
    }

    public static void SerializeToFile(this FileInfo file,
                                       object self,
                                       JsonSerializerSettings settings = null,
                                       Formatting formatting = Formatting.None) =>
        File.WriteAllText(file.FullName, self.ToJson(settings, formatting));
}