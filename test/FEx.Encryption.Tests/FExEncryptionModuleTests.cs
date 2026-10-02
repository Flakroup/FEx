using FEx.Encryption.Abstractions;
using FEx.Encryption.Exceptions;
using Shouldly;
using Xunit;

namespace FEx.Encryption.Tests;

/// <summary>
/// <see cref="FExEncryption.CreateCipher" /> is tested on its own elsewhere; these go through the module the
/// way an application does. Without them, the line that hands the cipher to every
/// <see cref="SecureNotifyPropertyChanged" /> could be deleted and the whole suite would stay green while
/// every real encrypted property threw.
/// </summary>
[Collection(StaticStateCollection.Name)]
public sealed class FExEncryptionModuleTests
{
    private const string PassPhrase = "the configured passphrase";

    [Fact]
    public void Initialize_WithAPassPhrase_WiresTheCipherEveryPropertyUses()
    {
        new FExEncryption(new FExEncryptionSettings { PassPhrase = PassPhrase }).Initialize();

        var settings = new ModuleSecrets { Secret = "hunter2" };

        settings.Secret.ShouldBe("hunter2");
        settings.RawSecret.ShouldNotBeNull();
        settings.RawSecret.ShouldNotContain("hunter2");

        // The value has to be readable by the configured passphrase and nothing else - proof the static
        // cipher was built from the settings rather than from some default.
        new FExStringCipher(PassPhrase, FExStringCipher.MinIterations).Decrypt(settings.RawSecret!).ShouldBe("hunter2");
        Should.Throw<FExDecryptionException>(
            () => new FExStringCipher("some other passphrase", FExStringCipher.MinIterations).Decrypt(settings.RawSecret!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    public void Initialize_WithoutAUsablePassPhrase_RefusesAndLeavesTheCipherAlone(string? passPhrase)
    {
        new FExEncryption(new FExEncryptionSettings { PassPhrase = PassPhrase }).Initialize();
        var configured = FExEncryption.Cipher;

        var module = new FExEncryption(new FExEncryptionSettings { PassPhrase = passPhrase });

        Should.Throw<FExEncryptionNotConfiguredException>(module.Initialize);
        module.IsInitialized.ShouldBeFalse();
        FExEncryption.Cipher.ShouldBeSameAs(configured);
    }

    private sealed class ModuleSecrets : SecureNotifyPropertyChanged
    {
        private string? _secret;

        internal string? RawSecret => _secret;

        public string? Secret
        {
            get => DecryptFromSource(_secret);
            set => EncryptSource(ref _secret, value);
        }
    }
}
