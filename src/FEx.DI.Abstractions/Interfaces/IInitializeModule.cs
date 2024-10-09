using FEx.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace FEx.DI.Abstractions.Interfaces;

public interface IInitializeModule : IFExInitialize
{
    bool HasBeenCompleted { get; }

    void ConfigureServices(IServiceCollection services);
    Task CompleteInitializationAsync(IServiceCollection services);
}