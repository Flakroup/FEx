using System;
using System.Collections;
using System.Collections.Generic;

namespace FEx.Agnostics.Collections.Concurrent;

public partial class ConcurrentList<T>
{
    /// <summary>Gets a value indicating whether access to the underlying list is synchronized, as reported by the list itself.</summary>
    public bool IsSynchronized => ((ICollection)Items).IsSynchronized;

    /// <summary>Gets a value indicating whether the underlying list has a fixed size.</summary>
    public bool IsFixedSize => ((IList)Items).IsFixedSize;

    /// <summary>Gets the underlying list as the object to synchronize on.</summary>
    public object SyncRoot => Items;

    /// <summary>Gets a value indicating whether the underlying list is read-only.</summary>
    public bool IsReadOnly => ((IList)Items).IsReadOnly;

    object? IList.this[int index]
    {
        get => this[index];
        // IList indexer set: value is unboxed to T (null into a value-type list throws, matching IList semantics).
        set => this[index] = (T)value!;
    }

    /// <inheritdoc cref="List{T}.CopyTo(T[])" />
    public void CopyTo(Array array, int index) => Read(() => ((ICollection)Items).CopyTo(array, index));

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc cref="List{T}.Contains" />
    public bool Contains(object? value) => Contains((T)value!);

    /// <inheritdoc cref="List{T}.IndexOf(T)" />
    public int IndexOf(object? value) => IndexOf((T)value!);

    /// <inheritdoc cref="List{T}.Insert" />
    public void Insert(int index, object? value) => Insert(index, (T)value!);

    /// <inheritdoc cref="List{T}.Remove" />
    public void Remove(object? value) => Remove((T)value!);

    /// <summary>Runs the action while holding the read lock.</summary>
    /// <param name="action">The action to run.</param>
    public void Read(Action action) => _lock.Read(action);

    /// <summary>Runs the function while holding the read lock and returns its result.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="action">The function to run.</param>
    /// <returns>The value returned by <paramref name="action"/>.</returns>
    public TResult Read<TResult>(Func<TResult> action) => _lock.ReadWithResult(action);

    /// <summary>Runs the action while holding the write lock.</summary>
    /// <param name="action">The action to run.</param>
    public void Write(Action action) => _lock.Write(action);

    /// <summary>Runs the function while holding the write lock and returns its result.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="action">The function to run.</param>
    /// <returns>The value returned by <paramref name="action"/>.</returns>
    public TResult Write<TResult>(Func<TResult> action) => _lock.WriteWithResult(action);
}