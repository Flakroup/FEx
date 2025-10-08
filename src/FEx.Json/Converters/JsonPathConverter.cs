using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;

namespace FEx.Json.Converters;

public class JsonPathConverter : JsonConverter
{
    public override bool CanWrite => false;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        object targetObj = Activator.CreateInstance(objectType);

        foreach (PropertyInfo prop in objectType.GetProperties().Where(p => p.CanRead && p.CanWrite))
        {
            JsonPropertyAttribute att = prop.GetCustomAttributes(true).OfType<JsonPropertyAttribute>().FirstOrDefault();

            string jsonPath = att is not null
                ? att.PropertyName
                : prop.Name;

            JToken token = jo.SelectToken(jsonPath);

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

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
        throw new NotImplementedException();
}