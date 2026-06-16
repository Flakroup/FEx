using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FEx.Core.Collections.Concurrent;

/// <summary>
/// Thread-safe HashSet using composition with ReaderWriterLockSlim.
/// https://stackoverflow.com/questions/18922985/concurrent-hashsett-in-net-framework
/// </summary>
#pragma warning disable IDISP025 // generic collection, may be subclassed
public class ConcurrentHashSet<T> : ISet<T>, IReadOnlyCollection<T>, IDisposable
#pragma warning restore IDISP025
{
    private readonly HashSet<T> _set;
    private readonly ReaderWriterLockSlim _lock = new();

    public int Count
    {
        get
        {
            _lock.EnterReadLock();

            try
            {
                return _set.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public bool IsReadOnly => false;

    public ConcurrentHashSet()
    {
        _set = new();
    }

    public ConcurrentHashSet(IEnumerable<T> collection)
    {
        _set = new(collection);
    }

    public ConcurrentHashSet(IEqualityComparer<T> comparer)
    {
        _set = new(comparer);
    }

    public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        _set = new(collection, comparer);
    }

    void ICollection<T>.Add(T item) => Add(item);

    public bool Remove(T item)
    {
        _lock.EnterWriteLock();

        try
        {
            return _set.Remove(item);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();

        try
        {
            _set.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Contains(T item)
    {
        _lock.EnterReadLock();

        try
        {
            return _set.Contains(item);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _lock.EnterReadLock();

        try
        {
            _set.CopyTo(array, arrayIndex);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<T> GetEnumerator() => GetSnapshot().GetEnumerator();

    public bool Add(T item)
    {
        _lock.EnterWriteLock();

        try
        {
            return _set.Add(item);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void UnionWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();

        try
        {
            _set.UnionWith(other);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void IntersectWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();

        try
        {
            _set.IntersectWith(other);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void ExceptWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();

        try
        {
            _set.ExceptWith(other);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();

        try
        {
            _set.SymmetricExceptWith(other);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();

        try
        {
            return _set.IsSubsetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();

        try
        {
            return _set.IsSupersetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();

        try
        {
            return _set.IsProperSupersetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();

        try
        {
            return _set.IsProperSubsetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        _lock.EnterReadLock();

        try
        {
            return _set.Overlaps(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        _lock.EnterReadLock();

        try
        {
            return _set.SetEquals(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public int RemoveWhere(Predicate<T> match)
    {
        _lock.EnterWriteLock();

        try
        {
            return _set.RemoveWhere(match);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void AddRange(IEnumerable<T> items)
    {
        var deferredList = items?.ToList();

        if (deferredList.IsNullOrEmpty())
            return;

        _lock.EnterWriteLock();

        try
        {
            foreach (var item in deferredList)
                _set.Add(item);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public List<T> GetSnapshot()
    {
        _lock.EnterReadLock();

        try
        {
            return _set.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

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

    #region IDisposable
    public void Dispose()
    {
        _lock.Dispose();
    }
    #endregion
}