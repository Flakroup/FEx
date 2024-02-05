// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using FEx.Basics.Abstractions.Collections;
using FEx.Basics.Abstractions.Interfaces.Collections;
using FEx.Basics.Utilities.Collections;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace FEx.Basics.Collections;

/// <summary>
///     A hash set that implements the interfaces required for Entity Framework to use notification based change tracking
///     for a collection navigation property.
/// </summary>
/// <typeparam name="T"> The type of elements in the hash set. </typeparam>
/// [DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[Serializable]
public class ObservableHashSet<T> : BaseObservableCollection<T>, ISet<T>, IReadOnlyCollection<T>, IChangeableCollection
{
    protected static readonly string[] PropertyChangedArgs = { nameof(Count) };

    private HashSet<T> _set;

    /// <summary>
    ///     Event raised when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler CollectionChanged
    {
        add => Notifier.CollectionChanged += value;
        remove => Notifier.CollectionChanged -= value;
    }

    /// <summary>
    ///     Event raised when a property on the collection changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged
    {
        add => Notifier.PropertyChanged += value;
        remove => Notifier.PropertyChanged -= value;
    }

    /// <summary>
    ///     Gets the number of elements that are contained in the hash set.
    /// </summary>
    public virtual int Count => _set.Count;

    /// <summary>
    ///     Gets a value indicating whether the hash set is read-only.
    /// </summary>
    public virtual bool IsReadOnly => ((ICollection<T>)_set).IsReadOnly;

