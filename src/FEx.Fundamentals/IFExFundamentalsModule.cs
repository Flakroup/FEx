using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Utilities;
using FEx.DependencyInjection;
using FEx.Fundamentals.StackTraces;
using FEx.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using StrongInject;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Fundamentals;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExFundamentalsModule : IFExDependencyInjectionModule, IContainer<FExFoundation>,
    IContainer<FExLoggingFoundation>, IContainer<IEventDeliverer>, IContainer<IExceptionHandler>,
    IContainer<ITasksInfoSubject>, IContainer<IStackTraceProvider>, IContainer<IFExDispatcher>,
    IContainer<ISynchronizedAccessService>, IContainer<IComparer<string>>, IContainer<IAppInfoProvider>,
    IContainer<IStackTraceFilter[]>, IContainer<ILogger>, IContainer<SimpleTasksPool>
{
}