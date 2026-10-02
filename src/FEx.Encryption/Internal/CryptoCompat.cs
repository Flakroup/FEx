using System;
using System.Security.Cryptography;
using System.Text;

namespace FEx.Encryption.Internal;

/// <summary>
/// The two cryptographic primitives whose BCL surface is not the same on every target framework.
/// </summary>
/// <remarks>
/// <see cref="Pbkdf2Sha256Managed" /> and <see cref="FixedTimeEqualsManaged" /> are compiled on every
/// target framework even though they are only ever called on <c>netstandard2.0</c>. That is deliberate:
/// the test project builds for <c>net10.0</c> alone, so a managed implementation hidden behind
/// <c>#if NETSTANDARD2_0</c> would be the most delicate code in the module and the only code no test
/// could reach. Compiling it everywhere lets the tests pin it against the BCL on the framework they do
/// run on.
/// </remarks>
internal static class CryptoCompat
{
    private const int HashLength = 32;

    /// <summary>Derives <paramref name="length" /> bytes of key material from a passphrase using PBKDF2-HMAC-SHA256.</summary>
    internal static byte[] DeriveKey(string passPhrase, byte[] salt, int iterations, int length)
    {
#if NET6_0_OR_GREATER
        return Rfc2898DeriveBytes.Pbkdf2(passPhrase, salt, iterations, HashAlgorithmName.SHA256, length);
#elif NETSTANDARD2_1
        // The instance constructors carry no obsoletion here; the static Pbkdf2 arrived only in .NET 6.
        using var derive = new Rfc2898DeriveBytes(passPhrase, salt, iterations, HashAlgorithmName.SHA256);

        return derive.GetBytes(length);
#else
        return Pbkdf2Sha256Managed(passPhrase, salt, iterations, length);
#endif
    }

    /// <summary>Compares two byte arrays in time that does not depend on where they first differ.</summary>
    internal static bool FixedTimeEquals(byte[] left, byte[] right)
    {
#if NETSTANDARD2_0
        return FixedTimeEqualsManaged(left, right);
#else
        return CryptographicOperations.FixedTimeEquals(left, right);
#endif
    }

    /// <summary>PBKDF2-HMAC-SHA256 per RFC 2898, for the framework whose BCL only offers the SHA-1 variant.</summary>
    internal static byte[] Pbkdf2Sha256Managed(string passPhrase, byte[] salt, int iterations, int length)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(passPhrase));
        var blockCount = (length + HashLength - 1) / HashLength;
        var output = new byte[length];
        var seed = new byte[salt.Length + 4];
        Buffer.BlockCopy(salt, 0, seed, 0, salt.Length);

        for (var block = 1; block <= blockCount; block++)
        {
            seed[salt.Length] = (byte)(block >> 24);
            seed[salt.Length + 1] = (byte)(block >> 16);
            seed[salt.Length + 2] = (byte)(block >> 8);
            seed[salt.Length + 3] = (byte)block;

            var u = hmac.ComputeHash(seed);
            var accumulator = (byte[])u.Clone();

            for (var iteration = 1; iteration < iterations; iteration++)
            {
                u = hmac.ComputeHash(u);

                for (var i = 0; i < HashLength; i++)
                    accumulator[i] ^= u[i];
            }

            var offset = (block - 1) * HashLength;
            Buffer.BlockCopy(accumulator, 0, output, offset, Math.Min(HashLength, length - offset));
        }

        return output;
    }

    /// <summary>Constant-time array comparison, for the framework without <c>CryptographicOperations</c>.</summary>
    internal static bool FixedTimeEqualsManaged(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;

        var difference = 0;

        for (var i = 0; i < left.Length; i++)
            difference |= left[i] ^ right[i];

        return difference == 0;
    }
}
