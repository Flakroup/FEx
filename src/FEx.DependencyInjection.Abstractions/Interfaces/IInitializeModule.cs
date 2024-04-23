using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

public interface IInitializeModule
{
    bool IsInitialized { get; }
    bool HasBeenCompleted { get; }

    void Initialize();
    void ConfigureServices(object container, IServiceCollection services);
    Task CompleteInitializationAsync();
}