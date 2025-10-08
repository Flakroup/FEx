using ReactiveUI;
using System.Threading.Tasks;

namespace FEx.Avaloniax.Abstractions.Interfaces;

public interface INavigationService : IScreen
{
    Task NavigateAsync<T>() where T : IRoutableViewModel;
    Task NavigateAndResetAsync<T>() where T : IRoutableViewModel;
    Task GoBackAsync();
}