using FEx.Basics.Abstractions.Interfaces.Collections;
using FEx.Basics.Collections.Concurrent;
using FEx.Basics.Utilities.Collections;
using FEx.Extensions.Collections.Enumerables;
using FEx.Extensions.Collections.Lists;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace FEx.Basics.Abstractions.Collections.Concurrent;

[DebuggerTypeProxy(typeof(ListDebugView<>))]
[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
[Serializable]
public abstract class BaseConcurrentCollection<TColl, T> : BaseObservableCollection<T>,
    IBaseConcurrentCollection<TColl, T> where TColl : class, IList<T>
{
    protected static readonly string[] PropertyChangedArgs = { nameof(Count), "Item[]" };
    protected readonly bool _useResetOnBulkOperations;

    [NonSerialized] protected readonly ReaderWriterLockSlim _lock;

    [NonSerialized] private readonly object _syncRoot;

    public bool IsSynchronized { get; }

    public bool IsFixedSize { get; }

    public int Count => Read(() => Collection.Count);

    public bool IsReadOnly => Collection.IsReadOnly;

    public T this[int index]
    {
        get => Read(() => Collection[index]);
        set
        {
            T oldItem = Write(() =>
            {
                T item = Collection[index];
                Collection[index] = value;

                return item;
            });

            OnReplaceInCollection(value, oldItem, index);
        }
    }

    public object SyncRoot => _syncRoot;

    protected TColl Collection { get; }

    object IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value;
    }

    protected BaseConcurrentCollection(IEnumerable<T> collection = null,
                                       bool useBaseConstructor = true,
                                       bool passIndexOfRemovedItem = false,
                                       bool useResetOnBulkOperations = true,
                                       bool sendAsyncEvents = true)
        : this(passIndexOfRemovedItem, sendAsyncEvents)
    {
        if (collection.IsNotNullOrEmptyEnumerable())
        {
            if (useBaseConstructor)
                Collection = (TColl)Activator.CreateInstance(typeof(TColl), collection);
            else
                AddRange(collection);
        }

        _useResetOnBulkOperations = useResetOnBulkOperations;
    }

    private BaseConcurrentCollection(bool passIndexOfRemovedItem = false, bool sendAsyncEvents = true)
        : base(passIndexOfRemovedItem, sendAsyncEvents)
    {
        _syncRoot = new object();
        _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        Collection = (TColl)Activator.CreateInstance(typeof(TColl));
    }

    public virtual void DoBulkOperation(Action<TColl> action, Func<TColl, bool> triggerCollectionChanged)
    {
        DoBulkOperation(c =>
        {
            action(c);

            return (object)null;
        }, triggerCollectionChanged);
    }

    public virtual TR DoBulkOperation<TR>(Func<TColl, TR> func, Func<TColl, bool> triggerCollectionChanged)
    {
        try
        {
            return Write(() => func(Collection));
        }
        finally
        {
            if (triggerCollectionChanged(Collection))
                OnCollectionReset();
        }
    }

    public int Replace(int index, T item)
    {
        if (Collection.Count < index
            || index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index was out of range");

        T oldItem = Write(() =>
        {
            T value = Collection[index];
            Collection[index] = item;

            return value;
        });

        OnReplaceInCollection(item, oldItem, index);

        return index;
    }

    /// <summary>
    ///     Adds the specified items to this collection.
    /// </summary>
    /// <param name="collection">The items collection to add</param>
    public void AddRange(IEnumerable<T> collection)
    {
        if (collection is not null)
        {
            (List<T> added, int startingIndex) =
                DoBulkOperation(coll => InternalAddRange(coll, collection), _ => false);

            InternalOnAddedRange(added, startingIndex);
        }
    }

    public (bool hasRemovedAny, IList<T> removed) RemoveWhere(Func<T, bool> predicate)
    {
        (List<T> removed, int index) = Write(() =>
        {
            List<T> removedItems = null;
            var lastRemovedItemIndex = 0;

            for (int i = Collection.Count - 1; i > -1; i--)
            {
                T item = Collection[i];

                if (predicate(item))
                {
                    (removedItems ??= []).Add(item);
                    Collection.RemoveAt(i);
                    lastRemovedItemIndex = i;
                }
            }

            return (removed: removedItems, index: lastRemovedItemIndex);
        });

        if (removed is not null)
        {
            if (_useResetOnBulkOperations)
                OnCollectionReset();
            else
                OnRemoveFromCollection(removed, index);
        }

        return (removed is not null, removed);
    }

    public void CopyTo(Array array, int index)
    {
        Read(() => ((ICollection)Collection).CopyTo(array, index));
    }

    /// <summary>
    ///     Adds an object to the end of the <see cref="ConcurrentCollection{T}" />.
    /// </summary>
    /// <param name="item">
    ///     The object to be added to the end of the <see cref="ConcurrentCollection{T}" />.
    ///     The value can be null for reference types
    /// </param>
    public void Add(T item)
    {
        int index = Write(() =>
        {
            Collection.Add(item);

            return Collection.Count - 1;
        });

        OnAddToCollection(item, index);
    }

    public void Clear()
    {
        if (Collection.Count > 0)
        {
            Write(Collection.Clear);
            OnCollectionReset();
        }
    }

    public bool Contains(T item)
    {
        return Read(() => Collection.Contains(item));
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Read(() => Collection.CopyTo(array, arrayIndex));
    }

    /// <summary>
    ///     Removes the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    public bool Remove(T item)
    {
        (bool hasRemoved, int index) = Write(() =>
        {
            int itemIndex = Collection.IndexOf(item);

            if (itemIndex < 0)
                return (false, index: itemIndex);

            Collection.RemoveAt(itemIndex);

            return (true, index: itemIndex);
        });

        if (hasRemoved)
            OnRemoveFromCollection(item, index);

        return hasRemoved;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<T> GetEnumerator() => Read(Collection.GetEnumerator);

    public int Add(object value)
    {
        var item = (T)value;
        Add(item);

        return Collection.Count;
    }

    public bool Contains(object value) => Contains((T)value);

    public int IndexOf(object value) => IndexOf((T)value);

    public void Insert(int index, object value)
    {
        Insert(index, (T)value);
    }

    public void Remove(object value)
    {
        Remove((T)value);
    }

    public int IndexOf(T item)
    {
        return Read(() => Collection.IndexOf(item));
    }

    public void Insert(int index, T item)
    {
        T oldItem = Write(() =>
        {
            if (index < 0
                || Collection.Count < index && Collection.Count != 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index was out of collection range");

            T value = Collection.Count > 0 && index < Collection.Count
                ? Collection[index]
                : default;

            Collection.Insert(index, item);

            return value;
        });

        OnReplaceInCollection(item, oldItem, index);
    }

    public void RemoveAt(int index)
    {
        T item = Write(() =>
        {
            if (index <= -1
                || index >= Collection.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index was out of collection range");

            T value = Collection[index];
            Collection.RemoveAt(index);

            return value;
        });

        OnRemoveFromCollection(item, index);
    }

    public void AddUniqueRange(IEnumerable<T> collection)
    {
        if (collection is not null)
        {
            (List<T> added, int startingIndex) = DoBulkOperation(
                coll => InternalAddRange(coll, collection.Distinct().Where(x => !coll.Contains(x))), _ => false);

            InternalOnAddedRange(added, startingIndex);
        }
    }

    /// <summary>
    ///     Adds an object to the end of the <see cref="ConcurrentCollection{T}" /> if it not exists in it yet.
    /// </summary>
    /// <param name="item">
    ///     The object to be added to the end of the <see cref="ConcurrentCollection{T}" />.
    ///     The value can be null for reference types
    /// </param>
    /// <param name="notifyOfChange">If true notifies about added item</param>
    public bool AddUnique(T item, bool notifyOfChange = true)
    {
        bool hasBeenAdded = Write(() =>
        {
            if (Collection.Contains(item))
                return false;

            Collection.Add(item);

            return true;
        });

        if (hasBeenAdded && notifyOfChange)
            OnAddToCollection(item, Collection.IndexOf(item));

        return hasBeenAdded;
    }

    public void ReplaceRange(IEnumerable<T> collection)
    {
        if (collection is not null)
        {
            var c = 0;

            DoBulkOperation(coll =>
            {
                c = coll.Count;
                coll.Clear();
                coll.AddRangeToList(collection);
            }, coll => c != 0 || coll.Count > 0);
        }
    }

    protected override string[] GetPropertyChangedArgs() => PropertyChangedArgs;

    protected void Read(Action action)
    {
        _lock.EnterReadLock();

        try
        {
            action();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    protected TResult Read<TResult>(Func<TResult> action)
    {
        _lock.EnterReadLock();

        try
        {
            return action();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    protected void Write(Action action)
    {
        _lock.EnterWriteLock();

        try
        {
            action();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    protected TResult Write<TResult>(Func<TResult> action)
    {
        _lock.EnterWriteLock();

        try
        {
            return action();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private (List<T> added, int startingIndex) InternalAddRange(TColl coll, IEnumerable<T> collection)
    {
        int startingIndex = coll.Count;
        List<T> added = null;

        foreach (T item in collection)
        {
            (added ??= []).Add(item);
            coll.Add(item);
        }

        return (added, startingIndex);
    }

    private void InternalOnAddedRange(List<T> added, int startingIndex)
    {
        if (added is not null)
        {
            if (_useResetOnBulkOperations)
                OnCollectionReset();
            else
                OnAddToCollection(added, startingIndex);
        }
    }
}