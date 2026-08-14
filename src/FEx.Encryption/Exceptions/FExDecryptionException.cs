using FEx.Agnostics.Exceptions;
using System;

namespace FEx.Encryption.Exceptions;

/// <summary>
/// A stored value could not be turned back into plaintext - wrong passphrase, tampered or corrupted
/// bytes, or an envelope this version does not understand.
/// </summary>
/// <remarks>
/// This is a data condition, not a programming error: a consumer is expected to catch it and tell the
/// user their saved value cannot be read. Configuration mistakes surface as
/// <see cref="FExEncryptionNotConfiguredException" /> instead, so catching one never swallows the other.
/// </remarks>
[Serializable]
public class FExDecryptionException : FExException
{
    public FExDecryptionException()
    {
    }

    public FExDecryptionException(string message)
        : base(message)
    {
    }

    public FExDecryptionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
