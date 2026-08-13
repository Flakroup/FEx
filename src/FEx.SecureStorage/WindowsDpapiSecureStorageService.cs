#if NET5_0_OR_GREATER
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.IO;
using FEx.Json.Extensions;
using FEx.SecureStorage.Abstractions;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace FEx.SecureStorage;

/// <summary>
/// Windows-only secure storage backed by DPAPI (Data Protection API).
/// Encryption is per-user (DataProtectionScope.CurrentUser), so payloads can only be
/// decrypted by the same Windows user account that wrote them. Files are stored under
/// <c>~/.fexStorage-dpapi/</c> with extension <c>.dpapi</c>.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsDpapiSecureStorageService : ISecureStorageService
{
    private const string DpapiFileExtension = ".dpapi";
    private readonly DirectoryInfo _storage;

    public WindowsDpapiSecureStorageService()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("WindowsDpapiSecureStorageService requires Windows.");

        _storage = SpecialDirectory.SpecialDirectories[Environment.SpecialFolder.UserProfile]
            .Directory.GetDescendantDirectory(".fexStorage-dpapi");
    }

    public WindowsDpapiSecureStorageService(DirectoryInfo storage)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("WindowsDpapiSecureStorageService requires Windows.");

        _storage = storage;
    }

    public T Get<T>(string key)
    {
        var file = _storage.GetDescendantFile(key + DpapiFileExtension);
        var encrypted = File.ReadAllBytes(file.FullName);
        var decrypted =
 ProtectedData.Unprotect(encrypted, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
        var json = System.Text.Encoding.UTF8.GetString(decrypted);

        // Get<T> contract is non-null; a null here means corrupt/missing stored JSON - fail loudly.
        return json.FromJson<T>().Guard(nameof(key));
    }

    public void Set(string key, object content)
    {
        var file = _storage.GetDescendantFile(key + DpapiFileExtension);
        var json = content.ToJson();
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var encrypted = ProtectedData.Protect(bytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
        File.WriteAllBytes(file.FullName, encrypted);
    }
}
#endif