using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.IO;
using FEx.Encryption;
using FEx.Json.Extensions;
using FEx.SecureStorage.Abstractions;
using System;
using System.IO;

namespace FEx.SecureStorage;

public class SecureStorageService : ISecureStorageService
{
    private const string FexFileExtension = ".sfex";
    private readonly DirectoryInfo _storage;
    private readonly string _cipher;

    public SecureStorageService()
    {
        _cipher = $"{Environment.UserName}@{Environment.MachineName}".GenerateMd5OfString();

        _storage = SpecialDirectory.SpecialDirectories[Environment.SpecialFolder.UserProfile]
            .Directory.GetDescendantDirectory(".fexStorage");
    }

    public SecureStorageService(string cipher, DirectoryInfo storage)
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
