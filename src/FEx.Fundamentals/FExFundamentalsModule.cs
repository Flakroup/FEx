using FEx.Abstractions;
using FEx.Asyncx.Helpers;
using FEx.Basics.Abstractions.Interfaces;
using FEx.Basics.Helpers;
using FEx.Fundamentals.StackTraces;
using FEx.Fundamentals.Subjects;
using FEx.Fundamentals.Utilities;
using StrongInject;
using System.Linq;

namespace FEx.Fundamentals;

[Register(typeof(AsyncHelper))]
[Register(typeof(Foundation), Scope.SingleInstance)]
[Register(typeof(StackTraceGenerator), Scope.SingleInstance, typeof(IStackTraceProvider))]
[Register(typeof(AppInfoProvider), Scope.SingleInstance, typeof(IAppInfoProvider))]
[Register(typeof(EventDeliverer), Scope.SingleInstance, typeof(IEventDeliverer))]
[Register(typeof(TasksInfoSubject), Scope.SingleInstance, typeof(ITasksInfoSubject))]
[Register(typeof(ExceptionHandler), Scope.SingleInstance, typeof(IExceptionHandler))]
[Register(typeof(FExFundamentalsModuleInitializer), Scope.SingleInstance, typeof(FExFundamentalsModuleInitializer), typeof(IInitializeModule))]
public class FExFundamentalsModule
{
    [Instance]
    public static IStackTraceFilter[] StackTraceFilters => Enumerable.Empty<IStackTraceFilter>().ToArray();

    [Instance]
    public static IFExServiceProvider ServiceProviderInstance => Foundation.StrongInjectServiceProvider;
}