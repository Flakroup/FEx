using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FEx.Encryption;

public static class StringHasher
{
    private const int IvSize = 16;

    public static string EncryptString(string key, string plainInput)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.GenerateIV();

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var memoryStream = new MemoryStream();
        memoryStream.Write(aes.IV, 0, IvSize);

        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
        using (var streamWriter = new StreamWriter(cryptoStream))
            streamWriter.Write(plainInput);

        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public static string DecryptString(string key, string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        if (fullCipher.Length < IvSize)
            throw new ArgumentException("Invalid cipher text.");

        var iv = new byte[IvSize];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, IvSize);

        var cipher = new byte[fullCipher.Length - IvSize];
        Buffer.BlockCopy(fullCipher, IvSize, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var memoryStream = new MemoryStream(cipher);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream);

        return streamReader.ReadToEnd();
    }
}