using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FEx.Core.Collections.Concurrent;

/// <summary>
/// https://stackoverflow.com/questions/18922985/concurrent-hashsett-in-net-framework
/// </summary>
public class ConcurrentHashSet<T> : HashSet<T>
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion); //todo use extended

    public ConcurrentHashSet(IEnumerable<T> collection)
        : base(collection)
    {
    }

    public ConcurrentHashSet()
    {
    }

    public ConcurrentHashSet(IEqualityComparer<T> comparer)
        : base(comparer)
    {
    }

    public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        : base(collection, comparer)
    {
    }

    public new IEnumerator<T> GetEnumerator() => RunLocked(base.GetEnumerator);

    public TR RunLocked<TR>(Func<TR> func)
    {
        _lock.EnterWriteLock();

        try
        {
            return func();
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
                _lock.ExitWriteLock();
        }
    }

    public void RunLocked(Action action)
    {
        _lock.EnterWriteLock();

        try
        {
            action();
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
                _lock.ExitWriteLock();
        }
    }

    #region Implementation of ICollection<T> ...ish
    public new bool Add(T item) => RunLocked(() => base.Add(item));

    public new void UnionWith(IEnumerable<T> other) => RunLocked(() => base.UnionWith(other));

    public new void IntersectWith(IEnumerable<T> other) => RunLocked(() => base.IntersectWith(other));

    public new void ExceptWith(IEnumerable<T> other) => RunLocked(() => base.ExceptWith(other));

    public new void SymmetricExceptWith(IEnumerable<T> other) => RunLocked(() => base.SymmetricExceptWith(other));

    public new bool IsSubsetOf(IEnumerable<T> other) => RunLocked(() => base.IsSubsetOf(other));

    public new bool IsSupersetOf(IEnumerable<T> other) => RunLocked(() => base.IsSupersetOf(other));

    public new bool IsProperSupersetOf(IEnumerable<T> other) => RunLocked(() => base.IsProperSupersetOf(other));

    public new bool IsProperSubsetOf(IEnumerable<T> other) => RunLocked(() => base.IsProperSubsetOf(other));

    public new bool Overlaps(IEnumerable<T> other) => RunLocked(() => base.Overlaps(other));

    public new bool SetEquals(IEnumerable<T> other) => RunLocked(() => base.SetEquals(other));

    public new void Clear() => RunLocked(base.Clear);

    public new bool Contains(T item) => RunLocked(() => base.Contains(item));

    public new void CopyTo(T[] array, int arrayIndex) => RunLocked(() => base.CopyTo(array, arrayIndex));

    public new bool Remove(T item) => RunLocked(() => base.Remove(item));

    public new int RemoveWhere(Predicate<T> match) => RunLocked(() => base.RemoveWhere(match));

    public new int Count => RunLocked(() => base.Count);

    public bool IsReadOnly => false;

    public void AddRange(IEnumerable<T> items)
    {
        var deferredList = items?.ToList();

        if (deferredList.IsNullOrEmpty())
            return;

        RunLocked(() =>
        {
            foreach (T item in deferredList)
                base.Add(item);
        });
    }
    #endregion
}