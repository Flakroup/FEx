using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>Serializes access to named resources using one semaphore per key.</summary>
public interface ISynchronizedAccessService
{
    /// <summary>Gets the semaphore for a key, creating it when missing.</summary>
    /// <param name="key">The resource key.</param>
    /// <param name="maxParallel">The number of callers allowed in at the same time when the semaphore is created.</param>
    /// <returns>The semaphore for the key.</returns>
    SemaphoreSlim EnsureLock(string key, int maxParallel = 1);
    /// <summary>Runs an action while holding the lock of a key.</summary>
    /// <param name="action">The action to run.</param>
    /// <param name="key">The resource key.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the lock.</param>
    void RunLocked(Action action, string key, CancellationToken cancellationToken = default);
    /// <summary>Runs a function while holding the lock of a key.</summary>
    /// <typeparam name="T">The function result type.</typeparam>
    /// <param name="action">The function to run.</param>
    /// <param name="key">The resource key.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the lock.</param>
    /// <returns>The function result.</returns>
    T RunLocked<T>(Func<T> action, string key, CancellationToken cancellationToken = default);
    /// <summary>Runs an action while holding the lock of a key, waiting for the lock asynchronously.</summary>
    /// <param name="action">The action to run.</param>
    /// <param name="key">The resource key.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the lock.</param>
    /// <returns>A task that completes when the action has run.</returns>
    Task RunLockedAsync(Action action, string key, CancellationToken cancellationToken = default);
    /// <summary>Runs a function while holding the lock of a key, waiting for the lock asynchronously.</summary>
    /// <typeparam name="T">The function result type.</typeparam>
    /// <param name="action">The function to run.</param>
    /// <param name="key">The resource key.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the lock.</param>
    /// <returns>A task that yields the function result.</returns>
    Task<T> RunLockedAsync<T>(Func<T> action, string key, CancellationToken cancellationToken = default);
    /// <summary>Releases the lock of a key.</summary>
    /// <param name="key">The resource key.</param>
    void Release(string key);
    /// <summary>Waits asynchronously to enter the lock of a key.</summary>
    /// <param name="key">The resource key.</param>
    /// <param name="maxParallel">The number of callers allowed in at the same time when the semaphore is created.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the lock.</param>
    /// <returns>A task that completes when the lock has been acquired.</returns>
    Task WaitAsync(string key, int maxParallel = 1, CancellationToken cancellationToken = default);
    /// <summary>Waits to enter the lock of a key.</summary>
    /// <param name="key">The resource key.</param>
    /// <param name="maxParallel">The number of callers allowed in at the same time when the semaphore is created.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the lock.</param>
    void Wait(string key, int maxParallel = 1, CancellationToken cancellationToken = default);
    /// <summary>Removes and disposes the lock of a key.</summary>
    /// <param name="key">The resource key.</param>
    /// <exception cref="InvalidOperationException">The lock is currently held.</exception>
    void RemoveLock(string key);
}