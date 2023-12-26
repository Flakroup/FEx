using Avalonia;
using FEx.Avaloniax.Abstractions;
using FEx.Extensions;
using ReactiveUI;
using System;
using System.Reflection;

namespace FEx.Avaloniax.Services;

public class AppViewLocator : IViewLocator
{
    public bool SupportsRecycling => false;

    public IViewFor ResolveView<T>(T viewModel, string contract = null)
    {
        if (viewModel.Guard(nameof(viewModel)) is not FExAvaloniaViewModelBase)
            throw new ArgumentOutOfRangeException(viewModel.GetType().Name);

        Assembly vmAssembly = viewModel.GetType().Assembly;
        string name = viewModel.GetType().FullName!.Replace("ViewModel", "View");
        Type type = vmAssembly.GetType(name) ?? throw new ArgumentOutOfRangeException(viewModel.GetType().Name);
        var view = (IViewFor)Activator.CreateInstance(type);

        if (view is StyledElement styledElement)
            styledElement.DataContext = viewModel;

        return view;
    }
}