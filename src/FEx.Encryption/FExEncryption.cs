using FEx.Common.Extensions;
using FEx.DI.Abstractions;
using FEx.Encryption.Abstractions.Interfaces;
using FEx.Extensions;
using System;

namespace FEx.Encryption;

public class FExEncryption : InitializeOnlyModule
{
    private readonly IFExEncryptionSettings _settings;
    private static string _passPhrase;

    public static string PassPhrase
    {
        get => _passPhrase.Guard();
        private set => _passPhrase = value.Guard(nameof(value));
    }

    public FExEncryption(IFExEncryptionSettings settings)
    {
        _settings = settings;
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        PassPhrase = _settings?.PassPhrase ?? $"{Environment.UserName}@{Environment.MachineName}".GenerateMd5OfString();
    }
}