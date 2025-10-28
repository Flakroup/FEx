using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Common.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.MVVM.Rx.Abstractions.Interfaces;
using FEx.MVVM.Rx.Utilities;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.MVVM.Rx;

[Register(typeof(StatusService), Scope.SingleInstance, typeof(IStatusService))]
[Register(typeof(FExMvvmRx), Scope.SingleInstance, typeof(FExMvvmRx), typeof(IFExInitializable))]
[Register(typeof(FExMvvmRxModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExMvvmRxModule : InitializeModule<IFExMvvmRxContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExMvvmRxContainer container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IStatusService>(container);
    }
}