using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Core.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Extensions;

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
        var instanceType = GetType();
        TypeName = instanceType.Name;
        TypeFullName = instanceType.FullName;
        _dependencies = new();

        foreach (var dependency in dependencies)
            AddDependency(dependency);
    }
}