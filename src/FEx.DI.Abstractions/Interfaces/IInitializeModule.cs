using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace FEx.DI.Abstractions.Interfaces;

public interface IInitializeModule
{
    bool IsInitialized { get; }
    bool HasBeenCompleted { get; }

    void Initialize();
    void ConfigureServices(IServiceCollection services);
    Task CompleteInitializationAsync(IServiceCollection services);
}