using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FEx.Encryption;

/// <summary>
/// Writes and reads a <see cref="SecureNotifyPropertyChanged" /> so that encrypted properties travel as their
/// ciphertext. Applied to the base class, so every derived type gets it without asking.
/// </summary>
/// <remarks>
/// Stateless with respect to the instance: each getter is called normally (it returns plaintext and, as a side
/// effect, records the property's current ciphertext), and the record - not the getter's value - is written.
/// Nothing the getters return depends on whether a serializer is running, so a handled serializer error, a
/// concurrent serialization of the same instance or a throwing callback cannot make a getter leak.
/// <para>
/// It honours what the contract says about each member - <c>[JsonIgnore]</c>, <c>[JsonProperty]</c> names and
/// fields, <c>ShouldSerialize*</c>, member converters, <see cref="NullValueHandling" /> - and runs the type's
/// own serialization callbacks. It does not implement reference preservation or type-name handling.
/// </para>
/// </remarks>
internal sealed class SecureNotifyPropertyChangedConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => typeof(SecureNotifyPropertyChanged).IsAssignableFrom(objectType);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not SecureNotifyPropertyChanged secure)
        {
            writer.WriteNull();

            return;
        }

        var contract = ObjectContract(serializer, value.GetType());
        Invoke(contract.OnSerializingCallbacks, value, serializer.Context);
        writer.WriteStartObject();

        foreach (var property in contract.Properties)
        {
            if (property.Ignored
                || !property.Readable
                || property.ValueProvider is null
                || property.PropertyName is null
                || property.ShouldSerialize?.Invoke(value) == false)
                continue;

            // The getter runs first: it returns plaintext and refreshes the property's recorded ciphertext.
            var memberValue = property.ValueProvider.GetValue(value);
            var isEncrypted = false;

            if (property.UnderlyingName is { } name && secure.TryGetStoredCiphertext(name, out var ciphertext))
            {
                memberValue = ciphertext;
                isEncrypted = true;
            }

            if (memberValue is null && (property.NullValueHandling ?? serializer.NullValueHandling) == NullValueHandling.Ignore)
                continue;

            writer.WritePropertyName(property.PropertyName);

            if (!isEncrypted && property.Converter is { CanWrite: true } converter)
                converter.WriteJson(writer, memberValue, serializer);
            else
                serializer.Serialize(writer, memberValue);
        }

        writer.WriteEndObject();
        Invoke(contract.OnSerializedCallbacks, value, serializer.Context);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var contract = ObjectContract(serializer, objectType);

        var target = existingValue as SecureNotifyPropertyChanged
                     ?? contract.DefaultCreator?.Invoke() as SecureNotifyPropertyChanged
                     ?? throw new JsonSerializationException(
                         $"{objectType.Name} needs a parameterless constructor to be deserialized.");

        var json = JObject.Load(reader);
        Invoke(contract.OnDeserializingCallbacks, target, serializer.Context);

        foreach (var member in json.Properties())
        {
            var property = contract.Properties.GetClosestMatchProperty(member.Name);

            if (property is null
                || property.Ignored
                || !property.Writable
                || property.ValueProvider is not { } valueProvider
                || property.PropertyType is not { } propertyType)
                continue;

            var token = member.Value;

            // An envelope is stored verbatim: the setter's EncryptSource takes it as the ciphertext it already is,
            // whatever the property's own type. A plain value - a legacy file written in the clear - goes through
            // the setter normally and is encrypted there.
            if (token.Type == JTokenType.String
                && property.UnderlyingName is { } name
                && (string?)token is { } text
                && FExStringCipher.IsEnvelope(text))
            {
                var placeholder = propertyType == typeof(string)
                    ? text
                    : propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;

                target.SetFromStoredCiphertext(name, text, () => valueProvider.SetValue(target, placeholder));

                continue;
            }

            var memberValue = property.Converter is { CanRead: true } converter
                ? ReadWithConverter(converter, token, propertyType, serializer)
                : token.ToObject(propertyType, serializer);

            valueProvider.SetValue(target, memberValue);
        }

        Invoke(contract.OnDeserializedCallbacks, target, serializer.Context);

        return target;
    }

    private static object? ReadWithConverter(JsonConverter converter, JToken token, Type type, JsonSerializer serializer)
    {
        using var tokenReader = token.CreateReader();
        tokenReader.Read();

        return converter.ReadJson(tokenReader, type, null, serializer);
    }

    private static JsonObjectContract ObjectContract(JsonSerializer serializer, Type type) =>
        serializer.ContractResolver.ResolveContract(type) as JsonObjectContract
        ?? throw new JsonSerializationException($"{type.Name} does not resolve to an object contract.");

    private static void Invoke(IList<SerializationCallback> callbacks, object target, StreamingContext context)
    {
        foreach (var callback in callbacks)
            callback(target, context);
    }
}
