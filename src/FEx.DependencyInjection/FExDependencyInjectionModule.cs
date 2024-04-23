using FEx.DependencyInjection.Abstractions.Interfaces;
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