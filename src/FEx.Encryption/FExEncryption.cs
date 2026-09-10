using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.Encryption.Abstractions.Interfaces;
using FEx.Encryption.Exceptions;

namespace FEx.Encryption;

/// <summary>
/// Turns the configured passphrase into the process-wide <see cref="FExStringCipher" />.
/// </summary>
/// <remarks>
/// There is no fallback passphrase. Deriving one from the user and machine name - as this module once
/// did - produces a key anyone can reconstruct from public information, which is worse than no
/// encryption because it reads as protection. Without <c>IFExEncryptionSettings.PassPhrase</c> the
/// module throws at initialization.
/// </remarks>
public class FExEncryption : InitializeOnlyModule, IFExPriorityInitialize
{
    /// <summary>Shortest passphrase a consumer is allowed to configure.</summary>
    public const int MinimumPassPhraseLength = 8;

    private readonly IFExEncryptionSettings? _settings;

    // Set during OnInitialize; the getter guards against pre-init access.
    private static FExStringCipher? _cipher;

    /// <summary>The cipher every <see cref="SecureNotifyPropertyChanged" /> reads from.</summary>
    /// <exception cref="System.ArgumentNullException">The module has not been initialized yet.</exception>
    public static FExStringCipher Cipher
    {
        get => _cipher.GuardProperty();
        private set => _cipher = value.Guard(nameof(value));
    }

    public int Priority { get; }

    public FExEncryption(IFExEncryptionSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Validates the configured passphrase and builds a cipher from it. Pure - it touches no static state,
    /// so the policy can be tested on its own.
    /// </summary>
    /// <exception cref="FExEncryptionNotConfiguredException">
    /// No passphrase was configured, or it is shorter than <see cref="MinimumPassPhraseLength" />.
    /// </exception>
    public static FExStringCipher CreateCipher(IFExEncryptionSettings? settings)
    {
        var passPhrase = settings?.PassPhrase;

        if (string.IsNullOrWhiteSpace(passPhrase))
            throw new FExEncryptionNotConfiguredException(
                "FEx.Encryption has no passphrase. Set IFExEncryptionSettings.PassPhrase - there is no default, because a key derived from public machine or user names would protect nothing.");

        if (passPhrase!.Length < MinimumPassPhraseLength)
            throw new FExEncryptionNotConfiguredException(
                $"The configured passphrase is {passPhrase.Length} characters; at least {MinimumPassPhraseLength} are required.");

        return new FExStringCipher(passPhrase);
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Cipher = CreateCipher(_settings);
    }
}
