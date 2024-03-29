using FEx.EFCore.Helpers;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.EFCore;

[Register(typeof(ResilientTransaction))]
public class FExEFCoreModule
{
    public static void AddServices<TContainer>(IServiceCollection services) where TContainer : class, IFExEFCoreModule
    {
        services.AddTransientServiceUsingContainer<TContainer, ResilientTransaction>();
    }
}