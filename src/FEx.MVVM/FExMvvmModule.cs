using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.MVVM;

[Register(typeof(FExMvvm), Scope.SingleInstance, typeof(FExMvvm), typeof(IFExInitialize))]
[Register(typeof(FExMvvmModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExMvvmModule : InitializeModule<IFExMvvmContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExMvvmContainer container, IServiceCollection services)
    {
        services.AddTransientServiceUsingContainer<IMessagePopupService>(container);
    }
}