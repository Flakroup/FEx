using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Interfaces;

public interface ISynchronizedAccessService
{
    SemaphoreSlim EnsureLock(string key, int maxParallel = 1);
    void RunLocked(Action action, string key, CancellationToken cancellationToken = default);
    T RunLocked<T>(Func<T> action, string key, CancellationToken cancellationToken = default);
    Task RunLockedAsync(Action action, string key, CancellationToken cancellationToken = default);
    Task<T> RunLockedAsync<T>(Func<T> action, string key, CancellationToken cancellationToken = default);
    void Release(string key);
    Task WaitAsync(string key, int maxParallel = 1, CancellationToken cancellationToken = default);
    void Wait(string key, int maxParallel = 1, CancellationToken cancellationToken = default);
    void RemoveLock(string key);
}