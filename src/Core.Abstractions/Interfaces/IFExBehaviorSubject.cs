using System;

namespace FEx.Core.Abstractions.Interfaces;

public interface IFExBehaviorSubject<T> : IFExSubject<T>
{
    /// <summary>
    /// Gets the current value or throws an exception.
    /// </summary>
    /// <value>
    /// The initial value passed to the constructor until <see cref="IObserver{T}.OnNext(T)" /> is called; after
    /// which, the last value passed to <see cref="IObserver{T}.OnNext(T)" />.
    /// </value>
    /// <remarks>
    ///     <para><see cref="Value" /> is frozen after <see cref="IObserver{T}.OnCompleted()" /> is called.</para>
    ///     <para>
    ///     After <see cref="IObserver{T}.OnError(Exception)" /> is called, <see cref="Value" /> always throws the specified
    ///     exception.
    ///     </para>
    ///     <para>An exception is always thrown after <see cref="IDisposable.Dispose()" /> is called.</para>
    ///     <alert type="caller">
    ///     Reading <see cref="Value" /> is a thread-safe operation, though there's a potential race condition when
    ///     <see cref="IObserver{T}.OnNext(T)" /> or <see cref="IObserver{T}.OnError(Exception)" /> are being invoked
    ///     concurrently.
    ///     In some cases, it may be necessary for a caller to use external synchronization to avoid race conditions.
    ///     </alert>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Dispose was called.</exception>
    T Value { get; }
}