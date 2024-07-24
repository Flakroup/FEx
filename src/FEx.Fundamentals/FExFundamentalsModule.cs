using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using FEx.Asyncx.Utilities;
using FEx.Basics.Helpers;
using FEx.Basics.Services;
using FEx.Basics.Utilities;
using FEx.Basics.Utilities.Comparers;
using FEx.DependencyInjection;
using FEx.DI.Abstractions.Interfaces;
using FEx.Fundamentals.StackTraces;
using FEx.Fundamentals.Subjects;
using FEx.Fundamentals.Utilities;
using FEx.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Fundamentals;

[Register(typeof(AsyncHelper))]
[Register(typeof(SimpleTasksPool))]
[Register(typeof(FExFoundation), Scope.SingleInstance)]
[Register(typeof(FExLoggingFoundation), Scope.SingleInstance)]
[Register(typeof(StackTraceGenerator), Scope.SingleInstance, typeof(IStackTraceProvider))]
[Register(typeof(AppInfoProvider), Scope.SingleInstance, typeof(IAppInfoProvider))]
[Register(typeof(EventDeliverer), Scope.SingleInstance, typeof(IEventDeliverer))]
[Register(typeof(TasksInfoSubject), Scope.SingleInstance, typeof(ITasksInfoSubject))]
[Register(typeof(ExceptionHandler), Scope.SingleInstance, typeof(IExceptionHandler))]
[Register(typeof(SynchronizedAccessService), Scope.SingleInstance, typeof(ISynchronizedAccessService))]
[Register(typeof(FExFundamentalsModuleInitializer),
    Scope.SingleInstance,
    typeof(FExFundamentalsModuleInitializer),
    typeof(IInitializeModule))]
public class FExFundamentalsModule : FExDependencyInjectionModule
{
    [Instance]
    public static IStackTraceFilter[] StackTraceFilters => Enumerable.Empty<IStackTraceFilter>().ToArray();

    [Instance]
    public static IComparer<string> StringComparerInstance => AlphanumComparatorFast.Instance;

    public static void AddServices(IFExFundamentalsModule container, IServiceCollection services)
    {
        services.AddTransientServiceUsingContainer<AsyncHelper>(container);
        services.AddTransientServiceUsingContainer<ILogger>(container);

        services.AddSingletonServiceUsingContainer<FExFoundation>(container);
        services.AddSingletonServiceUsingContainer<FExLoggingFoundation>(container);
        services.AddSingletonServiceUsingContainer<IStackTraceProvider>(container);
        services.AddSingletonServiceUsingContainer<IAppInfoProvider>(container);
        services.AddSingletonServiceUsingContainer<IEventDeliverer>(container);
        services.AddSingletonServiceUsingContainer<ITasksInfoSubject>(container);
        services.AddSingletonServiceUsingContainer<IExceptionHandler>(container);
        services.AddSingletonServiceUsingContainer<ISynchronizedAccessService>(container);
        services.AddSingletonServiceUsingContainer<IFExDispatcher>(container);
        services.AddSingletonServiceUsingContainer<IStackTraceFilter[]>(container);
        services.AddSingletonServiceUsingContainer<IComparer<string>>(container);
    }
}