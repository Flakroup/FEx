using FEx.Abstractions.Interfaces;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Extensions;
using System;

namespace FEx.Avaloniax.Abstractions;

public abstract partial class AsyncInitializableViewModelBase : FExAvaloniaViewModelBase, IAsyncInitializable
{
    protected AsyncInitializableViewModelBase(INavigationService navigationService,
                                              params IAsyncInitializable[] dependencies)
        : base(navigationService)
    {
        _logger = this.GetLogger();
        _initializationSemaphore = new();
        _taskSemaphore = new();
        Type instanceType = GetType();
        TypeName = instanceType.Name;
        TypeFullName = instanceType.FullName;
        _dependencies = new();

        foreach (IAsyncInitializable dependency in dependencies)
            AddDependency(dependency);
    }
}