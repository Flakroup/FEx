using FEx.Agnostics.Abstractions.Interfaces;
using System.Threading.Tasks;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

public interface IInitializeModule<in TEngineContext> : IFExInitializable
{
    bool HasBeenCompleted { get; }

    void RegisterServices(TEngineContext context);
    Task CompleteInitializationAsync(TEngineContext context);
}