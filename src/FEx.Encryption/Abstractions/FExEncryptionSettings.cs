using FEx.Encryption.Abstractions.Interfaces;

namespace FEx.Encryption.Abstractions;

public class FExEncryptionSettings : IFExEncryptionSettings
{
    /// <inheritdoc />
    public string? PassPhrase { get; set; }
}