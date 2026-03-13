using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.NuGetx;

[Register(typeof(NuGetManager), Scope.SingleInstance)]
[Register(typeof(NuGetLogger<NuGetManager>), Scope.SingleInstance)]
[Register(typeof(NuGetEx), Scope.SingleInstance, typeof(NuGetEx), typeof(IInitializeModule<IServiceCollection>))]
public class NuGetExModule
{
    public static void AddServices(INuGetExModule container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<NuGetManager>(container);
        services.AddSingletonServiceUsingContainer<NuGetLogger<NuGetManager>>(container);
    }
}