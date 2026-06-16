using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Logging;
using System;
using System.Threading;

namespace FEx.Agnostics.Utilities;

public class ExtendedReaderWriterLockSlim : ReaderWriterLockSlim
{
    private readonly Type _ownerType;

    public ExtendedReaderWriterLockSlim(object owner)
        : this(owner, LockRecursionPolicy.SupportsRecursion)
    {
    }

    public ExtendedReaderWriterLockSlim(object owner, LockRecursionPolicy recursionPolicy)
        : base(recursionPolicy)
    {
        _ownerType = owner.GetType();
    }

    public void Read(Action action) => Execute(action, LockType.Read);

    public TResult ReadWithResult<TResult>(Func<TResult> action) => ExecuteWithResult(action, LockType.Read);

    public void Write(Action action) => Execute(action, LockType.Write);

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