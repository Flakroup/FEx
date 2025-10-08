using System;
using System.Text;

namespace FEx.Encryption;

public static class EncryptionHelper
{
    public static string Decode(string encryptedInput)
    {
        byte[] bytes = Convert.FromBase64String(encryptedInput);

        return Encoding.UTF8.GetString(bytes);
    }

    public static string Encode(this string valueToEncrypt)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(valueToEncrypt);

        return Convert.ToBase64String(bytes);
    }
}