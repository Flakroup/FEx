using FEx.Encryption.Exceptions;
using Shouldly;
using System;
using System.Linq;
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

    [Theory]
    [InlineData(9_999)]
    [InlineData(1_000_001)]
    public void Decrypt_RejectsAValidlyTaggedEnvelopeWithAnOutOfRangeIterationCount(int iterations)
    {
        // Editing the count of an existing envelope breaks its tag, so that case is refused with or without
        // the range guard. This envelope is genuinely written - and tagged - at the out-of-range count, so
        // only the guard stands between it and the derivation it asks for. The counts are literals on
        // purpose: written as MaxIterations + 1 they would follow a widened bound and pin nothing.
        var writer = new FExStringCipher(PassPhrase, FastIterations);
        var envelope = writer.EncryptCore("secret", Bytes(16, 0x10), iterations, Bytes(16, 0x20));
        var reader = new FExStringCipher(PassPhrase, FastIterations);
        var derivationsBefore = reader.Derivations;

        Should.Throw<FExDecryptionException>(() => reader.Decrypt(envelope));

        reader.Derivations.ShouldBe(derivationsBefore, "the count must be refused before any key is derived");
    }

    [Fact]
    public void Encrypt_ProducesTheVersion1EnvelopeByteForByte()
    {
        // Computed independently of this code (Python hashlib + cryptography) from the documented layout:
        // PBKDF2-HMAC-SHA256 -> 64 bytes, [0..31] AES key, [32..63] MAC key; AES-256-CBC/PKCS7; HMAC-SHA256
        // over header || ciphertext. Any change here makes every stored value unreadable after an upgrade.
        const string expected =
            "AQAAJxAAAQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHwOwlfbA+BguftrmrsBZzch/IwyRwYdErxLEgiqx/H4ClGxYlODfuCcUKZkgRcf8cgUSnox2uuLvXTg7RnZKJAw=";

        var envelope = _cipher.EncryptCore("golden plaintext ✓", Bytes(16, 0x00), FExStringCipher.MinIterations,
            Bytes(16, 0x10));

        envelope.ShouldBe(expected);
    }

    [Fact]
    public void Decrypt_ReadsACheckedInVersion1Envelope()
    {
        // Written at DefaultIterations with a salt and IV this suite never generates - a value as it would
        // sit in a settings file from an earlier build.
        const string stored =
            "AQADNFChssPU5fYHGCk6S1xtfo+QDx4tPEtaaXiHlqW0w9Lh8LEs0zedvt2H3ycrTKq9cmcDLeqoT3Z2r7wBn4auqiTtO6G5zAY2d35YQoQym0i7JfbatKivT3qzQYMEIRiAglIPaRwRBTy4MELW1BAu28CI";

        new FExStringCipher(PassPhrase, FastIterations).Decrypt(stored).ShouldBe("written by version 1 of the envelope");
    }

    [Fact]
    public void KeyCache_DerivesOncePerSaltAcrossRepeatedReads()
    {
        // These are read from property getters; without the cache every read would re-run PBKDF2 - 210k
        // iterations at the default.
        var cipher = new FExStringCipher(PassPhrase, FastIterations);
        var foreign = new FExStringCipher(PassPhrase, FastIterations).Encrypt("from another session");
        cipher.Derivations.ShouldBe(1, "the constructor pays for its own salt up front");

        for (var i = 0; i < 3; i++)
        {
            cipher.Decrypt(cipher.Encrypt("own")).ShouldBe("own");
            cipher.Decrypt(foreign).ShouldBe("from another session");
        }

        cipher.Derivations.ShouldBe(2, "one derivation per distinct salt, however many reads");
    }

    [Fact]
    public void KeyCache_StopsGrowingAtItsCeiling()
    {
        const int ceiling = 64;
        var writer = new FExStringCipher(PassPhrase, FastIterations);
        var envelopes = Enumerable.Range(1, ceiling + 20)
            .Select(i => writer.EncryptCore("secret", Bytes(16, (byte)i), FastIterations, Bytes(16, 0x20)))
            .ToList();
        var reader = new FExStringCipher(PassPhrase, FastIterations);

        envelopes.ForEach(envelope => reader.Decrypt(envelope).ShouldBe("secret"));

        reader.CachedKeyCount.ShouldBe(ceiling);

        // Salts that made it in stay free; one past the ceiling pays on every read instead of evicting.
        var derivations = reader.Derivations;
        reader.Decrypt(envelopes[0]);
        reader.Derivations.ShouldBe(derivations);
        reader.Decrypt(envelopes[^1]);
        reader.Decrypt(envelopes[^1]);
        reader.Derivations.ShouldBe(derivations + 2);
        reader.CachedKeyCount.ShouldBe(ceiling);
    }

    private static byte[] Bytes(int length, byte start) =>
        Enumerable.Range(0, length).Select(i => (byte)(start + i)).ToArray();

    private static string Tamper(string envelope, int offset)
    {
        var bytes = Convert.FromBase64String(envelope);
        bytes[offset] ^= 0xFF;

        return Convert.ToBase64String(bytes);
    }
}
