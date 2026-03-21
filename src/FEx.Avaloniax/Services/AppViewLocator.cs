using Avalonia;
using FEx.Avaloniax.Abstractions;
using ReactiveUI;
using System;
using System.Linq;

namespace FEx.Avaloniax.Services;

public class AppViewLocator : IViewLocator
{
    public static bool SupportsRecycling => false;

    /// <summary>
    /// Determines the view for an associated ViewModel.
    /// </summary>
    /// <typeparam name="T">The view model type.</typeparam>
    /// <param name="viewModel">View model.</param>
    /// <param name="contract">Contract.</param>
    /// <returns>The view associated with the given view model.</returns>
#pragma warning disable S2360
    public IViewFor ResolveView<T>(T viewModel, string contract = null)
#pragma warning restore S2360
    {
        if (viewModel is null)
            throw new ArgumentNullException(nameof(viewModel));

        var vmType = viewModel.GetType();

        if (viewModel is not FExAvaloniaViewModelBase)
            throw new ArgumentOutOfRangeException(nameof(viewModel),
                $"{vmType.Name} does not inherit from {nameof(FExAvaloniaViewModelBase)}");

        var vmAssembly = vmType.Assembly;
        var name = vmType.Name!.Replace("ViewModel", "View"); //todo cache types and check inheritance
        var type = vmAssembly.GetTypes().First(t => name.Equals(t.Name));
        var view = (IViewFor)Activator.CreateInstance(type);

        if (view is StyledElement styledElement)
            styledElement.DataContext = viewModel;

        return view;
    }
}