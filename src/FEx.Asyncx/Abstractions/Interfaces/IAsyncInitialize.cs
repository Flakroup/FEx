using System.Threading.Tasks;

namespace FEx.Asyncx.Abstractions.Interfaces;

public interface IAsyncInitialize
{
    Task<bool> InitializationTask { get; }
    bool IsInitialized { get; }
}