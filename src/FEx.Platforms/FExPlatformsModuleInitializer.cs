using FEx.DependencyInjection.Abstractions;
using FEx.Platforms.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Platforms;

public class FExPlatformsModuleInitializer : InitializeModule<IFExPlatformsModule>
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

    protected override void AddServices(IFExPlatformsModule container, IServiceCollection services)
    {
        FExPlatformsModule.AddServices(container, services);
    }
}