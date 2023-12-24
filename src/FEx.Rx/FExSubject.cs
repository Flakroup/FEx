using System;
using System.Reactive.Subjects;

namespace FEx.Rx;

public class FExSubject<T> : IDisposable, IObservable<T>
{
#pragma warning disable IDISP008
    protected readonly ISubject<T> _subject;
#pragma warning restore IDISP008

    private bool _isDisposed;

    public FExSubject(ISubject<T> subject = null)
    {
        _subject = subject ?? new Subject<T>();
    }

    public IDisposable Subscribe(IObserver<T> observer) => _subject.Subscribe(observer);

    public virtual void OnNext(T value)
    {
        SynchronizedOnNext(value);
    }

    protected void SynchronizedOnNext(T value)
    {
        Subject.Synchronize(_subject).OnNext(value);
    }

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (_isDisposed)
            return;

        if (isDisposing && _subject is IDisposable disposable)
            disposable.Dispose();

        _isDisposed = true;
    }
    #endregion
}