using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.Encryption.Abstractions.Interfaces;
using System;

namespace FEx.Encryption;

public class FExEncryption : InitializeOnlyModule, IFExPriorityInitialize
{
    private readonly IFExEncryptionSettings _settings;
    // Set during OnInitialize; the getter guards against pre-init access.
    private static string? _passPhrase;

    public static string PassPhrase
    {
        get => _passPhrase.GuardProperty();
        private set => _passPhrase = value.Guard(nameof(value));
    }

    public int Priority { get; }

    public FExEncryption(IFExEncryptionSettings settings)
    {
        _settings = settings;
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        PassPhrase = (_settings?.PassPhrase ?? $"{Environment.UserName}@{Environment.MachineName}".GenerateMd5OfString()).Guard(nameof(PassPhrase));
    }
}