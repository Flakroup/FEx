using FEx.Agnostics.Exceptions;
using System;

namespace FEx.Encryption.Exceptions;

/// <summary>
/// The module was asked to encrypt or decrypt without a usable passphrase.
/// </summary>
/// <remarks>
/// There is deliberately no default passphrase. A key derived from something public - a user name, a
/// machine name - protects nothing while looking like it does, so the module refuses to start rather
/// than hand out false assurance. Set <c>IFExEncryptionSettings.PassPhrase</c>.
/// <para>
/// This is a programming error and should not be caught; a value that genuinely cannot be read surfaces
/// as <see cref="FExDecryptionException" />.
/// </para>
/// </remarks>
[Serializable]
public class FExEncryptionNotConfiguredException : FExException
{
    public FExEncryptionNotConfiguredException()
    {
    }

    public FExEncryptionNotConfiguredException(string message)
        : base(message)
    {
    }

    public FExEncryptionNotConfiguredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
