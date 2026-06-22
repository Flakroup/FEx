using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Comparers;
using FEx.Agnostics.Helpers;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Helpers;
using FEx.Core.Abstractions.Implementations;
using FEx.Core.Abstractions.Interfaces;
using FEx.Core.Abstractions.Models;
using FEx.Core.Abstractions.Providers;
using FEx.Core.Abstractions.Services;
using FEx.Core.Abstractions.Settings;
using FEx.Core.Subjects;
using FEx.Core.Utilities;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Core;

[Register(typeof(FExCoreModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(FExCoreStatics), Scope.SingleInstance, typeof(IFExInitializable))]
[Register(typeof(StackTraceProvider), Scope.SingleInstance, typeof(IStackTraceProvider))]
[Register(typeof(NavigationFlowSubject), Scope.SingleInstance, typeof(INavigationFlowSubject))]
[Register(typeof(AlphanumComparatorFast), Scope.SingleInstance, typeof(AlphanumComparatorFast))]
[Register(typeof(SynchronizedAccessService), Scope.SingleInstance, typeof(ISynchronizedAccessService))]
[Register(typeof(TasksInfoSubject), Scope.SingleInstance, typeof(ITasksInfoSubject))]
[Register(typeof(AsyncHelper), typeof(IAsyncHelper))]
[Register(typeof(DeadlockMonitor), typeof(IDeadlockMonitor))]
[Register(typeof(DefaultAppVersionProvider), typeof(IAppVersionProvider))]
[Register(typeof(DefaultDispatcher), typeof(IFExDispatcher))]
[Register(typeof(ExceptionHandler), typeof(IExceptionHandler))] //todo what with DebugExceptionHandler?
[Register(typeof(MainThreadContextProvider), typeof(IMainThreadContextProvider))]
[Register(typeof(AppInfoProvider), typeof(IAppInfoProvider))]
[Register(typeof(AppInfo), typeof(IAppInfo))]
[Register(typeof(AppThreadingSettings), typeof(IAppThreadingSettings))]
public class FExCoreModule : InitializeModule<IFExCoreContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExCoreContainer container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IStackTraceProvider>(container);
        services.AddSingletonServiceUsingContainer<INavigationFlowSubject>(container);

        services.AddTransientServiceUsingContainer<IAsyncHelper>(container);
        services.AddTransientServiceUsingContainer<IDeadlockMonitor>(container);
        services.AddTransientServiceUsingContainer<IFExDispatcher>(container);
        services.AddTransientServiceUsingContainer<IAppVersionProvider>(container);
    }
}