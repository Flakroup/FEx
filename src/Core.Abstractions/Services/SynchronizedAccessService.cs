using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Core.Abstractions.Services;

public sealed class SynchronizedAccessService : ISynchronizedAccessService, IDisposable
{
    private ConcurrentDictionary<string, SemaphoreSlim> AccessSemaphores { get; }

    public SynchronizedAccessService()
    {
        AccessSemaphores = new();
    }

    public SemaphoreSlim EnsureLock(string key, int maxParallel = 1) =>
        key is not null
            ? AccessSemaphores.GetOrAdd(key, _ => new(maxParallel, maxParallel))
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
        if (key is not null
            && AccessSemaphores.TryGetValue(key, out var semaphore))
            semaphore.Release();
    }

    public async Task WaitAsync(string key, int maxParallel = 1, CancellationToken cancellationToken = default) =>
        await EnsureLock(key, maxParallel).WaitAsync(cancellationToken);

    public void Wait(string key, int maxParallel = 1, CancellationToken cancellationToken = default) =>
        EnsureLock(key, maxParallel).Wait(cancellationToken);

    public void RemoveLock(string key)
    {
        if (!AccessSemaphores.TryRemove(key, out var semaphore))
            return;

        if (semaphore.CurrentCount == 0)
        {
            AccessSemaphores.TryAdd(key, semaphore);

            throw new InvalidOperationException($"Key {key} is still busy");
        }

        semaphore.Dispose();
    }

    #region IDisposable
    public void Dispose() => Parallel.ForEach(AccessSemaphores.Values, sem => sem.Dispose());
    #endregion
}