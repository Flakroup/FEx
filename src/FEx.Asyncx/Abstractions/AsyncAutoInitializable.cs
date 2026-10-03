using FEx.Core.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Asyncx.Abstractions;

public abstract class AsyncAutoInitializable : AsyncInitializable, IRequiresInitialization
{
    protected AsyncAutoInitializable(IAsyncInitializable[] dependencies)
        : base(dependencies)
    {
    }

    void IRequiresInitialization.Initialize() => BeginInitialization();
}