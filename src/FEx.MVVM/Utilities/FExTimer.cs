using FEx.Rx.BaseObjects;
using FEx.Rx.Extensions;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.MVVM.Utilities;

public class FExTimer : ReactiveNotifyPropertyChanged, IDisposable
{
    private bool _isDisposed;
    private bool _isRunning;
    private IDisposable _timer;

    /// <summary>
    /// The timer interval
    /// </summary>
    public TimeSpan Interval { get; protected set; }

    /// <summary>
    /// Indicates whether this timer is running.
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        protected set => SetProperty(ref _isRunning, value);
    }

    protected IObservable<long> IntervalObservable =>
        Observable.Interval(Interval, Scheduler.Default).Where(_ => IsRunning);

    public FExTimer()
    {
        Interval = FExMvvm.DefaultUIRefreshInterval;
    }

    public FExTimer WithCallback(Action callback)
    {
        _timer?.Dispose();
        _timer = IntervalObservable.AsyncSubscribe(_ => ExecuteCallback(callback));

        return this;
    }

    public FExTimer WithAsyncCallback(Func<Task> asyncCallback, CancellationToken cancellationToken = default)
    {
        _timer?.Dispose();
        _timer = IntervalObservable.SubscribeTask((_, ct) => ExecuteCallbackAsync(asyncCallback, ct),
            cancellationToken);

        return this;
    }

    public FExTimer WithInterval(double milliseconds) => WithInterval(TimeSpan.FromMilliseconds(milliseconds));

    public FExTimer WithInterval(TimeSpan interval)
    {
        Interval = interval.TotalMilliseconds > 0
            ? interval
            : FExMvvm.DefaultUIRefreshInterval;

        return this;
    }

    /// <summary>
    /// Starts the timer.
    /// </summary>
    public void TimerStart()
    {
        if (IsRunning)
            return;

        IsRunning = true;
    }

    /// <summary>
    /// Stops the timer.
    /// </summary>
    public void TimerStop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
    }

    private void ExecuteCallback(Action callback)
    {
        if (!IsRunning)
            return;

        callback();
    }

    private async Task ExecuteCallbackAsync(Func<Task> callback, CancellationToken cancellationToken)
    {
        if (!IsRunning
            || cancellationToken.IsCancellationRequested)
            return;

        await callback();
    }

    #region IDisposable
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (disposing)
        {
            IsRunning = false;
            _timer?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}