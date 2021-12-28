using FEx.Extensions.Collections.Enumerables;
using FEx.Extensions.Collections.Lists;
using System.Collections;
using System.Diagnostics;

namespace FEx.Extensions.Collections.Concurrent;

[DebuggerTypeProxy(typeof(ListDebugView<>))]
[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
[Serializable]
public abstract class BaseConcurrentCollection<TColl, T> : BaseObservableCollection<T>, IBaseConcurrentCollection<TColl, T>
    where TColl : class, IList<T>
{
    protected static readonly string[] PropertyChangedArgs = {nameof(Count), "Item[]"};
    protected readonly bool UseResetOnBulkOperations;

    [NonSerialized] protected readonly object Locker;

    [NonSerialized] private readonly object _syncRoot;

    protected BaseConcurrentCollection(
        IEnumerable<T> collection = null,
        bool useBaseConstructor = true,
        bool passIndexOfRemovedItem = false,
        bool useResetOnBulkOperations = true)
        : this(passIndexOfRemovedItem)
    {
        if (collection.IsNotNullOrEmptyEnumerable())
        {
            if (useBaseConstructor)
            {
                Collection = (TColl) Activator.CreateInstance(typeof(TColl), collection);
            }
            else
            {
                AddRange(collection);
            }
        }

        UseResetOnBulkOperations = useResetOnBulkOperations;
    }

    protected BaseConcurrentCollection(bool passIndexOfRemovedItem = false)
        : base(passIndexOfRemovedItem)
    {
        _syncRoot = new object();
        Locker = new object();
        Collection = (TColl) Activator.CreateInstance(typeof(TColl));
    }

    public bool IsSynchronized { get; }

    public bool IsFixedSize { get; }

    protected TColl Collection { get; }

    public int Count
    {
        get
        {
            lock (Locker)
            {
                return Collection.Count;
            }
        }
    }

    public bool IsReadOnly => Collection.IsReadOnly;

    public T this[int index]
    {
        get
        {
            lock (Locker)
            {
                return Collection[index];
            }
        }
        set
        {
            lock (Locker)
            {
                T oldItem = Collection[index];
                Collection[index] = value;
                OnReplaceInCollection(value, oldItem, index);
            }
        }
    }

    public object SyncRoot => _syncRoot;

    object IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T) value;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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
        lock (Locker)
        {
            Collection.Add(item);
            OnAddToCollection(item, Collection.IndexOf(item));
        }
    }

    /// <summary>
    ///     Adds an object to the end of the <see cref="ConcurrentCollection{T}" /> if it not exists in it yet.
    /// </summary>
    /// <param name="item">
    ///     The object to be added to the end of the <see cref="ConcurrentCollection{T}" />.
    ///     The value can be null for reference types
    /// </param>
    /// <param name="onAdded">Action to invoke after item insert</param>
    /// <param name="notifyOfChange">If true notifies about added item</param>
    public bool AddUnique(T item, Action<TColl, T> onAdded = null, bool notifyOfChange = true)
    {
        lock (Locker)
        {
            if (!Collection.Contains(item))
            {
                Collection.Add(item);

                if (notifyOfChange)
                {
                    OnAddToCollection(item, Collection.IndexOf(item));
                }

                onAdded?.Invoke(Collection, item);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    ///     Adds the specified items to this collection.
    /// </summary>
    /// <param name="collection">The items collection to add</param>
    public void AddRange(IEnumerable<T> collection)
    {
        if (collection != null)
        {
            DoBulkOperation(coll => InternalAddRange(coll, collection), _ => false);
        }
    }

    public void AddUniqueRange(IEnumerable<T> collection)
    {
        if (collection != null)
        {
            DoBulkOperation(coll => InternalAddRange(coll, collection.Distinct().Where(x => !coll.Contains(x))), _ => false);
        }
    }

    public virtual void DoBulkOperation(Action<TColl> action, Func<TColl, bool> triggerCollectionChanged)
    {
        DoBulkOperation(c =>
        {
            action(c);
            return (object) null;
        }, triggerCollectionChanged);
    }

    public virtual TR DoBulkOperation<TR>(Func<TColl, TR> func, Func<TColl, bool> triggerCollectionChanged)
    {
        lock (Locker)
        {
            try
            {
                return func(Collection);
            }
            finally
            {
                if (triggerCollectionChanged(Collection))
                {
                    OnCollectionReset();
                }
            }
        }
    }

    public void Clear()
    {
        lock (Locker)
        {
            if (Collection.Count > 0)
            {
                Collection.Clear();
                OnCollectionReset();
            }
        }
    }

    public bool Contains(T item)
    {
        lock (Locker)
        {
            return Collection.Contains(item);
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (Locker)
        {
            Collection.CopyTo(array, arrayIndex);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock (Locker)
        {
            return Collection.GetEnumerator();
        }
    }

    public int IndexOf(T item)
    {
        lock (Locker)
        {
            return Collection.IndexOf(item);
        }
    }

    public void Insert(int index, T item)
    {
        lock (Locker)
        {
            if (index >= 0 && Collection.Count > index)
            {
                Collection.Insert(index, item);
                OnAddToCollection(item, index);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index was out of collection range");
            }
        }
    }

    public int Replace(int index, T item)
    {
        lock (Locker)
        {
            if (Collection.Count < index || index < 0)
            {
                Collection.Add(item);
                OnAddToCollection(item, Collection.IndexOf(item));
                return Collection.IndexOf(item);
            }

            T oldItem = Collection[index];
            Collection[index] = item;
            OnReplaceInCollection(item, oldItem, index);
            return index;
        }
    }

    /// <summary>
    ///     Removes the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    public bool Remove(T item)
    {
        lock (Locker)
        {
            int index = Collection.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            Collection.RemoveAt(index);
            OnRemoveFromCollection(item, index);

            return true;
        }
    }

    public void RemoveAt(int index)
    {
        lock (Locker)
        {
            if (index > -1 && index < Collection.Count)
            {
                T item = Collection[index];
                Collection.RemoveAt(index);
                OnRemoveFromCollection(item, index);
            }
        }
    }

    public (bool hasRemovedAny, IList<T> removed) RemoveWhere(Func<T, bool> predicate)
    {
        lock (Locker)
        {
            List<T> removed = null;
            var index = 0;

            for (int i = Collection.Count - 1; i > -1; i--)
            {
                T item = Collection[i];

                if (predicate(item))
                {
                    (removed ?? (removed = new List<T>())).Add(item);
                    Collection.RemoveAt(i);
                    index = i;
                }
            }

            if (removed != null)
            {
                if (UseResetOnBulkOperations)
                {
                    OnCollectionReset();
                }
                else
                {
                    OnRemoveFromCollection(removed, index);
                }
            }

            return (removed != null, removed);
        }
    }

    public void CopyTo(Array array, int index)
    {
        lock (Locker)
        {
            ((ICollection) Collection).CopyTo(array, index);
        }
    }

    public int Add(object value)
    {
        var item = (T) value;

        lock (Locker)
        {
            Collection.Add(item);
            OnAddToCollection(item, Collection.IndexOf(item));
            return Collection.Count;
        }
    }

    public bool Contains(object value)
    {
        return Contains((T) value);
    }

    public int IndexOf(object value)
    {
        return IndexOf((T) value);
    }

    public void Insert(int index, object value)
    {
        Insert(index, (T) value);
    }

    public void Remove(object value)
    {
        Remove((T) value);
    }

    public void ReplaceRange(IEnumerable<T> collection)
    {
        if (collection != null)
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

    protected override string[] GetPropertyChangedArgs()
    {
        return PropertyChangedArgs;
    }

    private void InternalAddRange(TColl coll, IEnumerable<T> collection)
    {
        int startingIndex = coll.Count;
        List<T> added = null;

        foreach (T item in collection)
        {
            (added ?? (added = new List<T>())).Add(item);
            coll.Add(item);
        }

        if (added != null)
        {
            if (UseResetOnBulkOperations)
            {
                OnCollectionReset();
            }
            else
            {
                OnAddToCollection(added, startingIndex);
            }
        }
    }
}