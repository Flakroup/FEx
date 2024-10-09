using FEx.DI.Abstractions.Interfaces;
using FEx.Platforms.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Platforms;

[Register(typeof(RegistryService), Scope.SingleInstance, typeof(IRegistryService))]
[Register(typeof(FExPlatforms),
    Scope.SingleInstance,
    typeof(FExPlatforms),
    typeof(IInitializeModule))]
public class FExPlatformsModule
{
    public static void AddServices(IFExPlatformsContainer module, IServiceCollection services) => services.AddSingletonServiceUsingContainer<IRegistryService>(module);
}