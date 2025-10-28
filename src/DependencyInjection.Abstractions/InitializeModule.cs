using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Helpers;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using System.Threading.Tasks;

namespace FEx.DependencyInjection.Abstractions;

public abstract class InitializeModule<TContainer, TEngineContext> : FExInitializable, IInitializeModule<TEngineContext>
    where TContainer : class
{
    public bool HasBeenCompleted { get; private set; }

    protected InitializeModule(params IFExInitializable[] dependencies)
        : base(dependencies)
    {
    }

    public void RegisterServices(TEngineContext context)
    {
        var container = GetModule();
        RegisterServices(container, context);
    }

    public async ValueTask CompleteInitializationAsync(TEngineContext context)
    {
        if (HasBeenCompleted)
            return;

        await OnCompleteInitializationAsync(context);
        HasBeenCompleted = true;
    }

    public virtual ValueTask OnCompleteInitializationAsync(TEngineContext context) => FExValueTaskHelper.CompletedTask;

    protected abstract void RegisterServices(TContainer container, TEngineContext context);

    protected virtual TContainer GetModule() => FExServiceProvider.GetDefaultContainer<TContainer>();
}