using Avalonia.Controls;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FEx.MVVM.Rx.BaseObjects;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;

namespace FEx.Avaloniax.Abstractions;

public abstract class FExAvaloniaViewModelBase : ReactiveNotifyPropertyChanged, IRoutableViewModel,
    IActivatableViewModel, IDisposable
{
    private bool _isDisposed;

    public string UrlPathSegment { get; }
    public IScreen HostScreen => NavigationService;

    // Assigned in the ctor for the normal runtime path; only stays unset during design-time preview
    // (the ctor early-returns when Design.IsDesignMode), where the navigation service is never used.
    public INavigationService NavigationService { get; protected set; } = null!;

    public ViewModelActivator Activator { get; }

    protected FExAvaloniaViewModelBase(INavigationService navigationService)
    {
        UrlPathSegment = $"{GetType().Name}_{Guid.NewGuid()}";
        Activator = new();

        if (Design.IsDesignMode)
            return;

        NavigationService = navigationService;

        this.WhenActivated(HandleActivation);
    }

    protected virtual void OnDeactivated()
    {
    }

    protected virtual async Task OnActivatedAsync()
    {
        await Task.CompletedTask;
    }

    private void HandleActivation(CompositeDisposable disposables)
    {
        FExCoreStatics.AsyncHelper.FireTaskOnThreadPoolAndForget(OnActivatedAsync);
        Disposable.Create(OnDeactivated).DisposeWith(disposables);
    }

    #region IDisposable
    protected virtual void ThrowIfDisposed()
    {
#if NETSTANDARD
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().FullName);
#else
        ObjectDisposedException.ThrowIf(_isDisposed, this);
#endif
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            Activator.Dispose();

        _isDisposed = true;
    }
    #endregion
}