using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Platforms.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Platforms;

[Register(typeof(RegistryService), Scope.SingleInstance, typeof(IRegistryService))]
[Register(typeof(FExPlatformsModuleInitializer),
    Scope.SingleInstance,
    typeof(FExPlatformsModuleInitializer),
    typeof(IInitializeModule))]
public class FExPlatformsModule
{
    public static void AddServices(IFExPlatformsModule module, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IRegistryService>(module);
    }
}