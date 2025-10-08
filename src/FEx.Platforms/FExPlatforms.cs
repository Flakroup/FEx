using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Platforms.Abstractions.Interfaces;

namespace FEx.Platforms;

public class FExPlatforms : FExInitialize
{
    private static IRegistryService _registryService;

    public static IRegistryService RegistryService
    {
        get => _registryService.GuardProperty();
        private set => _registryService = value.Guard(nameof(value));
    }

    public FExPlatforms(IRegistryService registryService)
    {
        RegistryService = registryService;
    }

    protected override void OnInitialize()
    {
        // Platform-specific initialization if needed
    }
}