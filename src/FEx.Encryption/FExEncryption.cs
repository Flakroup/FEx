using FEx.Encryption.Abstractions.Interfaces;
using FEx.Extensions;
using System;

namespace FEx.Encryption;

public class FExEncryption
{
    private static string _passPhrase;

    public static string PassPhrase
    {
        get => _passPhrase.Guard();
        private set => _passPhrase = value;
    }

    public static void Initialize(IFExEncryptionSettings settings)
    {
        PassPhrase = settings?.PassPhrase
                     ?? $"{Environment.UserName}@{Environment.MachineName}".GenerateMd5OfString();
    }
}