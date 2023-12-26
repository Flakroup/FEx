using System;
using System.Threading;

namespace FEx.Utilities.Basics;

public class ExtendedReaderWriterLockSlim : ReaderWriterLockSlim
{
    public ExtendedReaderWriterLockSlim(LockRecursionPolicy recursionPolicy = LockRecursionPolicy.SupportsRecursion)
        : base(recursionPolicy)
    {
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
        if (type == LockType.Write)
            EnterWriteLock();
        else
            EnterReadLock();
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