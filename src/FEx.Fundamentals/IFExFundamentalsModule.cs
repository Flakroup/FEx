using FEx.Abstractions;
using FEx.Basics.Helpers;
using StrongInject;

namespace FEx.Fundamentals;

public interface IFExFundamentalsModule : IContainer<Foundation>, IContainer<IFExServiceProvider>,
    IContainer<IInitializeModule[]>, IContainer<IEventDeliverer>, IContainer<AsyncHelper>, IContainer<IExceptionHandler>
{
}