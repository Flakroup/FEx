using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace FEx.Json.Converters;

public class JsonPathConverter : JsonConverter
{
    public override bool CanWrite => false;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        var targetObj = Activator.CreateInstance(objectType);

        foreach (var prop in objectType.GetProperties().Where(p => p.CanRead && p.CanWrite))
        {
            var att = prop.GetCustomAttributes(true).OfType<JsonPropertyAttribute>().FirstOrDefault();

            var jsonPath = att?.PropertyName ?? prop.Name;

            var token = jo.SelectToken(jsonPath);

            if (token is not null
                && token.Type != JTokenType.Null)
            {
                var value = token.ToObject(prop.PropertyType, serializer);
                prop.SetValue(targetObj, value, null);
            }
        }

        return targetObj;
    }

    public override bool CanConvert(Type objectType) =>
        // CanConvert is not called when [JsonConverter] attribute is used
        false;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
        throw new NotImplementedException();
}