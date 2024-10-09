using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using FEx.DI.Abstractions;
using FEx.DI.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System;

namespace FEx.DependencyInjection;

[Register(typeof(FExDependencyInjection),
    Scope.SingleInstance,
    typeof(FExDependencyInjection),
    typeof(IInitializeModule))]
[Register(typeof(AsyncHelper), typeof(IAsyncHelper))]
[Register(typeof(FExFoundation), Scope.SingleInstance, typeof(FExFoundation), typeof(IFExPriorityInitialize))]
[Register(typeof(FExMicrosoftDIServiceProvider), Scope.SingleInstance, typeof(FExMicrosoftDIServiceProvider))]
public class FExDependencyInjectionModule
{
    [Instance(Options.AsEverythingPossible)]
    public static IFExServiceProvider ServiceProviderInstance => FExServiceProvider.ServiceProvider;

    public static void AddServices(IFExDependencyInjectionModule container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IScopeProvider>(container);
        services.AddSingletonServiceUsingContainer<IServiceProvider>(container);
        services.AddSingletonServiceUsingContainer<IFExServiceProvider>(container);
        services.AddSingletonServiceUsingContainer<IFExInitialize[]>(container);
        services.AddSingletonServiceUsingContainer<IInitializeModule[]>(container);
        services.AddSingletonServiceUsingContainer<IFExPriorityInitialize[]>(container);
    }
}