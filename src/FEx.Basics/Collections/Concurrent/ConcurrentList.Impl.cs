using System;
using System.Collections;
using System.Collections.Generic;

namespace FEx.Basics.Collections.Concurrent;

public partial class ConcurrentList<T>
{
    public bool IsSynchronized => ((ICollection)Items).IsSynchronized;

    public bool IsFixedSize => ((IList)Items).IsFixedSize;

    public object SyncRoot => Items;

    public bool IsReadOnly => ((IList)Items).IsReadOnly;

    object IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value;
    }

    /// <inheritdoc cref="List{T}.CopyTo(T[])" />
    public void CopyTo(Array array, int index) => Read(() => ((ICollection)Items).CopyTo(array, index));

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc cref="List{T}.Contains" />
    public bool Contains(object value) => Contains((T)value);

    /// <inheritdoc cref="List{T}.IndexOf(T)" />
    public int IndexOf(object value) => IndexOf((T)value);

    /// <inheritdoc cref="List{T}.Insert" />
    public void Insert(int index, object value) => Insert(index, (T)value);

    /// <inheritdoc cref="List{T}.Remove" />
    public void Remove(object value) => Remove((T)value);

    public void Read(Action action) => _lock.Read(action);

    public TResult Read<TResult>(Func<TResult> action) => _lock.ReadWithResult(action);

    public void Write(Action action) => _lock.Write(action);

    public TResult Write<TResult>(Func<TResult> action) => _lock.WriteWithResult(action);
}