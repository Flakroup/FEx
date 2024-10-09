using FEx.Abstractions.Interfaces;
using FEx.MVVM.Rx.Abstractions.Interfaces;
using FEx.MVVM.Rx.Utilities;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.MVVM.Rx;

[Register(typeof(FExMvvmRx), Scope.SingleInstance, typeof(FExMvvmRx), typeof(IFExInitialize))]
[Register(typeof(StatusService), Scope.SingleInstance, typeof(IStatusService))]
public class FExMvvmRxModule
{
    public static void AddServices(IFExMvvmRxContainer container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IStatusService>(container);
    }
}