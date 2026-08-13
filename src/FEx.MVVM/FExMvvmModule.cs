using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.MVVM;

[Register(typeof(FExMvvm), Scope.SingleInstance, typeof(FExMvvm), typeof(IFExInitializable))]
[Register(typeof(FExMvvmModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExMvvmModule : InitializeModule<IFExMvvmContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExMvvmContainer? container, IServiceCollection services)
    {
        container = container.Guard(nameof(container));
        services.AddTransientServiceUsingContainer<IMessagePopupService>(container);
    }
}