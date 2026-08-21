using FEx.Encryption.Abstractions;
using FEx.Encryption.Exceptions;
using Shouldly;
using System;
using Xunit;

namespace FEx.Encryption.Tests;

/// <summary>
/// This module used to fall back to a passphrase derived from the user and machine name when none was
/// configured - a key anyone able to read those two public values could rebuild, handed out silently to
/// every consumer who never set one. The point of these is that no such fallback comes back: the absence
/// of a passphrase has to be an error, not a default.
/// </summary>
public sealed class FExEncryptionTests
{
    [Fact]
    public void CreateCipher_WithoutSettings_Throws() =>
        Should.Throw<FExEncryptionNotConfiguredException>(() => FExEncryption.CreateCipher(null));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCipher_WithoutAPassPhrase_Throws(string? passPhrase) =>
        Should.Throw<FExEncryptionNotConfiguredException>(
            () => FExEncryption.CreateCipher(new FExEncryptionSettings { PassPhrase = passPhrase }));

    [Fact]
    public void CreateCipher_WithATooShortPassPhrase_Throws()
    {
        var tooShort = new string('a', FExEncryption.MinimumPassPhraseLength - 1);

        Should.Throw<FExEncryptionNotConfiguredException>(
            () => FExEncryption.CreateCipher(new FExEncryptionSettings { PassPhrase = tooShort }));
    }

    [Fact]
    public void CreateCipher_WithAConfiguredPassPhrase_ReturnsAWorkingCipher()
    {
        var settings = new FExEncryptionSettings { PassPhrase = new string('a', FExEncryption.MinimumPassPhraseLength) };

        var cipher = FExEncryption.CreateCipher(settings);

        cipher.Decrypt(cipher.Encrypt("secret")).ShouldBe("secret");
    }

    [Fact]
    public void CreateCipher_DoesNotDeriveTheKeyFromTheMachineOrUser()
    {
        // The dead default was MD5("{UserName}@{MachineName}"). Whatever a consumer configures, a cipher
        // built from that string must not be able to read it.
        var machineDerived = new FExStringCipher($"{Environment.UserName}@{Environment.MachineName}",
            FExStringCipher.MinIterations);

        var configured = FExEncryption.CreateCipher(new FExEncryptionSettings { PassPhrase = "a configured secret" });

        Should.Throw<FExDecryptionException>(() => machineDerived.Decrypt(configured.Encrypt("secret")));
    }
}
