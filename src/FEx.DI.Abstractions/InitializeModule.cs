using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.DI.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace FEx.DI.Abstractions;

public abstract class InitializeModule<TModule> : FExInitialize, IInitializeModule where TModule : class
{
    public bool HasBeenCompleted { get; private set; }

    protected InitializeModule(params IFExInitialize[] dependencies)
        : base(dependencies)
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
        TModule module = GetModule();
        AddServices(module, services);
    }

    public async Task CompleteInitializationAsync(IServiceCollection services)
    {
        if (HasBeenCompleted)
            return;

        await OnCompleteInitializationAsync(services);
        HasBeenCompleted = true;
    }

    public virtual async Task OnCompleteInitializationAsync(IServiceCollection services) => await Task.CompletedTask;

    protected abstract void AddServices(TModule container, IServiceCollection services);

    protected virtual TModule GetModule() => FExServiceProvider.GetDefaultContainer<TModule>();
}