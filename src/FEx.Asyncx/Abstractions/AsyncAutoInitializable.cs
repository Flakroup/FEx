using StrongInject;

namespace FEx.Asyncx.Abstractions;

public abstract class AsyncAutoInitializable : AsyncInitializable, IRequiresInitialization
{
    void IRequiresInitialization.Initialize() => Initialize();
}