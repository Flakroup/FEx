using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.IO;
using FEx.Encryption;
using FEx.Encryption.Exceptions;
using FEx.Json.Extensions;
using FEx.SecureStorage.Abstractions;
using System;
using System.IO;

namespace FEx.SecureStorage;

/// <summary>
/// Cross-platform last-resort storage that persists values as files under <c>~/.fexStorage/</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is obfuscation, not confidentiality, and the name says so on purpose.</b> With no OS keystore
/// and no secret from the consumer, the only key available is derived from <c>{UserName}@{MachineName}</c>
/// - values anyone who can query the machine already knows. Stored data is therefore protected against
/// idle inspection, not against anybody who wants it.
/// </para>
/// <para>
/// Prefer <c>WindowsDpapiSecureStorageService</c>, <c>MacOsKeychainSecureStorageService</c> or
/// <c>LinuxLibsecretSecureStorageService</c> wherever they exist - they are compiled only for
/// <c>net5.0</c> and later, so on the down-level targets this type is the only implementation there is.
/// For real protection here, construct it with a <see cref="FExStringCipher" /> built from a passphrase
/// only your application knows.
/// </para>
/// <para>
/// What the machine-bound key does still buy: the payload is authenticated, so a file edited or swapped
/// underneath the application is detected on read instead of decoding into plausible nonsense.
/// </para>
/// </remarks>
public class ObfuscatedFileStorageService : ISecureStorageService
{
    private const string FexFileExtension = ".sfex";
    private readonly DirectoryInfo _storage;
    private readonly FExStringCipher _cipher;

    /// <summary>Machine-bound key, default storage directory - the fallback the module picks by itself.</summary>
    public ObfuscatedFileStorageService()
        : this(new FExStringCipher($"{Environment.UserName}@{Environment.MachineName}"),
            SpecialDirectory.SpecialDirectories[Environment.SpecialFolder.UserProfile]
                .Directory.GetDescendantDirectory(".fexStorage"))
    {
    }

    /// <param name="cipher">Supply one built from an application secret to get actual confidentiality.</param>
    /// <param name="storage">Directory the values are written to.</param>
    public ObfuscatedFileStorageService(FExStringCipher cipher, DirectoryInfo storage)
    {
        _cipher = cipher.Guard(nameof(cipher));
        _storage = storage.Guard(nameof(storage));
    }

    /// <exception cref="FExDecryptionException">The stored file has been altered or was written with another key.</exception>
    public T Get<T>(string key)
    {
        var file = _storage.GetDescendantFile(key + FexFileExtension);
        var encrypted = File.ReadAllText(file.FullName);
        var decrypted = _cipher.Decrypt(encrypted);

        // Get<T> contract is non-null; a null here means corrupt/missing stored JSON - fail loudly.
        return decrypted.FromJson<T>().Guard(nameof(key));
    }

    public void Set(string key, object content)
    {
        var file = _storage.GetDescendantFile(key + FexFileExtension);
        var encrypted = _cipher.Encrypt(content.ToJson());
        File.WriteAllText(file.FullName, encrypted);
    }
}
