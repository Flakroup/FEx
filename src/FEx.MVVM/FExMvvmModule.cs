using FEx.DI.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.MVVM;

[Register(typeof(FExMvvmModuleInitializer),
    Scope.SingleInstance,
    typeof(FExMvvmModuleInitializer),
    typeof(IInitializeModule))]
public class FExMvvmModule
{
    public static void AddServices(IFExMvvmModule container, IServiceCollection services) => services.AddTransientServiceUsingContainer<IMessagePopupService>(container);
}