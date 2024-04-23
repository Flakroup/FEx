using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.EFCore;

[Register(typeof(ResilientTransaction))]
[Register(typeof(FExEFCoreModuleInitializer),
    Scope.SingleInstance,
    typeof(FExEFCoreModuleInitializer),
    typeof(IInitializeModule))]
public class FExEFCoreModule
{
    public static void AddServices(IFExEFCoreModule module, IServiceCollection services)
    {
        services.AddTransientServiceUsingContainer<ResilientTransaction>(module);
        services.AddSingletonServiceUsingContainer<ISqlDbHelper>(module);
    }
}