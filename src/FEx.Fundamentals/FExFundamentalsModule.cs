using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using FEx.Basics.Abstractions.Interfaces;
using FEx.Basics.Helpers;
using FEx.Basics.Services;
using FEx.Basics.Utilities;
using FEx.Basics.Utilities.Comparers;
using FEx.Fundamentals.StackTraces;
using FEx.Fundamentals.Subjects;
using FEx.Fundamentals.Utilities;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Fundamentals;

[Register(typeof(AsyncHelper))]
[Register(typeof(Foundation), Scope.SingleInstance)]
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
public class FExFundamentalsModule
{
    [Instance]
    public static IStackTraceFilter[] StackTraceFilters => Enumerable.Empty<IStackTraceFilter>().ToArray();

    [Instance]
    public static IFExServiceProvider ServiceProviderInstance => Foundation.StrongInjectServiceProvider;

    [Instance]
    public static IComparer<string> StringComparerInstance => AlphanumComparatorFast.Instance;

    public static void AddServices<TContainer>(IServiceCollection services) where TContainer : class, IFExFundamentalsModule
    {
        services.AddTransientServiceUsingContainer<TContainer, AsyncHelper>();
        services.AddTransientServiceUsingContainer<TContainer, IFExDispatcher>();

        services.AddSingletonServiceUsingContainer<TContainer, Foundation>();
        services.AddSingletonServiceUsingContainer<TContainer, IFExServiceProvider>();
        services.AddSingletonServiceUsingContainer<TContainer, IEventDeliverer>();
        services.AddSingletonServiceUsingContainer<TContainer, IExceptionHandler>();
        services.AddSingletonServiceUsingContainer<TContainer, ITasksInfoSubject>();
        services.AddSingletonServiceUsingContainer<TContainer, IStackTraceProvider>();
        services.AddSingletonServiceUsingContainer<TContainer, ISynchronizedAccessService>();
        services.AddSingletonServiceUsingContainer<TContainer, IComparer<string>>();
    }
}