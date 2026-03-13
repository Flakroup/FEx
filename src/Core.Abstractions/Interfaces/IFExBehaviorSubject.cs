using System;
using System.Reactive.Subjects;

namespace FEx.Core.Abstractions.Interfaces;

public interface IFExBehaviorSubject<T> : IFExSubject<T>
{
    /// <summary>
    /// Gets the current value or throws an exception.
    /// </summary>
    /// <value>
    /// The initial value passed to the constructor until <see cref="BehaviorSubject{T}.OnNext" /> is called; after
    /// which, the last value passed to <see cref="BehaviorSubject{T}.OnNext" />.
    /// </value>
    /// <remarks>
    ///     <para><see cref="Value" /> is frozen after <see cref="BehaviorSubject{T}.OnCompleted" /> is called.</para>
    ///     <para>
    ///     After <see cref="BehaviorSubject{T}.OnError" /> is called, <see cref="Value" /> always throws the specified
    ///     exception.
    ///     </para>
    ///     <para>An exception is always thrown after <see cref="BehaviorSubject{T}.Dispose" /> is called.</para>
    ///     <alert type="caller">
    ///     Reading <see cref="Value" /> is a thread-safe operation, though there's a potential race condition when
    ///     <see cref="BehaviorSubject{T}.OnNext" /> or <see cref="BehaviorSubject{T}.OnError" /> are being invoked
    ///     concurrently.
    ///     In some cases, it may be necessary for a caller to use external synchronization to avoid race conditions.
    ///     </alert>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Dispose was called.</exception>
    T Value { get; }
}