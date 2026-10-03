using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Logging;
using System;
using System.Threading;

namespace FEx.Agnostics.Utilities;

/// <summary>A <see cref="ReaderWriterLockSlim"/> that runs delegates under the lock, retrying acquisition with warnings and failing after repeated time-outs.</summary>
public class ExtendedReaderWriterLockSlim : ReaderWriterLockSlim
{
    private readonly Type _ownerType;

    /// <summary>Initializes a recursion-supporting lock on behalf of an owner object.</summary>
    /// <param name="owner">The object whose type is named in time-out diagnostics.</param>
    public ExtendedReaderWriterLockSlim(object owner)
        : this(owner, LockRecursionPolicy.SupportsRecursion)
    {
    }

    /// <summary>Initializes a lock with the given recursion policy on behalf of an owner object.</summary>
    /// <param name="owner">The object whose type is named in time-out diagnostics.</param>
    /// <param name="recursionPolicy">Whether recursive lock entry is allowed.</param>
    public ExtendedReaderWriterLockSlim(object owner, LockRecursionPolicy recursionPolicy)
        : base(recursionPolicy)
    {
        _ownerType = owner.GetType();
    }

    /// <summary>Runs the action while holding the read lock.</summary>
    /// <param name="action">The action to run.</param>
    /// <exception cref="TimeoutException">The lock could not be acquired after 10 attempts of 30 seconds each.</exception>
    public void Read(Action action) => Execute(action, LockType.Read);

    /// <summary>Runs the function while holding the read lock and returns its result.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="action">The function to run.</param>
    /// <returns>The value returned by <paramref name="action"/>.</returns>
    /// <exception cref="TimeoutException">The lock could not be acquired after 10 attempts of 30 seconds each.</exception>
    public TResult ReadWithResult<TResult>(Func<TResult> action) => ExecuteWithResult(action, LockType.Read);

    /// <summary>Runs the action while holding the write lock.</summary>
    /// <param name="action">The action to run.</param>
    /// <exception cref="TimeoutException">The lock could not be acquired after 10 attempts of 30 seconds each.</exception>
    public void Write(Action action) => Execute(action, LockType.Write);

    /// <summary>Runs the function while holding the write lock and returns its result.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="action">The function to run.</param>
    /// <returns>The value returned by <paramref name="action"/>.</returns>
    /// <exception cref="TimeoutException">The lock could not be acquired after 10 attempts of 30 seconds each.</exception>
    public TResult WriteWithResult<TResult>(Func<TResult> action) => ExecuteWithResult(action, LockType.Write);

    private void Execute(Action action, LockType type)
    {
        EnterLock(type);

        try
        {
            action();
        }
        finally
        {
            ExitLock(type);
        }
    }

    private TResult ExecuteWithResult<TResult>(Func<TResult> action, LockType type)
    {
        EnterLock(type);

        try
        {
            return action();
        }
        finally
        {
            ExitLock(type);
        }
    }

    private void EnterLock(LockType type)
    {
        var retry = 0;
        const int maxRetries = 10;
        var timeout = TimeSpan.FromSeconds(30);

        while (true)
        {
            var hasLock = type == LockType.Write
                ? TryEnterWriteLock(timeout)
                : TryEnterReadLock(timeout);

            if (hasLock)
                break;

            retry++;

            FExStaticLogger.Warning(
                $"Couldn't acquire {type} lock for {_ownerType.FullName} in {timeout.GetTime()}. Retrying {retry}/{maxRetries}...");

            if (retry >= maxRetries)
                throw new TimeoutException(
                    $"Failed to acquire {type} lock for {_ownerType.FullName} after {maxRetries} retries ({maxRetries * 30}s total).");
        }
    }

    private void ExitLock(LockType type)
    {
        if (type == LockType.Write)
            ExitWriteLock();
        else
            ExitReadLock();
    }

    private enum LockType
    {
        Read = 1,
        Write
    }
}