using FEx.Basics.IO;
using FEx.Encryption;
using FEx.Extensions;
using FEx.Extensions.IO;
using FEx.Json.Extensions;
using System;
using System.IO;

namespace FEx.SecureStorage;

public class SecureStorageService
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

    public T Get<T>(string key)
    {
        FileInfo file = _storage.GetDescendantFile(key + FexFileExtension);
        string encrypted = File.ReadAllText(file.FullName);
        string decrypted = StringHasher.DecryptString(_cipher, encrypted);

        return decrypted.FromJson<T>();
    }

    public void Set(string key, object content)
    {
        FileInfo file = _storage.GetDescendantFile(key + FexFileExtension);
        string decrypted = content.ToJson();
        string encrypted = StringHasher.EncryptString(_cipher, decrypted);
        File.WriteAllText(file.FullName, encrypted);
    }
}