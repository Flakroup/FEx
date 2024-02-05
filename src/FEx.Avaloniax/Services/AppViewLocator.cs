using Avalonia;
using FEx.Avaloniax.Abstractions;
using ReactiveUI;
using System;
using System.Reflection;

namespace FEx.Avaloniax.Services;

public class AppViewLocator : IViewLocator
{
    public bool SupportsRecycling => false;

    /// <summary>
    /// Determines the view for an associated ViewModel.
    /// </summary>
    /// <typeparam name="T">The view model type.</typeparam>
    /// <param name="viewModel">View model.</param>
    /// <param name="contract">Contract.</param>
    /// <returns>The view associated with the given view model.</returns>
    public IViewFor ResolveView<T>(T viewModel, string contract = null)
    {
        if (viewModel is null)
            throw new ArgumentNullException(nameof(viewModel));

        Type vmType = viewModel.GetType();

        if (viewModel is not FExAvaloniaViewModelBase)
            throw new ArgumentOutOfRangeException(nameof(viewModel),
                $"{vmType.Name} does not inherit from {nameof(FExAvaloniaViewModelBase)}");

        Assembly vmAssembly = vmType.Assembly;
        string name = vmType.Name!.Replace("ViewModel", "View");//todo cache types and check inheritance 
        Type type = vmAssembly.GetType(name) ?? throw new ArgumentOutOfRangeException(vmType.Name);
        var view = (IViewFor)Activator.CreateInstance(type);

        if (view is StyledElement styledElement)
            styledElement.DataContext = viewModel;

        return view;
    }
}