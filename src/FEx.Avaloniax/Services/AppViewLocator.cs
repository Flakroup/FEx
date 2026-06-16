using Avalonia;
using FEx.Avaloniax.Abstractions;
using ReactiveUI;
using System;
using System.Linq;

namespace FEx.Avaloniax.Services;

public class AppViewLocator : IViewLocator
{
    public static bool SupportsRecycling => false;

#pragma warning disable S2360
    public IViewFor<TViewModel> ResolveView<TViewModel>(string contract = null) where TViewModel : class =>
        (IViewFor<TViewModel>)CreateView(typeof(TViewModel), null);
#pragma warning restore S2360

    public IViewFor ResolveView(object viewModel, string contract = null)
    {
        if (viewModel is null)
            throw new ArgumentNullException(nameof(viewModel));

        if (viewModel is not FExAvaloniaViewModelBase)
            throw new ArgumentOutOfRangeException(nameof(viewModel),
                $"{viewModel.GetType().Name} does not inherit from {nameof(FExAvaloniaViewModelBase)}");

        return CreateView(viewModel.GetType(), viewModel);
    }

    private static IViewFor CreateView(Type vmType, object viewModel)
    {
        var name = vmType.Name!.Replace("ViewModel", "View"); //todo cache types and check inheritance
        var type = vmType.Assembly.GetTypes().First(t => name.Equals(t.Name));
        var view = (IViewFor)Activator.CreateInstance(type);

        if (view is StyledElement styledElement
            && viewModel is not null)
            styledElement.DataContext = viewModel;

        return view;
    }
}