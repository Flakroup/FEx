using FEx.Abstractions;
using FEx.Basics.Abstractions.Interfaces;
using FEx.Fundamentals.Helpers;
using FEx.Fundamentals.StackTraces;
using FEx.Fundamentals.Utilities;
using StrongInject;
using System.Linq;

namespace FEx.Fundamentals;

[Register(typeof(AsyncHelper))]
[Register(typeof(Foundation), Scope.SingleInstance)]
[Register(typeof(StackTraceGenerator), Scope.SingleInstance, typeof(IStackTraceProvider))]
[Register(typeof(AppInfoProvider), Scope.SingleInstance, typeof(IAppInfoProvider))]
public class FExFoundationModule
{
    [Instance]
    public static IStackTraceFilter[] StackTraceFilters => Enumerable.Empty<IStackTraceFilter>().ToArray();

    [Instance]
    public static IFExServiceProvider ServiceProviderInstance => Foundation.StrongInjectServiceProvider;
}