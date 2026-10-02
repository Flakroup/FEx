using FEx.Agnostics.Abstractions.Logging;
using FEx.Agnostics.BaseObjects;
using FEx.Encryption.Exceptions;
using FEx.Json.Extensions;
using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;

namespace FEx.Encryption;

/// <summary>
/// A <see cref="NotifyPropertyChanged" /> whose backing fields hold ciphertext instead of plaintext.
/// </summary>
/// <remarks>
/// <para>
/// <b>A failed read never destroys the stored value.</b> Earlier versions answered a decryption error by
/// writing null over the backing field, so moving a settings file to another machine erased the
/// protected data. The read path can no longer write at all - that is why the source parameter is passed
/// by value rather than by reference.
/// </para>
/// <para>
/// <b>Nor does it throw.</b> Types deriving from this class are serialized whole - <c>BaseUserSettings</c>
/// re-serializes on every property write - and a serializer walks every public getter. A getter that
/// threw would let one unreadable field block saving every other, unrelated property. Instead the getter
/// returns null, leaves the ciphertext untouched and reports through
/// <see cref="OnDecryptionFailed" />, which logs by default and can be overridden to surface the failure
/// in the UI.
/// </para>
/// <para>
/// <b>Serialize the backing field, never the property.</b> The property getter hands back plaintext, so a
/// serializer left to its own devices writes the secret out in the clear and undoes the whole point of
/// this class. Mark the property so it is skipped and the field so it is kept:
/// <code>
/// [JsonProperty("secret")]
/// private string? _secret;
///
/// [JsonIgnore]
/// public string? Secret
/// {
///     get => DecryptFromSource(_secret);
///     set => EncryptSource(ref _secret, value);
/// }
/// </code>
/// </para>
/// </remarks>
public class SecureNotifyPropertyChanged : NotifyPropertyChanged
{
    /// <summary>The cipher used for this instance. Overridable so a test can supply its own.</summary>
    protected virtual FExStringCipher Cipher => FExEncryption.Cipher;

    /// <summary>
    /// Domain validation for a value that has already been proven authentic. A false answer is treated
    /// exactly like a failed decryption: null is returned and the stored ciphertext is left alone.
    /// </summary>
    /// <remarks>
    /// This used to reject anything that did not look like text, because unauthenticated AES-CBC decrypts
    /// tampered data into garbage silently and that guess was the only signal available. The HMAC now
    /// proves authenticity before this runs, so the default accepts everything - the old filter also
    /// rejected tabs, newlines and symbols, which are legitimate content.
    /// </remarks>
    protected virtual bool IsValid(string? propertyName, string decryptedValue) => true;

    /// <summary>Called when a property could not be read. Logs a warning by default; the stored value is intact.</summary>
    /// <remarks>
    /// The default log names the property and the exception type only. An exception message is not safe to
    /// write out here: a deserializer quotes the offending value, and that value is decrypted plaintext.
    /// </remarks>
    protected virtual void OnDecryptionFailed(string? propertyName, Exception exception) =>
        FExStaticLogger.Warning(
            $"Encrypted property '{propertyName}' on {GetType().Name} could not be read ({exception.GetType().Name}); it reads as null and the stored value was left intact.");

    /// <summary>Decrypts a backing field, or returns null and reports if it cannot be read.</summary>
    protected string? DecryptFromSource(string? source, [CallerMemberName] string? propertyName = null)
    {
        if (source is null)
            return null;

        try
        {
            var decrypted = Cipher.Decrypt(source);

            if (IsValid(propertyName, decrypted))
                return decrypted;

            OnDecryptionFailed(propertyName,
                new FExDecryptionException($"The decrypted value of '{propertyName}' did not pass validation."));
        }
        catch (FExDecryptionException ex)
        {
            OnDecryptionFailed(propertyName, ex);
        }

        return null;
    }

    /// <summary>Decrypts a backing field and deserializes it from JSON.</summary>
    /// <remarks>
    /// A JSON error here is not an integrity problem - the tag already proved the bytes are ours - it means
    /// the stored shape no longer matches the type, which is what a model change looks like. Degrading to
    /// the default keeps a schema change from bringing the application down, and the failure is logged.
    /// <para>
    /// The JSON is deserialized directly rather than through <c>FromJson</c>: that helper logs the
    /// serializer's exception, whose message quotes the decrypted value, and with a debugger attached it
    /// writes the whole document to the temp folder. <see cref="OnDecryptionFailed" /> gets a
    /// <see cref="FExDecryptionException" /> naming the exception type and JSON path instead, with no inner
    /// exception that could carry the plaintext.
    /// </para>
    /// </remarks>
    protected T? DecryptFromJsonSource<T>(string? source, [CallerMemberName] string? propertyName = null)
    {
        var json = DecryptFromSource(source, propertyName);

        if (json is null)
            return default;

        try
        {
            return json == JsonExtensions.NullString
                ? default
                : JsonConvert.DeserializeObject<T>(json, JsonExtensions.DefaultSettings);
        }
        catch (Exception ex)
        {
            var path = ex switch
            {
                JsonReaderException reader => reader.Path,
                JsonSerializationException serialization => serialization.Path,
                _ => null
            };

            OnDecryptionFailed(propertyName,
                new FExDecryptionException(
                    $"The stored value of '{propertyName}' is not a valid {typeof(T).Name} ({ex.GetType().Name} at path '{path}')."));

            return default;
        }
    }

#pragma warning disable S2360 // CallerMemberName requires optional parameter
    protected bool EncryptJsonSource<T>(ref string? backingField,
                                        T newValue,
                                        Action<string?>? onPropertyChanged = null,
                                        [CallerMemberName] string? propertyName = null) =>
        EncryptSource(ref backingField, newValue?.ToJson(), onPropertyChanged, propertyName);

    protected bool EncryptSource(ref string? backingField,
                                 string? newValue,
                                 Action<string?>? onPropertyChanged = null,
                                 [CallerMemberName] string? propertyName = null)
#pragma warning restore S2360
    {
        var encrypted = newValue is null ? null : Cipher.Encrypt(newValue);

        return SetProperty(ref backingField, encrypted, onPropertyChanged, propertyName);
    }
}
