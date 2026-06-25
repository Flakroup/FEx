using System;
using System.Reactive.Linq;

namespace FEx.Legacy.Mvvm.Observables;

public class DelayedNotificationObserver<T> : IDisposable
{
    protected EventHandler<T> _stateChanged;

    private bool _isDisposed;

    protected IDisposable StateChange { get; }
    protected Func<T> Get { get; }
    protected Action<T> Set { get; }

    public DelayedNotificationObserver(Func<T> getFunc, Action<T> setFunc)
        : this(getFunc, setFunc, TimeSpan.Zero)
    {
    }

    public DelayedNotificationObserver(Func<T> getFunc, Action<T> setFunc, TimeSpan delayTimeSpan)
    {
        Get = getFunc;
        Set = setFunc;

        if (delayTimeSpan == TimeSpan.Zero
            || delayTimeSpan.TotalMilliseconds < 1D)
            delayTimeSpan = TimeSpan.FromMilliseconds(50);

        StateChange = Observable.FromEventPattern<EventHandler<T>, T>(h => _stateChanged += h, h => _stateChanged -= h)
            .Select(x => x.EventArgs)
            .Sample(delayTimeSpan)
            .Subscribe(v => Set(v));
    }

    public void Report(T value) => _stateChanged?.Invoke(this, value);

    ~DelayedNotificationObserver()
    {
        Dispose(false);
    }

    #region IDisposable
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
            StateChange?.Dispose();

        _isDisposed = true;
    }
    #endregion
}