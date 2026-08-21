using FEx.Encryption.Internal;
using Shouldly;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace FEx.Encryption.Tests;

/// <summary>
/// The managed PBKDF2 and constant-time compare are only ever CALLED on <c>netstandard2.0</c>, which no
/// test project builds. They are compiled everywhere precisely so these tests can reach them, and what
/// they must prove is agreement: a hand-rolled key derivation that merely looks plausible would silently
/// make data written on one target framework unreadable on another.
/// </summary>
public sealed class CryptoCompatTests
{
    public static TheoryData<string, int, int, int> Vectors =>
        new()
        {
            { "password", 16, 10_000, 32 },
            { "password", 16, 10_000, 64 },
            { "short", 16, 10_000, 20 },   // under one hash block
            { "short", 16, 10_000, 33 },   // one byte past a block boundary
            { "short", 16, 10_000, 63 },   // one byte short of two blocks
            { "short", 16, 10_000, 65 },   // one byte past two blocks
            { "zażółć gęślą jaźń", 16, 10_000, 64 },
            { "☃ 🎉", 16, 10_000, 48 },
            { "", 16, 10_000, 32 },
            { "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", 32, 10_000, 96 },
            { "boundary", 8, 1, 64 },
            { "boundary", 64, 3, 96 },
        };

    [Theory]
    [MemberData(nameof(Vectors))]
    public void Pbkdf2Sha256Managed_AgreesWithTheBcl(string passPhrase, int saltLength, int iterations, int length)
    {
        var salt = Salt(saltLength);

        var managed = CryptoCompat.Pbkdf2Sha256Managed(passPhrase, salt, iterations, length);
        var bcl = Rfc2898DeriveBytes.Pbkdf2(passPhrase, salt, iterations, HashAlgorithmName.SHA256, length);

        managed.ShouldBe(bcl);
    }

    [Theory]
    // Published PBKDF2-HMAC-SHA256 vectors, the SHA-256 counterparts of the RFC 6070 SHA-1 set.
    [InlineData("password", "salt", 1, 32, "120FB6CFFCF8B32C43E7225256C4F837A86548C92CCC35480805987CB70BE17B")]
    [InlineData("password", "salt", 2, 32, "AE4D0C95AF6B46D32D0ADFF928F06DD02A303F8EF3C251DFD6E2D85A95474C43")]
    [InlineData("passwordPASSWORDpassword",
        "saltSALTsaltSALTsaltSALTsaltSALTsalt",
        4096,
        40,
        "348C89DBCBD32B2F32D814B8116E84CF2B17347EBC1800181C4E2A1FB8DD53E1C635518C7DAC47E9")]
    public void Pbkdf2Sha256Managed_MatchesPublishedVectors(string passPhrase,
                                                            string salt,
                                                            int iterations,
                                                            int length,
                                                            string expected)
    {
        var derived = CryptoCompat.Pbkdf2Sha256Managed(passPhrase, Encoding.UTF8.GetBytes(salt), iterations, length);

        Hex(derived).ShouldBe(expected);
    }

    [Fact]
    public void DeriveKey_SelectsAnImplementationThatAgreesWithTheManagedOne()
    {
        var salt = Salt(16);

        var selected = CryptoCompat.DeriveKey("passphrase", salt, 10_000, 64);

        selected.ShouldBe(CryptoCompat.Pbkdf2Sha256Managed("passphrase", salt, 10_000, 64));
    }

    [Fact]
    public void FixedTimeEqualsManaged_IsTrueForEqualArrays() =>
        CryptoCompat.FixedTimeEqualsManaged([1, 2, 3], [1, 2, 3]).ShouldBeTrue();

    [Fact]
    public void FixedTimeEqualsManaged_IsFalseWhenAByteDiffers() =>
        CryptoCompat.FixedTimeEqualsManaged([1, 2, 3], [1, 2, 4]).ShouldBeFalse();

    [Fact]
    public void FixedTimeEqualsManaged_IsFalseWhenLengthsDiffer() =>
        CryptoCompat.FixedTimeEqualsManaged([1, 2, 3], [1, 2]).ShouldBeFalse();

    [Fact]
    public void FixedTimeEqualsManaged_IsTrueForTwoEmptyArrays() =>
        CryptoCompat.FixedTimeEqualsManaged([], []).ShouldBeTrue();

    [Fact]
    public void FixedTimeEquals_SelectsAnImplementationThatAgreesWithTheManagedOne()
    {
        CryptoCompat.FixedTimeEquals([9, 8, 7], [9, 8, 7]).ShouldBeTrue();
        CryptoCompat.FixedTimeEquals([9, 8, 7], [9, 8, 6]).ShouldBeFalse();
    }

    private static byte[] Salt(int length)
    {
        var salt = new byte[length];

        for (var i = 0; i < length; i++)
            salt[i] = (byte)(i * 7 + 3);

        return salt;
    }

    private static string Hex(byte[] bytes)
    {
        var text = new StringBuilder(bytes.Length * 2);

        foreach (var b in bytes)
            text.Append(b.ToString("X2"));

        return text.ToString();
    }
}
