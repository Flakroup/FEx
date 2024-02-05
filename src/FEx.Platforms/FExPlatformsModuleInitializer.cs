using FEx.Abstractions;
using FEx.Platforms.Abstractions;

namespace FEx.Platforms;

public class FExPlatformsModuleInitializer : InitializeModule
{
    private readonly IRegistryService _registryService;

    public FExPlatformsModuleInitializer(IRegistryService registryService)
    {
        _registryService = registryService;
    }

    protected override void OnInitialize()
    {
        Platform.Initialize(_registryService);
    }
}