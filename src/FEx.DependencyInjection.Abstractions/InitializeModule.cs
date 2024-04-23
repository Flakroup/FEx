using FEx.DependencyInjection.Abstractions.Interfaces;
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

    public void ConfigureServices(object container, IServiceCollection services) => AddServices((TModule)container, services);

    protected abstract void OnInitialize();

    protected abstract void AddServices(TModule container, IServiceCollection services);

    public async Task CompleteInitializationAsync()
    {
        await OnCompleteInitializationAsync();
        HasBeenCompleted = true;
    }

    public virtual async Task OnCompleteInitializationAsync() => await Task.CompletedTask;
}