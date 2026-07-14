using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Logging;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Core.Abstractions.Interfaces;

namespace FEx.Avaloniax.Abstractions;

public abstract partial class AsyncInitializableViewModelBase : FExAvaloniaViewModelBase, IAsyncInitializable
{
    protected AsyncInitializableViewModelBase(INavigationService navigationService,
                                              params IAsyncInitializable[] dependencies)
        : base(navigationService)
    {
        _logger = FExStaticLogger.Instance;
        _initializationSemaphore = new();
        _taskSemaphore = new();
        var instanceType = GetType();
        TypeName = instanceType.Name;
        TypeFullName = instanceType.FullName.Guard(nameof(instanceType));
        _dependencies = new();

        foreach (var dependency in dependencies)
            AddDependency(dependency);
    }
}