using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace FEx.Avaloniax.Abstractions;

public abstract class FExAvaloniaReactiveUserControl<T> : ReactiveUserControl<T>, IDisposable
    where T : class, IActivatableViewModel
{
    protected readonly CompositeDisposable _disposables;
    protected bool _disposed;

    protected FExAvaloniaReactiveUserControl()
    {
        _disposables = [];
    }

    protected virtual void OnActivated()
    {
    }

    protected virtual void OnDeactivated()
    {
    }

    protected void SubscribeToWhenActivated()
    {
        var subscription = this.WhenActivated(disposables =>
        {
            OnActivated();
            Disposable.Create(OnDeactivated).DisposeWith(disposables);
        });

        _disposables.Add(subscription);
    }

    #region IDisposable
    public virtual void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual void ThrowIfDisposed()
    {
#if NETSTANDARD
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);
#else
        ObjectDisposedException.ThrowIf(_disposed, this);
#endif
    }
    #endregion
}