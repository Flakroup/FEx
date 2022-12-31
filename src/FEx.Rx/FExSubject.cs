using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Rx;

public abstract class FExSubject<T> : IDisposable
{
    protected readonly CompositeDisposable _disposable;
    private readonly ISubject<T> _subject;
    private bool _isDisposed;

    protected FExSubject(ISubject<T> subject = null)
    {
        _subject = subject ?? new Subject<T>();
        _disposable = new();

        if (_subject is IDisposable disposable)
            _disposable.Add(disposable);
    }

    public IObservable<T> GetObservable()
    {
        return _subject;
    }

    public void SynchronizedOnNext(T value)
    {
        Subject.Synchronize(_subject)
            .OnNext(value);
    }

    public async Task<T> GetResultAsync(CancellationToken cancellationToken, Func<IObservable<T>, IObservable<T>> observableConfiguration = null)
    {
        return await GetResultAsync<T>(cancellationToken, observableConfiguration);
    }

    public async Task<TResult> GetResultAsync<TResult>(CancellationToken cancellationToken, Func<IObservable<T>, IObservable<TResult>> observableConfiguration = null)
    {
        IObservable<TResult> observable = observableConfiguration?.Invoke(_subject) ?? (IObservable<TResult>)_subject;

        return await observable.ToTask(cancellationToken, null, Scheduler.Default);
    }

    #region IDisposable

    protected virtual void Dispose(bool isDisposing)
    {
        if (!_isDisposed)
        {
            if (isDisposing)
                _disposable.Dispose();

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}