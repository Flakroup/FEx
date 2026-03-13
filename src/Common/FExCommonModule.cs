using FEx.Common.Abstractions.Interfaces;
using FEx.Common.Helpers;
using FEx.Common.Implementations;
using FEx.Common.Subjects;
using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Common;

[Register(typeof(FExCommonModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(MainThreadDispatcher), typeof(IFExDispatcher))]
[Register(typeof(FExInternetConnectionHelper), typeof(IFExInternetConnectionHelper))]
[Register(typeof(DeviceHelper), typeof(IDeviceHelper))]
[Register(typeof(ConnectivityChangedSubject), Scope.SingleInstance, typeof(IConnectivityChangedSubject))]
public class FExCommonModule : InitializeModule<IFExCommonContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExCommonContainer container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IConnectivityChangedSubject>(container);

        services.AddTransientServiceUsingContainer<IMainThreadContextProvider>(container);
        services.AddTransientServiceUsingContainer<IFExInternetConnectionHelper>(container);
        services.AddTransientServiceUsingContainer<IDeviceHelper>(container);
    }
}