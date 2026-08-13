using FEx.Core.Abstractions.Interfaces;
using System;
using System.Reactive.Subjects;

namespace FEx.Core.Abstractions.Subjects;

public class FExSubject<T> : IFExSubject<T>
{
#pragma warning disable IDISP008 // Don't assign member with injected and created disposables
    protected readonly ISubject<T> _subject;
#pragma warning restore IDISP008 // Don't assign member with injected and created disposables

    private readonly ISubject<T> _syncSubject;
    private bool _isDisposed;

    public FExSubject()
        : this(null)
    {
    }

    public FExSubject(ISubject<T>? subject)
    {
        _subject = subject ?? new Subject<T>();
        _syncSubject = Subject.Synchronize(_subject);
    }

    public virtual void OnNext(T value) => SynchronizedOnNext(value);

    public virtual void OnCompleted() => SynchronizedOnCompleted();

    /// <summary>Notifies the provider that an observer is to receive notifications.</summary>
    /// <param name="observer">The object that is to receive notifications.</param>
    /// <returns>
    /// A reference to an interface that allows observers to stop receiving notifications before the provider has
    /// finished sending them.
    /// </returns>
    public IDisposable Subscribe(IObserver<T> observer) => _subject.Subscribe(observer);

    protected void SynchronizedOnNext(T value) => _syncSubject.OnNext(value);

    protected void SynchronizedOnCompleted() => _syncSubject.OnCompleted();

    #region IDisposable
    protected virtual void Dispose(bool isDisposing)
    {
        if (_isDisposed)
            return;

        if (isDisposing)
        {
            _syncSubject.OnCompleted();

            if (_subject is IDisposable disposable)
                disposable.Dispose();
        }

        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}