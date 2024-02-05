using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using FEx.Basics.Abstractions.Interfaces;
using StrongInject;
using System.Collections.Generic;

namespace FEx.Fundamentals;

public interface IFExFundamentalsModule : IContainer<Foundation>, IContainer<IFExServiceProvider>,
    IContainer<IInitializeModule[]>, IContainer<IEventDeliverer>, IContainer<AsyncHelper>, IContainer<IExceptionHandler>, IContainer<ITasksInfoSubject>, IContainer<IStackTraceProvider>, IContainer<IFExDispatcher>, IContainer<ISynchronizedAccessService>, IContainer<IComparer<string>>
{
}