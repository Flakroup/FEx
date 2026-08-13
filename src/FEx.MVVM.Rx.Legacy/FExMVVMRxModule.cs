using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Common.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.MVVM.Rx.Legacy.Abstractions.Interfaces;
using FEx.MVVM.Rx.Legacy.Utilities;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.MVVM.Rx.Legacy;

[Register(typeof(StatusService), Scope.SingleInstance, typeof(IStatusService))]
[Register(typeof(FExMvvmRx), Scope.SingleInstance, typeof(FExMvvmRx), typeof(IFExInitializable))]
[Register(typeof(FExMvvmRxModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExMvvmRxModule : InitializeModule<IFExMvvmRxContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExMvvmRxContainer? container, IServiceCollection services)
    {
        container = container.Guard(nameof(container));
        services.AddSingletonServiceUsingContainer<IStatusService>(container);
    }
}