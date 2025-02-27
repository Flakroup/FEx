using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using FEx.Basics.Helpers;
using FEx.Basics.Services;
using FEx.Basics.Utilities;
using FEx.Common.Abstractions.Interfaces;
using FEx.Common.Comparers;
using FEx.Common.Models;
using FEx.Common.Providers;
using FEx.DependencyInjection;
using FEx.DI.Abstractions.Interfaces;
using FEx.Fundamentals.StackTraces;
using FEx.Fundamentals.Subjects;
using FEx.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Fundamentals;

[Register(typeof(AsyncHelper), typeof(IAsyncHelper))]
[Register(typeof(FExFoundation), Scope.SingleInstance)]
[Register(typeof(FExLoggingFoundation), Scope.SingleInstance, typeof(FExLoggingFoundation), typeof(IFExInitialize))]
[Register(typeof(StackTraceGenerator), Scope.SingleInstance, typeof(IStackTraceProvider))]
[Register(typeof(AppInfoProvider), Scope.SingleInstance, typeof(IAppInfoProvider))]
[Register(typeof(MainThreadContextProvider), Scope.SingleInstance, typeof(IMainThreadContextProvider))]
[Register(typeof(TasksInfoSubject), Scope.SingleInstance, typeof(ITasksInfoSubject))]
[Register(typeof(SynchronizedAccessService), Scope.SingleInstance, typeof(ISynchronizedAccessService))]
[Register(typeof(FExFundamentals), Scope.SingleInstance, typeof(FExFundamentals), typeof(IInitializeModule))]
[Register(typeof(AppInfo), Scope.SingleInstance, typeof(IAppInfo))]
[Register(typeof(AlphanumComparatorFast),
    Scope.SingleInstance,
    typeof(AlphanumComparatorFast),
    typeof(IComparer<string>))]
[Register(typeof(DeadlockMonitor), typeof(IDeadlockMonitor))]
public class FExFundamentalsModule : FExDependencyInjectionModule
{
    [Instance]
    public static IStackTraceFilter[] StackTraceFilters => Enumerable.Empty<IStackTraceFilter>().ToArray();

    public static void AddServices(IFExFundamentalsContainer container, IServiceCollection services)
    {
        services.AddTransientServiceUsingContainer<IAsyncHelper>(container);
        services.AddTransientServiceUsingContainer<ILogger>(container);
        services.AddTransientServiceUsingContainer<IFExDispatcher>(container);
        services.AddTransientServiceUsingContainer<IDeadlockMonitor>(container);

        services.AddSingletonServiceUsingContainer<FExFoundation>(container);
        services.AddSingletonServiceUsingContainer<FExLoggingFoundation>(container);
        services.AddSingletonServiceUsingContainer<IStackTraceProvider>(container);
        services.AddSingletonServiceUsingContainer<IAppInfoProvider>(container);
        services.AddSingletonServiceUsingContainer<IMainThreadContextProvider>(container);
        services.AddSingletonServiceUsingContainer<ITasksInfoSubject>(container);
        services.AddSingletonServiceUsingContainer<IExceptionHandler>(container);
        services.AddSingletonServiceUsingContainer<ISynchronizedAccessService>(container);
        services.AddSingletonServiceUsingContainer<IStackTraceFilter[]>(container);
        services.AddSingletonServiceUsingContainer<IComparer<string>>(container);
        services.AddSingletonServiceUsingContainer<AlphanumComparatorFast>(container);
        services.AddSingletonServiceUsingContainer<IAppInfo>(container);
    }
}