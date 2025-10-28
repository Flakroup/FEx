using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using System.Threading.Tasks;

namespace FEx.DependencyInjection.Abstractions;

public abstract class InitializeModule<TContainer, TEngineContext> : FExInitialize, IInitializeModule<TEngineContext>
    where TContainer : class
{
    public bool HasBeenCompleted { get; private set; }

    protected InitializeModule(params IFExInitialize[] dependencies)
        : base(dependencies)
    {
    }

    public void RegisterServices(TEngineContext context)
    {
        var container = GetModule();
        RegisterServices(container, context);
    }

    public async Task CompleteInitializationAsync(TEngineContext context)
    {
        if (HasBeenCompleted)
            return;

        await OnCompleteInitializationAsync(context);
        HasBeenCompleted = true;
    }

    public virtual async Task OnCompleteInitializationAsync(TEngineContext context) => await Task.CompletedTask;

    protected abstract void RegisterServices(TContainer container, TEngineContext context);

    protected virtual TContainer GetModule() => FExServiceProvider.GetDefaultContainer<TContainer>();
}