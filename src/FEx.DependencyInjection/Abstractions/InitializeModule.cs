using FEx.DI.Abstractions;
using FEx.DI.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace FEx.DependencyInjection.Abstractions;

public abstract class InitializeModule<TModule> : IInitializeModule where TModule : class
{
    public bool IsInitialized { get; private set; }
    public bool HasBeenCompleted { get; private set; }

    public void Initialize()
    {
        if (IsInitialized)
            return;

        OnInitialize();
        IsInitialized = true;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        TModule module = FExServiceProvider.GetDefaultContainer<TModule>();
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

    protected virtual void OnInitialize()
    {
    }
}