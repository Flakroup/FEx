using FEx.Encryption.Exceptions;
using Shouldly;
using System;
using Xunit;

namespace FEx.Encryption.Tests;

/// <summary>
/// What this cipher replaced returned garbage for a wrong key instead of refusing, accepted only
/// passphrases that happened to be 16, 24 or 32 bytes long, and left ciphertext unauthenticated. These
/// pin the three properties that fix requires: a wrong key fails loudly, any passphrase works, and a
/// changed byte anywhere is caught.
/// </summary>
public sealed class FExStringCipherTests
{
    private const int FastIterations = FExStringCipher.MinIterations;
    private const string PassPhrase = "correct horse battery staple";

    private static readonly FExStringCipher _cipher = new(PassPhrase, FastIterations);

    public static TheoryData<string> Plaintexts =>
        new()
        {
            "",
            "plain ascii",
            "zażółć gęślą jaźń",
            "line one\nline two\r\n\tindented",   // the old validator rejected every one of these
            "emoji ☺ ✓ 🎉",
            new string('x', 10_000),
            "{\"nested\":\"json\",\"n\":1}",
        };

    [Theory]
    [MemberData(nameof(Plaintexts))]
    public void RoundTrip_ReturnsTheOriginal(string plainText) =>
        _cipher.Decrypt(_cipher.Encrypt(plainText)).ShouldBe(plainText);

    [Theory]
    [InlineData("short")]
    [InlineData("seventeen chars..")]
    [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789")]
    public void AnyPassPhraseLengthWorks(string passPhrase)
    {
        var cipher = new FExStringCipher(passPhrase, FastIterations);

        cipher.Decrypt(cipher.Encrypt("secret")).ShouldBe("secret");
    }

    [Fact]
    public void Encrypt_ProducesADifferentEnvelopeEveryTime() =>
        _cipher.Encrypt("same").ShouldNotBe(_cipher.Encrypt("same"));

    [Fact]
    public void Encrypt_RejectsNull() =>
        Should.Throw<ArgumentNullException>(() => _cipher.Encrypt(null!));

    [Fact]
    public void Decrypt_RejectsNull() =>
        Should.Throw<ArgumentNullException>(() => _cipher.Decrypt(null!));

    [Fact]
    public void Decrypt_WithAnotherPassPhrase_ThrowsInsteadOfReturningGarbage()
    {
        var envelope = _cipher.Encrypt("secret");
        var other = new FExStringCipher("an entirely different passphrase", FastIterations);

        Should.Throw<FExDecryptionException>(() => other.Decrypt(envelope));
    }

    [Theory]
    [InlineData(0, "version")]
    [InlineData(8, "salt")]
    [InlineData(24, "iv")]
    [InlineData(40, "tag")]
    [InlineData(72, "ciphertext")]
    public void Decrypt_DetectsAFlippedByteAnywhereInTheEnvelope(int offset, string field)
    {
        var tampered = Tamper(_cipher.Encrypt("secret"), offset);

        Should.Throw<FExDecryptionException>(() => _cipher.Decrypt(tampered),
            $"a flipped byte in the {field} must not pass");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(FExStringCipher.MinIterations - 1)]
    [InlineData(FExStringCipher.MaxIterations + 1)]
    public void Decrypt_RejectsAnOutOfRangeIterationCountWithoutDerivingAnything(int iterations)
    {
        // Reaching key derivation with int.MaxValue here would spin billions of HMAC rounds inside what is,
        // in real use, a property getter. The bound has to be checked before the salt is ever used.
        var envelope = Convert.FromBase64String(_cipher.Encrypt("secret"));
        envelope[1] = (byte)(iterations >> 24);
        envelope[2] = (byte)(iterations >> 16);
        envelope[3] = (byte)(iterations >> 8);
        envelope[4] = (byte)iterations;

        Should.Throw<FExDecryptionException>(() => _cipher.Decrypt(Convert.ToBase64String(envelope)));
    }

    [Fact]
    public void Decrypt_RejectsAnUnknownEnvelopeVersion()
    {
        var envelope = Convert.FromBase64String(_cipher.Encrypt("secret"));
        envelope[0] = 0x02;

        Should.Throw<FExDecryptionException>(() => _cipher.Decrypt(Convert.ToBase64String(envelope)));
    }

    [Fact]
    public void Decrypt_RejectsAValueTooShortToBeAnEnvelope()
    {
        var envelope = Convert.FromBase64String(_cipher.Encrypt("secret"));
        var truncated = new byte[50];
        Buffer.BlockCopy(envelope, 0, truncated, 0, truncated.Length);

        Should.Throw<FExDecryptionException>(() => _cipher.Decrypt(Convert.ToBase64String(truncated)));
    }

    [Fact]
    public void Decrypt_RejectsSomethingThatIsNotBase64() =>
        Should.Throw<FExDecryptionException>(() => _cipher.Decrypt("this is not base64 !!!"));

    [Fact]
    public void Decrypt_ReadsValuesWrittenUnderADifferentSaltBySameSecret()
    {
        // Each instance mints its own salt, so a settings file accumulates one per writing session.
        var earlierSession = new FExStringCipher(PassPhrase, FastIterations);
        var laterSession = new FExStringCipher(PassPhrase, FastIterations);

        var written = earlierSession.Encrypt("secret");

        laterSession.Decrypt(written).ShouldBe("secret");
        laterSession.Decrypt(laterSession.Encrypt("own")).ShouldBe("own");
    }

    [Fact]
    public void TwoInstancesWithDifferentSecretsDoNotShareCachedKeys()
    {
        // A static key cache keyed on salt alone would let the second instance reuse the first instance's
        // key and read data it has no secret for. Order matters here: the first call is what would fill it.
        var owner = new FExStringCipher("the owning passphrase", FastIterations);
        var envelope = owner.Encrypt("secret");
        owner.Decrypt(envelope).ShouldBe("secret");

        var stranger = new FExStringCipher("some other passphrase", FastIterations);

        Should.Throw<FExDecryptionException>(() => stranger.Decrypt(envelope));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsAMissingPassPhrase(string? passPhrase) =>
        Should.Throw<ArgumentException>(() => new FExStringCipher(passPhrase!, FastIterations));

    [Theory]
    [InlineData(0)]
    [InlineData(FExStringCipher.MinIterations - 1)]
    [InlineData(FExStringCipher.MaxIterations + 1)]
    public void Constructor_RejectsAnOutOfRangeIterationCount(int iterations) =>
        Should.Throw<ArgumentOutOfRangeException>(() => new FExStringCipher(PassPhrase, iterations));

    [Fact]
    public void DefaultIterationsAreWithinTheAcceptedBound()
    {
        FExStringCipher.DefaultIterations.ShouldBeGreaterThanOrEqualTo(FExStringCipher.MinIterations);
        FExStringCipher.DefaultIterations.ShouldBeLessThanOrEqualTo(FExStringCipher.MaxIterations);
    }

    private static string Tamper(string envelope, int offset)
    {
        var bytes = Convert.FromBase64String(envelope);
        bytes[offset] ^= 0xFF;

        return Convert.ToBase64String(bytes);
    }
}
