using FEx.Agnostics.Abstractions.Extensions;
using FEx.Encryption.Exceptions;
using FEx.Encryption.Internal;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FEx.Encryption;

/// <summary>
/// Authenticated string encryption: AES-256-CBC for confidentiality, HMAC-SHA256 over the result for
/// integrity, and PBKDF2-HMAC-SHA256 to turn a passphrase into keys.
/// </summary>
/// <remarks>
/// <para>
/// <b>Envelope, version 1</b> - Base64 of:
/// <c>[0]</c> version, <c>[1..4]</c> iteration count (big-endian int32), <c>[5..20]</c> salt,
/// <c>[21..36]</c> IV, <c>[37..68]</c> HMAC-SHA256 tag over <c>[0..36] || ciphertext</c>,
/// <c>[69..]</c> ciphertext. Encrypt-then-MAC, so a modified byte anywhere is detected before any
/// attempt is made to decrypt it.
/// </para>
/// <para>
/// <b>Why the iteration count is validated first.</b> Verifying the tag needs the MAC key, deriving the
/// MAC key needs the iteration count - so those four bytes are the one field consumed before anything
/// has vouched for them. A corrupted value of <c>int.MaxValue</c> would spin billions of HMAC rounds
/// inside a property getter. The parser therefore checks length, version and
/// <see cref="MinIterations" />..<see cref="MaxIterations" /> before deriving anything. The bound is a
/// denial-of-service guard, not a security floor: an attacker cannot lower the work factor by editing
/// the field, because a different iteration count yields a different key and fails the tag.
/// </para>
/// <para>
/// <b>Key derivation happens once, not per call.</b> A single PBKDF2 pass at
/// <see cref="DefaultIterations" /> costs hundreds of milliseconds, and this class is read from property
/// getters. The constructor derives the key for its own random salt; keys for salts met while decrypting
/// are memoised per instance. The cache is never static - the passphrase is not part of its key, so a
/// shared map would let one instance decrypt with another instance's key.
/// </para>
/// </remarks>
public sealed class FExStringCipher
{
    /// <summary>PBKDF2 iterations used for values this instance writes.</summary>
    public const int DefaultIterations = 210_000;

    /// <summary>Lowest iteration count accepted, from a stored envelope or from a caller.</summary>
    public const int MinIterations = 10_000;

    /// <summary>Highest iteration count accepted, bounding the work a corrupted envelope can demand.</summary>
    public const int MaxIterations = 1_000_000;

    private const byte EnvelopeVersion = 0x01;
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int TagSize = 32;
    private const int KeySize = 32;
    private const int IterationsOffset = 1;
    private const int SaltOffset = 5;
    private const int IvOffset = 21;
    private const int TagOffset = 37;
    private const int SignedHeaderSize = 37;
    private const int PayloadOffset = 69;
    private const int MinCipherBlock = 16;
    private const int MaxCachedKeys = 64;

    private readonly ConcurrentDictionary<string, DerivedKey> _keys = new();
    private readonly string _passPhrase;
    private readonly byte[] _salt;
    private readonly int _iterations;

    /// <inheritdoc cref="FExStringCipher(string, int)" />
    public FExStringCipher(string passPhrase)
        : this(passPhrase, DefaultIterations)
    {
    }

    /// <param name="passPhrase">The secret. Any length; it is stretched, not used as a key directly.</param>
    /// <param name="iterations">PBKDF2 iterations for values this instance writes.</param>
    /// <exception cref="ArgumentException"><paramref name="passPhrase" /> is null, empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="iterations" /> is outside the accepted bound.</exception>
    public FExStringCipher(string passPhrase, int iterations)
    {
        if (string.IsNullOrWhiteSpace(passPhrase))
            throw new ArgumentException("A passphrase is required.", nameof(passPhrase));

        if (iterations < MinIterations
            || iterations > MaxIterations)
            throw new ArgumentOutOfRangeException(nameof(iterations),
                iterations,
                $"The iteration count must be between {MinIterations} and {MaxIterations}.");

        _passPhrase = passPhrase;
        _iterations = iterations;
        _salt = RandomBytes(SaltSize);

        // Pay the derivation here rather than on the first property read.
        GetKey(_salt, _iterations);
    }

    /// <summary>Encrypts <paramref name="plainText" /> into a self-describing, tamper-evident Base64 envelope.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="plainText" /> is null.</exception>
    public string Encrypt(string plainText)
    {
        plainText.Guard(nameof(plainText));

        var key = GetKey(_salt, _iterations);

        using var aes = Aes.Create();
        aes.Key = key.Cipher;
        aes.GenerateIV();

        byte[] payload;

        using (var encryptor = aes.CreateEncryptor())
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            payload = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        var envelope = new byte[PayloadOffset + payload.Length];
        envelope[0] = EnvelopeVersion;
        WriteInt32BigEndian(envelope, IterationsOffset, _iterations);
        Buffer.BlockCopy(_salt, 0, envelope, SaltOffset, SaltSize);
        Buffer.BlockCopy(aes.IV, 0, envelope, IvOffset, IvSize);
        Buffer.BlockCopy(payload, 0, envelope, PayloadOffset, payload.Length);
        Buffer.BlockCopy(ComputeTag(key.Mac, envelope, payload), 0, envelope, TagOffset, TagSize);

        return Convert.ToBase64String(envelope);
    }

