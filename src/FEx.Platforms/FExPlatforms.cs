using FEx.Common.Extensions;
using FEx.DI.Abstractions;
using FEx.Platforms.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Platforms;

public class FExPlatforms : InitializeModule<IFExPlatformsContainer>
{
    private static IRegistryService _registryService;

    public static IRegistryService RegistryService
    {
        get => _registryService.Guard();
        private set => _registryService = value.Guard(nameof(value));
    }

    public FExPlatforms(IRegistryService registryService)
    {
        RegistryService = registryService;
    }

    protected override void AddServices(IFExPlatformsContainer container, IServiceCollection services) =>
        FExPlatformsModule.AddServices(container, services);
}