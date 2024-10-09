using FEx.Abstractions.Interfaces;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Common.Extensions;
using ReactiveUI;
using System.Threading.Tasks;

namespace FEx.Avaloniax.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IFExServiceProvider _serviceProvider;
    private readonly IFExDispatcher _dispatcher;

    public RoutingState Router { get; }

    public NavigationService(IFExServiceProvider serviceProvider, IFExDispatcher dispatcher)
    {
        _serviceProvider = serviceProvider.Guard(nameof(serviceProvider));
        _dispatcher = dispatcher.Guard(nameof(dispatcher));

        Router = new();
    }

    public async Task GoBackAsync()
    {
        if (Router.NavigationStack.Count <= 0)
            return;

        await _dispatcher.InvokeOnMainThreadAsync(Router.NavigateBack.Execute);
    }

    public async Task NavigateAsync<T>() where T : IRoutableViewModel =>
        await _dispatcher.InvokeOnMainThreadAsync(() => Router.Navigate.Execute(GetViewModel<T>()));

    public async Task NavigateAndResetAsync<T>() where T : IRoutableViewModel =>
        await _dispatcher.InvokeOnMainThreadAsync(() => Router.NavigateAndReset.Execute(GetViewModel<T>()));

    private T GetViewModel<T>() where T : IRoutableViewModel
    {
        T viewModel = _serviceProvider.GetRequiredService<T>();

        if (viewModel is IAsyncInitializable asyncInitializable)
            asyncInitializable.BeginInitialization();

        return viewModel;
    }
}