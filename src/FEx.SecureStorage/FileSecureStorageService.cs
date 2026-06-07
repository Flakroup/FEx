using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.IO;
using FEx.Encryption;
using FEx.Json.Extensions;
using FEx.SecureStorage.Abstractions;
using System;
using System.IO;

namespace FEx.SecureStorage;

/// <summary>
/// Cross-platform fallback storage that persists values as encrypted files under
/// <c>~/.fexStorage/</c>. Uses a per-user/per-machine cipher derived from
/// <c>{UserName}@{MachineName}</c>. Not OS-level secure - prefer
/// <c>WindowsDpapiSecureStorageService</c>, <c>MacOsKeychainSecureStorageService</c>
/// or <c>LinuxLibsecretSecureStorageService</c> when available.
/// </summary>
public class FileSecureStorageService : ISecureStorageService
{
    private const string FexFileExtension = ".sfex";
    private readonly DirectoryInfo _storage;
    private readonly string _cipher;

    public FileSecureStorageService()
    {
        _cipher = $"{Environment.UserName}@{Environment.MachineName}".GenerateMd5OfString();

        _storage = SpecialDirectory.SpecialDirectories[Environment.SpecialFolder.UserProfile]
            .Directory.GetDescendantDirectory(".fexStorage");
    }

    public FileSecureStorageService(string cipher, DirectoryInfo storage)
    {
        _cipher = cipher;
        _storage = storage;
    }

    public T Get<T>(string key)
    {
        var file = _storage.GetDescendantFile(key + FexFileExtension);
        var encrypted = File.ReadAllText(file.FullName);
        var decrypted = StringHasher.DecryptString(_cipher, encrypted);

        return decrypted.FromJson<T>();
    }

    public void Set(string key, object content)
    {
        var file = _storage.GetDescendantFile(key + FexFileExtension);
        var decrypted = content.ToJson();
        var encrypted = StringHasher.EncryptString(_cipher, decrypted);
        File.WriteAllText(file.FullName, encrypted);
    }
}
