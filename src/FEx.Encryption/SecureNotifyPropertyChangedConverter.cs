using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace FEx.Encryption;

/// <summary>
/// Makes Newtonsoft.Json persist the ciphertext of a <see cref="SecureNotifyPropertyChanged" />'s encrypted
/// properties. Applied to the base class, so every derived type gets it without asking.
/// </summary>
/// <remarks>
/// It does not serialize anything itself. The first time a derived type reaches it, it rewires that type's
/// cached contract - each member's value provider is wrapped (see
/// <see cref="SecureNotifyPropertyChanged.GetPersistedValue" />), each reference-typed member reads an envelope
/// string as a ciphertext token - and then takes itself off the contract, so this call and every later one run
/// through Newtonsoft.Json's own object handling: member order, ignore rules, null and reference handling, and
/// per-member error handling (a handled error skips that member, never the whole object).
/// </remarks>
internal sealed class SecureNotifyPropertyChangedConverter : JsonConverter
{
    private static readonly object InstallLock = new();

    public override bool CanConvert(Type objectType) => typeof(SecureNotifyPropertyChanged).IsAssignableFrom(objectType);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();

            return;
        }

        Install(serializer, value.GetType());
        serializer.Serialize(writer, value);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        Install(serializer, objectType);

        if (existingValue is null)
            return serializer.Deserialize(reader, objectType);

        serializer.Populate(reader, existingValue);

        return existingValue;
    }

    private static void Install(JsonSerializer serializer, Type type)
    {
        if (serializer.ContractResolver.ResolveContract(type) is not JsonObjectContract contract)
            throw new JsonSerializationException($"{type.Name} does not resolve to an object contract.");

        lock (InstallLock)
        {
            // Already installed (or replaced by the host - documented as a bypass on the base class).
            if (contract.Converter is not SecureNotifyPropertyChangedConverter)
                return;

            foreach (var property in contract.Properties)
            {
                if (property.ValueProvider is { } provider and not SecureValueProvider)
                    property.ValueProvider = new SecureValueProvider(provider, property.UnderlyingName);

                if (property.PropertyType is { IsValueType: false } propertyType
                    && propertyType != typeof(string)
                    && propertyType != typeof(object)
                    && property.Converter is not EnvelopeReadingConverter)
                    property.Converter = new EnvelopeReadingConverter(property.Converter);
            }

            // Only now, with every member wrapped, does the default object path become reachable for this type.
            contract.Converter = null;
        }
    }

    private sealed class SecureValueProvider : IValueProvider
    {
        private readonly IValueProvider _inner;
        private readonly string? _name;

        public SecureValueProvider(IValueProvider inner, string? name)
        {
            _inner = inner;
            _name = name;
        }

        public object? GetValue(object target) =>
            target is SecureNotifyPropertyChanged secure
                ? secure.GetPersistedValue(() => _inner.GetValue(target), _name)
                : _inner.GetValue(target);

        public void SetValue(object target, object? value)
        {
            if (target is SecureNotifyPropertyChanged secure)
            {
                if (value is CiphertextToken token)
                {
                    secure.SetFromStoredCiphertext(token.Ciphertext, () => _inner.SetValue(target, null));

                    return;
                }

                if (value is string text && FExStringCipher.IsEnvelope(text))
                {
                    secure.SetFromStoredCiphertext(text, () => _inner.SetValue(target, text));

                    return;
                }
            }

            _inner.SetValue(target, value);
        }
    }

    /// <summary>
    /// Lets a member whose type is not string - a <see cref="SecureNotifyPropertyChanged.DecryptFromJsonSource{T}" />
    /// property - write and read its ciphertext string instead of treating it as the member's type.
    /// </summary>
    private sealed class EnvelopeReadingConverter : JsonConverter
    {
        private readonly JsonConverter? _inner;

        public EnvelopeReadingConverter(JsonConverter? inner) => _inner = inner;

        public override bool CanConvert(Type objectType) => true;

        // Writes too: Newtonsoft resolves a sealed member type's contract from the declaration, not the value, so
        // the ciphertext string has to be written here rather than parsed as the member's type.
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is string ciphertext)
                writer.WriteValue(ciphertext);
            else if (_inner is { CanWrite: true })
                _inner.WriteJson(writer, value, serializer);
            else
                serializer.Serialize(writer, value);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String && reader.Value is string text && FExStringCipher.IsEnvelope(text))
                return new CiphertextToken(text);

            return _inner is { CanRead: true }
                ? _inner.ReadJson(reader, objectType, existingValue, serializer)
                : serializer.Deserialize(reader, objectType);
        }
    }

    private sealed class CiphertextToken
    {
        public CiphertextToken(string ciphertext) => Ciphertext = ciphertext;

        public string Ciphertext { get; }
    }
}
