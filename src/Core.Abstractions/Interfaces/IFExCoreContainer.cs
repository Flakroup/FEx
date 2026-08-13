using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Comparers;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Core.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExCoreContainer : IContainer<IAsyncHelper>, IContainer<IDeadlockMonitor>,
    IContainer<IStackTraceProvider>, IContainer<IFExDispatcher>, IContainer<IAppVersionProvider>,
    IContainer<INavigationFlowSubject>, IContainer<IAppThreadingSettings>, IContainer<AlphanumComparatorFast>,
    IContainer<ISynchronizedAccessService>, IContainer<IExceptionHandler>, IContainer<IAppInfoProvider>,
    IContainer<ITasksInfoSubject>
{
}