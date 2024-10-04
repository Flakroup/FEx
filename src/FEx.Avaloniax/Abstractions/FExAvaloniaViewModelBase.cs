using Avalonia.Controls;
using FEx.Abstractions;
using FEx.Abstractions.Enums;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.MVVM.Rx.BaseObjects;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace FEx.Avaloniax.Abstractions;

public abstract class FExAvaloniaViewModelBase : ReactiveNotifyPropertyChanged, IRoutableViewModel,
    IActivatableViewModel, IDisposable
{
    private bool _isDisposed;

    public string UrlPathSegment { get; }
    public IScreen HostScreen => NavigationService;

    public INavigationService NavigationService { get; protected set; }

    public ViewModelActivator Activator { get; }

    protected FExAvaloniaViewModelBase(INavigationService navigationService)
    {
        UrlPathSegment = $"{GetType().Name}_{Guid.NewGuid()}";
        Activator = new();

        if (Design.IsDesignMode)
            return;

        NavigationService = navigationService;

        this.WhenActivated(disposables =>
        {
            FExFoundation.AsyncHelper.FireTaskAndForget(OnActivatedAsync, AsyncMode.ThreadPool);
            Disposable.Create(OnDeactivated).DisposeWith(disposables);
        });
    }

    protected virtual void OnDeactivated()
    {
    }

    protected virtual async Task OnActivatedAsync()
    {
        await Task.CompletedTask;
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
            Activator?.Dispose();

        _isDisposed = true;
    }
    #endregion
}