    /// <summary>Verifies and decrypts an envelope produced by <see cref="Encrypt" />.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="cipherText" /> is null.</exception>
    /// <exception cref="FExDecryptionException">
    /// The value is not a readable envelope, was produced with a different passphrase, or has been altered.
    /// </exception>
    public string Decrypt(string cipherText)
    {
        cipherText.Guard(nameof(cipherText));

        var envelope = FromBase64(cipherText);

        if (envelope.Length < PayloadOffset + MinCipherBlock)
            throw new FExDecryptionException("The value is shorter than a cipher envelope can be.");

        if (envelope[0] != EnvelopeVersion)
            throw new FExDecryptionException($"Cipher envelope version {envelope[0]} is not supported.");

        var iterations = ReadInt32BigEndian(envelope, IterationsOffset);

        if (iterations < MinIterations
            || iterations > MaxIterations)
            throw new FExDecryptionException(
                $"The envelope asks for {iterations} PBKDF2 iterations, outside the accepted {MinIterations}..{MaxIterations}.");

        var salt = Slice(envelope, SaltOffset, SaltSize);
        var iv = Slice(envelope, IvOffset, IvSize);
        var tag = Slice(envelope, TagOffset, TagSize);
        var payload = Slice(envelope, PayloadOffset, envelope.Length - PayloadOffset);
        var key = GetKey(salt, iterations);

        if (!CryptoCompat.FixedTimeEquals(ComputeTag(key.Mac, envelope, payload), tag))
            throw new FExDecryptionException(
                "The value failed its integrity check - it was written with a different passphrase, or it has been altered.");

        using var aes = Aes.Create();
        aes.Key = key.Cipher;
        aes.IV = iv;

        try
        {
            using var decryptor = aes.CreateDecryptor();

            return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(payload, 0, payload.Length));
        }
        catch (CryptographicException ex)
        {
            throw new FExDecryptionException("The value passed its integrity check but could not be decoded.", ex);
        }
    }

    private static byte[] FromBase64(string value)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new FExDecryptionException("The value is not valid Base64.", ex);
        }
    }

    private static byte[] ComputeTag(byte[] macKey, byte[] envelope, byte[] payload)
    {
        var signed = new byte[SignedHeaderSize + payload.Length];
        Buffer.BlockCopy(envelope, 0, signed, 0, SignedHeaderSize);
        Buffer.BlockCopy(payload, 0, signed, SignedHeaderSize, payload.Length);

        using var hmac = new HMACSHA256(macKey);

        return hmac.ComputeHash(signed);
    }

    private static byte[] Slice(byte[] source, int offset, int length)
    {
        var slice = new byte[length];
        Buffer.BlockCopy(source, offset, slice, 0, length);

        return slice;
    }

    private static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];

        using var random = RandomNumberGenerator.Create();
        random.GetBytes(bytes);

        return bytes;
    }

    private static void WriteInt32BigEndian(byte[] target, int offset, int value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }

    private static int ReadInt32BigEndian(byte[] source, int offset) =>
        source[offset] << 24 | source[offset + 1] << 16 | source[offset + 2] << 8 | source[offset + 3];

    private DerivedKey GetKey(byte[] salt, int iterations)
    {
        var cacheKey = iterations.ToString(CultureInfo.InvariantCulture) + ":" + Convert.ToBase64String(salt);

        if (_keys.TryGetValue(cacheKey, out var cached))
            return cached;

        var derived = new DerivedKey(CryptoCompat.DeriveKey(_passPhrase, salt, iterations, KeySize * 2));

        // Each instance mints one salt, so a stored property carries the salt of the session that last
        // wrote it and a file ends up holding as many distinct salts as it has encrypted properties. Every
        // one of them has to stay memoised: a salt that falls outside the cache pays a full derivation on
        // every single read. The ceiling is here only so a hostile file cannot grow the map without bound.
        if (_keys.Count < MaxCachedKeys)
            _keys.TryAdd(cacheKey, derived);

        return derived;
    }

    private sealed class DerivedKey
    {
        internal DerivedKey(byte[] material)
        {
            Cipher = Slice(material, 0, KeySize);
            Mac = Slice(material, KeySize, KeySize);
        }

        internal byte[] Cipher { get; }

        internal byte[] Mac { get; }
    }
}
