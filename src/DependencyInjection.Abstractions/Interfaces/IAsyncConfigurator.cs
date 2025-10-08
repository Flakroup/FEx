using System.Threading.Tasks;

namespace FEx.DependencyInjection.Abstractions.Interfaces;

public interface IAsyncConfigurator : IAnyConfigurator
{
    Task ConfigureAsync();
}