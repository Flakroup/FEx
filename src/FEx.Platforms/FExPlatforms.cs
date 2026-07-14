using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Platforms.Abstractions.Interfaces;

namespace FEx.Platforms;

public class FExPlatforms : FExInitializable
{
    // Set via the RegistryService setter in the ctor (Guard-checked); reads go through GuardProperty which throws if unset.
    private static IRegistryService _registryService = null!;

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