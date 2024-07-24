using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using FEx.DI.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System;

namespace FEx.DependencyInjection;

[Register(typeof(FExStrongInjectServiceProvider), Scope.SingleInstance)]
[Register(typeof(FExMicrosoftDIServiceProvider),
    Scope.SingleInstance,
    typeof(FExMicrosoftDIServiceProvider),
    typeof(IScopeProvider),
    typeof(IServiceProvider))]
[Register(typeof(FExDependencyInjectionModuleInitializer),
    Scope.SingleInstance,
    typeof(FExDependencyInjectionModuleInitializer),
    typeof(IInitializeModule))]
[Register(typeof(AsyncHelper), typeof(IAsyncHelper))]
public class FExDependencyInjectionModule
{
    public static void AddServices(IFExDependencyInjectionModule container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<FExStrongInjectServiceProvider>(container);
        services.AddSingletonServiceUsingContainer<FExMicrosoftDIServiceProvider>(container);
        services.AddSingletonServiceUsingContainer<IScopeProvider>(container);
        services.AddSingletonServiceUsingContainer<IServiceProvider>(container);
    }
}