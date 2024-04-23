using FEx.DependencyInjection.Abstractions;
using FEx.Encryption.Abstractions.Interfaces;

namespace FEx.Encryption;

public class FExEncryptionModuleInitializer : InitializeOnlyModule
{
    private readonly IFExEncryptionSettings _settings;

    public FExEncryptionModuleInitializer(IFExEncryptionSettings settings)
    {
        _settings = settings;
    }

    protected override void OnInitialize()
    {
        FExEncryption.Initialize(_settings);
    }
}