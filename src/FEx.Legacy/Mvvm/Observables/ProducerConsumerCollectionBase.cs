using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FEx.Legacy.Mvvm.Observables;

/// <summary>
///     Provides a base implementation for producer-consumer collections that wrap other
///     producer-consumer collections.
///     Based on https://github.com/ChadBurggraf/parallel-extensions-extras
/// </summary>
/// <typeparam name="T">Specifies the type of elements in the collection.</typeparam>
/// <seealso cref="System.Collections.Concurrent.IProducerConsumerCollection{T}" />
[Serializable]
public abstract class ProducerConsumerCollectionBase<T> : IProducerConsumerCollection<T>
{
    /// <summary>
    ///     Gets the number of elements contained in the collection.
    /// </summary>
    public int Count => ContainedCollection.Count;

    /// <summary>
    ///     Gets the contained collection.
    /// </summary>
    /// <value>
    ///     The contained collection.
    /// </value>
    protected IProducerConsumerCollection<T> ContainedCollection { get; }

    /// <summary>
    ///     Gets whether the collection is synchronized.
    /// </summary>
    bool ICollection.IsSynchronized => ContainedCollection.IsSynchronized;

    /// <summary>
    ///     Gets the synchronization root object for the collection.
    /// </summary>
    object ICollection.SyncRoot => ContainedCollection.SyncRoot;

    /// <summary>
    ///     Initializes the ProducerConsumerCollectionBase instance.
    /// </summary>
    /// <param name="contained">The collection to be wrapped by this instance.</param>
    /// <exception cref="ArgumentNullException">contained</exception>
    protected ProducerConsumerCollectionBase(IProducerConsumerCollection<T> contained)
    {
        ContainedCollection = contained ?? throw new ArgumentNullException(nameof(contained));
    }

    /// <summary>
    ///     Copies the contents of the collection to an array.
    /// </summary>
    /// <param name="array">The array to which the data should be copied.</param>
    /// <param name="index">The starting index at which data should be copied.</param>
    void ICollection.CopyTo(Array array, int index) => ContainedCollection.CopyTo(array, index);

    /// <summary>
    ///     Gets an enumerator for the collection.
    /// </summary>
    /// <returns>
    ///     An enumerator.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     Gets an enumerator for the collection.
    /// </summary>
    /// <returns>
    ///     An enumerator.
    /// </returns>
    public IEnumerator<T> GetEnumerator() => ContainedCollection.GetEnumerator();

    /// <summary>
    ///     Attempts to add the specified value to the end of the deque.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>
    ///     true if the item could be added; otherwise, false.
    /// </returns>
    bool IProducerConsumerCollection<T>.TryAdd(T item) => TryAdd(item);

    /// <summary>
    ///     Attempts to remove and return an item from the collection.
    /// </summary>
    /// <param name="item">
    ///     When this method returns, if the operation was successful, item contains the item removed. If
    ///     no item was available to be removed, the value is unspecified.
    /// </param>
    /// <returns>
    ///     true if an element was removed and returned from the collection; otherwise, false.
    /// </returns>
    bool IProducerConsumerCollection<T>.TryTake(out T item) => TryTake(out item);

    /// <summary>
    ///     Creates an array containing the contents of the collection.
    /// </summary>
    /// <returns>
    ///     The array.
    /// </returns>
    public T[] ToArray() => [.. ContainedCollection];

    /// <summary>
    ///     Copies the contents of the collection to an array.
    /// </summary>
    /// <param name="array">The array to which the data should be copied.</param>
    /// <param name="index">The starting index at which data should be copied.</param>
    public void CopyTo(T[] array, int index) => ContainedCollection.CopyTo(array, index);

    /// <summary>
    ///     Attempts to add the specified value to the end of the deque.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>
    ///     true if the item could be added; otherwise, false.
    /// </returns>
    protected virtual bool TryAdd(T item) => ContainedCollection.TryAdd(item);

    /// <summary>
    ///     Attempts to remove and return an item from the collection.
    /// </summary>
    /// <param name="item">
    ///     When this method returns, if the operation was successful, item contains the item removed. If
    ///     no item was available to be removed, the value is unspecified.
    /// </param>
    /// <returns>
    ///     true if an element was removed and returned from the collection; otherwise, false.
    /// </returns>
    protected virtual bool TryTake(out T item) => ContainedCollection.TryTake(out item);
}