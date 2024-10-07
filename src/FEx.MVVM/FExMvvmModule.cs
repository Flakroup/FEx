using FEx.DI.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.MVVM;

[Register(typeof(FExMvvm), Scope.SingleInstance, typeof(FExMvvm), typeof(IInitializeModule))]
public class FExMvvmModule
{
    public static void AddServices(IFExMvvmModule container, IServiceCollection services) => services.AddTransientServiceUsingContainer<IMessagePopupService>(container);
}