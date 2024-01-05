using FEx.Abstractions;
using FEx.Asyncx.Helpers;
using StrongInject;

namespace FEx.Fundamentals;

public interface IFExFundamentalsModule : IContainer<Foundation>, IContainer<IFExServiceProvider>,
    IContainer<IInitializeModule[]>, IContainer<IEventDeliverer>, IContainer<AsyncHelper>, IContainer<IExceptionHandler>
{
}