    /// <summary>
    ///     Gets the <see cref="IEqualityComparer{T}" /> object that is used to determine equality for the values in the set.
    /// </summary>
    public virtual IEqualityComparer<T> Comparer => _set.Comparer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ObservableHashSet{T}" /> class
    ///     that is empty and uses the default equality comparer for the set type.
    /// </summary>
    public ObservableHashSet()
        : this(comparer: EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ObservableHashSet{T}" /> class
    ///     that uses the specified equality comparer for the set type, contains elements
    ///     copied from the specified collection, and has sufficient capacity to accommodate
    ///     the number of elements copied.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new set.</param>
    /// <param name="comparer">
    ///     The <see cref="IEqualityComparer{T}" /> implementation to use when
    ///     comparing values in the set, or null to use the default <see cref="IEqualityComparer{T}" />
    ///     implementation for the set type.
    /// </param>
    /// <param name="notifyOnCreationContext">True if should notify on main thread context</param>
    /// <param name="passIndexOfRemovedItem">if set to <c>true</c> [pass index of removed item].</param>
    public ObservableHashSet(IEnumerable<T> collection = null,
                             IEqualityComparer<T> comparer = null,
                             bool notifyOnCreationContext = false,
                             bool passIndexOfRemovedItem = false,
                             bool sendAsyncEvents = true)
        : base(passIndexOfRemovedItem, sendAsyncEvents)
    {
        comparer ??= EqualityComparer<T>.Default;

        _set = collection is null
            ? new HashSet<T>(comparer)
            : new HashSet<T>(collection, comparer);

        SetNotifyOnCreationContext(notifyOnCreationContext);
    }

    void ICollection<T>.Add(T item)
    {
        Add(item);
    }

    /// <summary>
    ///     Removes all elements from the hash set.
    /// </summary>
    public virtual void Clear()
    {
        if (_set.Count == 0)
            return;

        var removed = this.ToList();

        _set.Clear();

        OnCollectionChanged(ObservableHashSetSingletons.NoItems, removed);
    }

    /// <summary>
    ///     Determines whether the hash set object contains the
    ///     specified element.
    /// </summary>
    /// <param name="item">The element to locate in the hash set.</param>
    /// <returns>
    ///     True if the hash set contains the specified element; otherwise, false.
    /// </returns>
    public virtual bool Contains(T item) => _set.Contains(item);

    /// <summary>
    ///     Copies the elements of the hash set to an array, starting at the specified array index.
    /// </summary>
    /// <param name="array">
    ///     The one-dimensional array that is the destination of the elements copied from
    ///     the hash set. The array must have zero-based indexing.
    /// </param>
    /// <param name="arrayIndex"> The zero-based index in array at which copying begins. </param>
    public virtual void CopyTo(T[] array, int arrayIndex)
    {
        _set.CopyTo(array, arrayIndex);
    }

    /// <summary>
    ///     Removes the specified element from the hash set.
    /// </summary>
    /// <param name="item"> The element to remove. </param>
    /// <returns>
    ///     True if the element is successfully found and removed; otherwise, false.
    /// </returns>
    public virtual bool Remove(T item)
    {
        if (!_set.Contains(item))
            return false;

        _set.Remove(item);

        OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);

        return true;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     Adds the specified element to the hash set.
    /// </summary>
    /// <param name="item"> The element to add to the set. </param>
    /// <returns>
    ///     true if the element is added to the hash set; false if the element is already present.
    /// </returns>
    public virtual bool Add(T item)
    {
        if (_set.Contains(item))
            return false;

        _set.Add(item);

        OnCollectionChanged(NotifyCollectionChangedAction.Add, item);

        return true;
    }

    /// <summary>
    ///     Modifies the hash set to contain all elements that are present in itself, the specified collection, or both.
    /// </summary>
    /// <param name="other"> The collection to compare to the current hash set. </param>
    public virtual void UnionWith(IEnumerable<T> other)
    {
        var copy = new HashSet<T>(_set, _set.Comparer);

        copy.UnionWith(other);

        if (copy.Count == _set.Count)
            return;

        var added = copy.Where(i => !_set.Contains(i)).ToList();

        _set = copy;

        OnCollectionChanged(added, ObservableHashSetSingletons.NoItems);
    }

    /// <summary>
    ///     Modifies the current hash set to contain only
    ///     elements that are present in that object and in the specified collection.
    /// </summary>
    /// <param name="other"> The collection to compare to the current hash set. </param>
    public virtual void IntersectWith(IEnumerable<T> other)
    {
        var copy = new HashSet<T>(_set, _set.Comparer);

        copy.IntersectWith(other);

        if (copy.Count == _set.Count)
            return;

        var removed = _set.Where(i => !copy.Contains(i)).ToList();

        _set = copy;

        OnCollectionChanged(ObservableHashSetSingletons.NoItems, removed);
    }

    /// <summary>
    ///     Removes all elements in the specified collection from the hash set.
    /// </summary>
    /// <param name="other"> The collection of items to remove from the current hash set. </param>
    public virtual void ExceptWith(IEnumerable<T> other)
    {
        var copy = new HashSet<T>(_set, _set.Comparer);

        copy.ExceptWith(other);

        if (copy.Count == _set.Count)
            return;

        var removed = _set.Where(i => !copy.Contains(i)).ToList();

        _set = copy;

        OnCollectionChanged(ObservableHashSetSingletons.NoItems, removed);
    }

    /// <summary>
    ///     Modifies the current hash set to contain only elements that are present either in that
    ///     object or in the specified collection, but not both.
    /// </summary>
    /// <param name="other"> The collection to compare to the current hash set. </param>
    public virtual void SymmetricExceptWith(IEnumerable<T> other)
    {
        var copy = new HashSet<T>(_set, _set.Comparer);

        copy.SymmetricExceptWith(other);

        var removed = _set.Where(i => !copy.Contains(i)).ToList();
        var added = copy.Where(i => !_set.Contains(i)).ToList();

        if (removed.Count == 0
            && added.Count == 0)
            return;

        _set = copy;

        OnCollectionChanged(added, removed);
    }

    /// <summary>
    ///     Determines whether the hash set is a subset of the specified collection.
    /// </summary>
    /// <param name="other"> The collection to compare to the current hash set. </param>
    /// <returns>
    ///     True if the hash set is a subset of other; otherwise, false.
    /// </returns>
    public virtual bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

    /// <summary>
    ///     Determines whether the hash set is a proper subset of the specified collection.
    /// </summary>
    /// <param name="other"> The collection to compare to the current hash set. </param>
    /// <returns>
    ///     True if the hash set is a proper subset of other; otherwise, false.
    /// </returns>
    public virtual bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

    /// <summary>
    ///     Determines whether the hash set is a superset of the specified collection.
    /// </summary>
    /// <param name="other"> The collection to compare to the current hash set. </param>
    /// <returns>
    ///     True if the hash set is a superset of other; otherwise, false.
    /// </returns>
    public virtual bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

    /// <summary>
    ///     Determines whether the hash set is a proper superset of the specified collection.
    /// </summary>
    /// <param name="other"> The collection to compare to the current hash set. </param>
    /// <returns>
    ///     True if the hash set is a proper superset of other; otherwise, false.
    /// </returns>
    public virtual bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

    /// <summary>
    ///     Determines whether the current System.Collections.Generic.HashSet`1 object and a specified collection share common
    ///     elements.
    /// </summary>
    /// <param name="other"> The collection to compare to the current hash set. </param>
    /// <returns>
    ///     True if the hash set and other share at least one common element; otherwise, false.
    /// </returns>
    public virtual bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

    /// <summary>
    ///     Determines whether the hash set and the specified collection contain the same elements.
    /// </summary>
    /// <param name="other"> The collection to compare to the current hash set. </param>
    /// <returns>
    ///     True if the hash set is equal to other; otherwise, false.
    /// </returns>
    public virtual bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

    /// <summary>
    ///     Returns an enumerator that iterates through the hash set.
    /// </summary>
    /// <returns>
    ///     An enumerator for the hash set.
    /// </returns>
    public virtual HashSet<T>.Enumerator GetEnumerator() => _set.GetEnumerator();

    /// <summary>
    ///     Copies the elements of the hash set to an array.
    /// </summary>
    /// <param name="array">
    ///     The one-dimensional array that is the destination of the elements copied from
    ///     the hash set. The array must have zero-based indexing.
    /// </param>
    public virtual void CopyTo([NotNull] T[] array)
    {
        _set.CopyTo(array);
    }

    /// <summary>
    ///     Copies the specified number of elements of the hash set to an array, starting at the specified array index.
    /// </summary>
    /// <param name="array">
    ///     The one-dimensional array that is the destination of the elements copied from
    ///     the hash set. The array must have zero-based indexing.
    /// </param>
    /// <param name="arrayIndex"> The zero-based index in array at which copying begins. </param>
    /// <param name="count"> The number of elements to copy to array. </param>
    public virtual void CopyTo([NotNull] T[] array, int arrayIndex, int count)
    {
        _set.CopyTo(array, arrayIndex, count);
    }

    /// <summary>
    ///     Removes all elements that match the conditions defined by the specified predicate
    ///     from the hash set.
    /// </summary>
    /// <param name="match">
    ///     The <see cref="Predicate{T}" /> delegate that defines the conditions of the elements to remove.
    /// </param>
    /// <returns> The number of elements that were removed from the hash set. </returns>
    public virtual int RemoveWhere([NotNull] Predicate<T> match)
    {
        var copy = new HashSet<T>(_set, _set.Comparer);

        int removedCount = copy.RemoveWhere(match);

        if (removedCount == 0)
            return 0;

        var removed = _set.Where(i => !copy.Contains(i)).ToList();

        _set = copy;

        OnCollectionChanged(ObservableHashSetSingletons.NoItems, removed);

        return removedCount;
    }

    /// <summary>
    ///     Sets the capacity of the hash set to the actual number of elements it contains, rounded up to a nearby,
    ///     implementation-specific value.
    /// </summary>
    public virtual void TrimExcess()
    {
        _set.TrimExcess();
    }

    protected override string[] GetPropertyChangedArgs() => PropertyChangedArgs;
}