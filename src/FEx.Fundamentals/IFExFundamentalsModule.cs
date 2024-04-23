using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using FEx.Basics.Abstractions.Interfaces;
using FEx.DependencyInjection;
using FEx.Fundamentals.StackTraces;
using Microsoft.Extensions.Logging;
using StrongInject;
using System.Collections.Generic;

namespace FEx.Fundamentals;

public interface IFExFundamentalsModule : IFExDependencyInjectionModule, IContainer<Foundation>,
    IContainer<IEventDeliverer>, IContainer<AsyncHelper>, IContainer<IExceptionHandler>, IContainer<ITasksInfoSubject>,
    IContainer<IStackTraceProvider>, IContainer<IFExDispatcher>, IContainer<ISynchronizedAccessService>,
    IContainer<IComparer<string>>, IContainer<IAppInfoProvider>, IContainer<IStackTraceFilter[]>, IContainer<ILogger>
{
}