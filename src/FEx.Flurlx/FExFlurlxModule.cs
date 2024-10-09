using FEx.DI.Abstractions.Interfaces;
using FEx.Flurlx.Abstractions.Interfaces;
using Flurl.Http.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Flurlx;

[Register(typeof(FlurlConfigurator), Scope.SingleInstance, typeof(IFlurlConfigurator))]
[Register(typeof(FlurlClientCache), Scope.SingleInstance, typeof(IFlurlClientCache))]
[Register(typeof(FExFlurlx),
    Scope.SingleInstance,
    typeof(FExFlurlx),
    typeof(IInitializeModule))]
public class FExFlurlxModule
{
    public static void AddServices(IFExFlurlxModule container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IFlurlConfigurator>(container);
        services.AddSingletonServiceUsingContainer<IFlurlClientCache>(container);
    }
}