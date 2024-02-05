using FEx.Abstractions.Interfaces;
using FEx.Basics.Exceptions;
using FEx.Basics.Utilities;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Basics.Services;

public sealed class SynchronizedAccessService : ISynchronizedAccessService, IDisposable
{
    private ConcurrentDictionary<string, FExSemaphoreSlim> AccessSemaphores { get; }

    public SynchronizedAccessService()
    {
        AccessSemaphores = new ConcurrentDictionary<string, FExSemaphoreSlim>();
    }

    public SemaphoreSlim EnsureLock(string key, int maxParallel = 1) =>
        key is not null
            ? AccessSemaphores.GetOrAdd(key, _ => new FExSemaphoreSlim(maxParallel, maxParallel))
            : null;

    public void RunLocked(Action action, string key, CancellationToken cancellationToken = default)
    {
        Wait(key, cancellationToken: cancellationToken);

        try
        {
            action();
        }
        finally
        {
            Release(key);
        }
    }

    public T RunLocked<T>(Func<T> action, string key, CancellationToken cancellationToken = default)
    {
        Wait(key, cancellationToken: cancellationToken);

        try
        {
            return action();
        }
        finally
        {
            Release(key);
        }
    }

    public async Task RunLockedAsync(Action action, string key, CancellationToken cancellationToken = default)
    {
        await WaitAsync(key, cancellationToken: cancellationToken);

        try
        {
            action();
        }
        finally
        {
            Release(key);
        }
    }

    public async Task<T> RunLockedAsync<T>(Func<T> action, string key, CancellationToken cancellationToken = default)
    {
        await WaitAsync(key, cancellationToken: cancellationToken);

        try
        {
            return action();
        }
        finally
        {
            Release(key);
        }
    }

    public void Release(string key)
    {
        EnsureLock(key).Release();
    }

    public async Task WaitAsync(string key, int maxParallel = 1, CancellationToken cancellationToken = default)
    {
        await EnsureLock(key, maxParallel).WaitAsync(cancellationToken);
    }

    public void Wait(string key, int maxParallel = 1, CancellationToken cancellationToken = default)
    {
        EnsureLock(key, maxParallel).Wait(cancellationToken);
    }

    public void RemoveLock(string key)
    {
        if (!AccessSemaphores.TryGetValue(key, out FExSemaphoreSlim accessSemaphore))
            return;

        if (!accessSemaphore.IsIdle)
            throw new FExException($"Key {key} is still busy");

        if (AccessSemaphores.TryRemove(key, out FExSemaphoreSlim semaphore))
            semaphore.Dispose();
    }

    #region IDisposable
    public void Dispose()
    {
        Parallel.ForEach(AccessSemaphores.Values, sem => sem.Dispose());
    }
    #endregion
}