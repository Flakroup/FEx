using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.EFCore;

[Register(typeof(ResilientTransaction))]
[Register(typeof(FExEFCore), Scope.SingleInstance, typeof(FExEFCore), typeof(IFExInitialize))]
[Register(typeof(FExEFCoreModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExEFCoreModule : InitializeModule<IFExEFCoreModule, IServiceCollection>
{
    protected override void RegisterServices(IFExEFCoreModule container, IServiceCollection services)
    {
        services.AddTransientServiceUsingContainer<ResilientTransaction>(container);
        services.AddSingletonServiceUsingContainer<ISqlDbHelper>(container);
    }
}