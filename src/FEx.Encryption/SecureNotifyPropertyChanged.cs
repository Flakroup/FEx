using FEx.Agnostics.Abstractions.Logging;
using FEx.Agnostics.BaseObjects;
using FEx.Encryption.Exceptions;
using FEx.Json.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
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
/// <b>Serialization contract: what reaches disk is ciphertext, by default.</b> A property getter hands back
/// plaintext, so a serializer left to its own devices would write the secret out in the clear. This class
/// therefore carries <see cref="SecureNotifyPropertyChangedConverter" />, which Newtonsoft.Json picks up for
/// every derived type. Getters always return plaintext and never depend on serializer state; instead every
/// call of <see cref="DecryptFromSource" /> and <see cref="EncryptSource" /> records the current ciphertext
/// of the property it serves, and the converter writes that record in place of the getter's value. A plain
/// encrypted property therefore needs no attributes:
/// <code>
/// private string? _secret;
///
/// public string? Secret
/// {
///     get => DecryptFromSource(_secret);
///     set => EncryptSource(ref _secret, value);
/// }
/// </code>
/// The same holds for a property read through <see cref="DecryptFromJsonSource{T}" />: it is written as its
/// ciphertext string and read back into the backing field unchanged.
/// </para>
/// <para>
/// On the way in, a value with the shape of a cipher envelope is stored as the ciphertext it is - including
/// one written under another passphrase, which then reads as null and is reported, but survives. Anything
/// else - a file written in the clear by an older version - is encrypted on the way in, so the next save
/// migrates it. A setter handed a value that already authenticates as this cipher's output stores it
/// unchanged, which keeps <c>JsonConvert.PopulateObject</c> (it bypasses the converter on the root object)
/// from encrypting it twice.
/// </para>
/// <para>
/// The older field pattern - <c>[JsonProperty]</c> on the backing field, <c>[JsonIgnore]</c> on the property -
/// keeps working unchanged. The converter is Newtonsoft.Json's (FEx's serializer, which <c>BaseUserSettings</c>
/// uses); any other serializer must be pointed at the backing fields that way.
/// </para>
/// </remarks>
[JsonConverter(typeof(SecureNotifyPropertyChangedConverter))]
public class SecureNotifyPropertyChanged : NotifyPropertyChanged
{
    // The latest ciphertext each encrypted property was seen with, keyed by CLR property name. Refreshed by
    // every getter and setter call, so the converter can write the ciphertext without asking the getter for it.
    private readonly ConcurrentDictionary<string, string?> _storedCiphertext = new(StringComparer.Ordinal);

    // Ciphertext the converter is handing to one property's setter while it reads a document. Consumed by that
    // setter's EncryptSource call and removed by the converter right after; a getter never looks at it.
    private readonly ConcurrentDictionary<string, string> _incomingCiphertext = new(StringComparer.Ordinal);

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
        if (propertyName is not null)
            _storedCiphertext[propertyName] = source;

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
        string? encrypted;

        if (propertyName is not null && _incomingCiphertext.TryRemove(propertyName, out var stored))
            encrypted = stored;
        else if (newValue is null || IsOwnCiphertext(newValue))
            encrypted = newValue;
        else
            encrypted = Cipher.Encrypt(newValue);

        if (propertyName is not null)
            _storedCiphertext[propertyName] = encrypted;

        return SetProperty(ref backingField, encrypted, onPropertyChanged, propertyName);
    }

    /// <summary>The ciphertext <paramref name="propertyName" /> last read or wrote, if it is an encrypted property.</summary>
    internal bool TryGetStoredCiphertext(string propertyName, out string? ciphertext) =>
        _storedCiphertext.TryGetValue(propertyName, out ciphertext);

    /// <summary>
    /// Runs <paramref name="setter" /> so that the <see cref="EncryptSource" /> call it makes for
    /// <paramref name="propertyName" /> stores <paramref name="ciphertext" /> verbatim instead of encrypting.
    /// </summary>
    internal void SetFromStoredCiphertext(string propertyName, string ciphertext, Action setter)
    {
        _incomingCiphertext[propertyName] = ciphertext;

        try
        {
            setter();
        }
        finally
        {
            // A setter that never called EncryptSource (an unencrypted property) leaves its token behind.
            _incomingCiphertext.TryRemove(propertyName, out _);
        }
    }

    private bool IsOwnCiphertext(string value)
    {
        if (!FExStringCipher.IsEnvelope(value))
            return false;

        try
        {
            Cipher.Decrypt(value);

            return true;
        }
        catch (FExDecryptionException)
        {
            return false;
        }
    }
}
