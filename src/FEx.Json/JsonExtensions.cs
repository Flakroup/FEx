using FEx.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace FEx.Json;

public static class JsonExtensions
{
    private static JsonSerializerSettings _defaultSettings;
    public static string NullString { get; } = "\"null\"";

    public static JsonSerializerSettings DefaultSettings
    {
        get
        {
            if (_defaultSettings == null)
            {
                _defaultSettings = new()
                {
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                    DateParseHandling = DateParseHandling.None,
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                };
                ((List<JsonConverter>)_defaultSettings.Converters).AddRange(new JsonConverter[] { ParseStringConverter.Singleton, new VersionConverter() });
            }

            return _defaultSettings;
        }
        set => _defaultSettings = value;
    }

    public static void ConfigureDefaultSettings(Action<JsonSerializerSettings> configuration)
    {
        configuration(DefaultSettings);
    }

    public static string ToJson(this object self, JsonSerializerSettings settings = null, Formatting formatting = Formatting.None)
    {
        return JsonConvert.SerializeObject(self, formatting, settings ?? DefaultSettings);
    }

    public static T FromJson<T>(this string json, JsonSerializerSettings settings = null, T fallback = default)
    {
        try
        {
            if (json == NullString)
                return fallback;

            return JsonConvert.DeserializeObject<T>(json, settings ?? DefaultSettings);
        }
        catch // (Exception ex)
        {
            if (Debugger.IsAttached)
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "error.json"), json);

            throw;
        }
    }

    public static object FromJson(this string json, JsonSerializerSettings settings = null)
    {
        return JsonConvert.DeserializeObject(json, settings ?? DefaultSettings);
    }

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
    ///     Reformats the json.
    /// </summary>
    /// <param name="json">The json.</param>
    /// <returns>
    ///     System.String
    /// </returns>
    public static string ReformatJson(this string json)
    {
        object obj = JsonConvert.DeserializeObject(json);
        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }

    /// <summary>
    ///     Deserializes the token.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="jToken">The j token.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>
    ///     T
    /// </returns>
    public static T DeserializeToken<T>(this JToken jToken, JsonSerializerSettings settings = null)
    {
        return jToken.ToString()
            .FromJson<T>(settings);
    }

    public static string TrimJsonString(this string jsonValue)
    {
        jsonValue = jsonValue?.Trim();

        if (jsonValue?.StartsWith("\"{") ?? false)
            jsonValue = Regex.Unescape(jsonValue)
                .Trim('"');

        return jsonValue;
    }

    public static void PrettyPrintFile(string orgPath, string destPath)
    {
        using StreamReader file = File.OpenText(orgPath);
        using var reader = new JsonTextReader(file);
        using FileStream destFile = File.OpenWrite(destPath);
        destFile.SetLength(0);
        using var destFileWriter = new StreamWriter(destFile);
        using var destWriter = new JsonTextWriter(destFileWriter);
        destWriter.Formatting = Formatting.Indented;
        var obj = (JObject)JToken.ReadFrom(reader);
        obj.WriteTo(destWriter);
    }

    public static string PrettyPrintJson(this string json, JsonLoadSettings loadSettings = null, JsonSerializerSettings saveSettings = null, Formatting formatting = Formatting.Indented)
    {
        return JObject.Parse(json, loadSettings)
            .ToJson(saveSettings ?? DefaultSettings, formatting);
    }

    public static T DeserializeFromFile<T>(this FileInfo file, JsonSerializerSettings settings = null)
    {
        FileStream fStream = file.OpenRead();

        try
        {
            return fStream.DeserializeFromStream<T>(settings);
        }
        finally
        {
            fStream.Dispose();
        }
    }

    public static void SerializeToFile(this FileInfo file, object self, JsonSerializerSettings settings = null, Formatting formatting = Formatting.None)
    {
        File.WriteAllText(file.FullName, self.ToJson(settings, formatting));
    }
}