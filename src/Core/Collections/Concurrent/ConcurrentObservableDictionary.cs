using FEx.Agnostics.Abstractions.Collections.Concurrent;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace FEx.Core.Collections.Concurrent;

/// <summary>
/// Based on https://github.com/ChadBurggraf/parallel-extensions-extras
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <seealso cref="IDictionary{TKey,TValue}" />
/// <seealso cref="INotifyCollectionChanged" />
/// <seealso cref="INotifyPropertyChanged" />
[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[Serializable]
public class ConcurrentObservableDictionary<TKey, TValue> : BaseConcurrentList<KeyValuePair<TKey, TValue>>,
    IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>, INotifyCollectionChanged,
    INotifyPropertyChanged
{
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary;

    [NonSerialized]
    private readonly IFExDispatcher _dispatcher;

    /// <summary>
    /// Occurs when the collection changes, either by adding or removing an item.
    /// </summary>
    [field: NonSerialized]
    public event NotifyCollectionChangedEventHandler CollectionChanged;

    /// <summary>
    /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    [field: NonSerialized]
    public event PropertyChangedEventHandler PropertyChanged;

    public int Count => _dictionary.Count;
    public bool IsSynchronized => ((ICollection)_dictionary).IsSynchronized;
    public object SyncRoot => ((ICollection)_dictionary).SyncRoot;
    public bool IsFixedSize => ((IDictionary)_dictionary).IsFixedSize;
    public bool IsReadOnly => ((IDictionary)_dictionary).IsReadOnly;
    public ICollection<TKey> Keys => _dictionary.Keys;
    public ICollection<TValue> Values => _dictionary.Values;

    public object this[object key]
    {
        get => this[key.GetObject<TKey>()];
        set => this[key.GetObject<TKey>()] = value.GetObject<TValue>();
    }

    public TValue this[TKey key]
    {
        get => _dictionary[key];
        set => UpdateWithNotification(key, value);
    }

    int ICollection<KeyValuePair<TKey, TValue>>.Count => Count;
    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => IsReadOnly;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    ICollection IDictionary.Values => (ICollection)Values;
    ICollection IDictionary.Keys => (ICollection)Keys;

    public ConcurrentObservableDictionary()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _dispatcher = FExCoreStatics.Dispatcher;
#pragma warning restore CS0618 // Type or member is obsolete

        _dictionary = new();
    }

    public void CopyTo(Array array, int index) => ((ICollection)_dictionary).CopyTo(array, index);

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => TryAdd(item.Key, item.Value);

    void ICollection<KeyValuePair<TKey, TValue>>.Clear() => Clear();

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
        ValueIsEqual(item.Key, item.Value);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);

    public void Add(object key, object value) => TryAdd(key.GetObject<TKey>(), value.GetObject<TValue>());

    public bool Contains(object key) => ContainsKey(key.GetObject<TKey>());

    public IDictionaryEnumerator GetEnumerator() => ((IDictionary)_dictionary).GetEnumerator();

    public void Remove(object key) => Remove(key.GetObject<TKey>());

    public void Clear()
    {
        _dictionary.Clear();
        OnCollectionReset();
    }

    public void Add(TKey key, TValue value) => TryAdd(key, value);

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool Remove(TKey key)
    {
        var flag = _dictionary.TryRemove(key, out var val);

        if (flag)
            OnRemoveFromCollection(new KeyValuePair<TKey, TValue>(key, val), -1);

        return flag;
    }

    public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
        _dictionary.GetEnumerator();

    /// <summary>
    /// Attempts to add the specified key and value to the <see cref="ConcurrentDictionary{TKey, TValue}" />.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">
    /// The value of the element to add. The value can be a null reference (Nothing
    /// in Visual Basic) for reference types.
    /// </param>
    /// <returns>
    /// true if the key/value pair was added to the <see cref="ConcurrentDictionary{TKey, TValue}" />
    /// successfully; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> is null reference
    /// (Nothing in Visual Basic).
    /// </exception>
    /// <exception cref="OverflowException">
    /// The <see cref="ConcurrentDictionary{TKey, TValue}" />
    /// contains too many elements.
    /// </exception>
    public bool TryAdd(TKey key, TValue value)
    {
        var flag = _dictionary.TryAdd(key, value);

        if (flag)
            OnAddToCollection(new(key, value), -1);

        return flag;
    }

    /// <summary>
    /// Uses the specified functions to add a key/value pair to the
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key does not already exist, or to
    /// update a key/value pair in the <see cref="System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key
    /// already exists.
    /// </summary>
    /// <param name="key">The key to be added or whose value should be updated</param>
    /// <param name="addValueFactory">The function used to generate a value for an absent key</param>
    /// <param name="updateValueFactory">
    /// The function used to generate a new value for an existing key based on the key's
    /// existing value
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" />, <paramref name="addValueFactory" />, or <paramref name="updateValueFactory" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="OverflowException">The dictionary contains too many elements.</exception>
    /// <returns>
    /// The new value for the key. This will be either be the result of <paramref name="addValueFactory" /> (if the
    /// key was absent) or the result of <paramref name="updateValueFactory" /> (if the key was present).
    /// </returns>
    public TValue AddOrUpdate(TKey key,
                              Func<TKey, TValue> addValueFactory,
                              Func<TKey, TValue, TValue> updateValueFactory)
    {
        var wasUpdated = false;
        var capturedOldValue = default(TValue);

        var value = _dictionary.AddOrUpdate(key,
            addValueFactory,
            (k, existing) =>
            {
                wasUpdated = true;
                capturedOldValue = existing;

                return updateValueFactory(k, existing);
            });

        if (wasUpdated)
            OnReplaceInCollection(new(key, value), new(key, capturedOldValue), -1);
        else
            OnAddToCollection(new(key, value), -1);

        return value;
    }

    protected virtual void Dispatch(Action action) => _dispatcher.InvokeOnMainThread(action, this);

    /// <summary>
    /// Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (EventsAreSuppressed || PropertyChanged is null)
            return;

        Dispatch(() => PropertyChanged.HandlePropertyChanged(this, e));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (EventsAreSuppressed || CollectionChanged is null)
            return;

        Dispatch(() => CollectionChanged.Invoke(this, e));
    }

    /// <inheritdoc />
    protected override void OnIndexerPropertyChanged()
    {
        base.OnIndexerPropertyChanged();
        OnPropertyChanged(new(nameof(Keys)));
        OnPropertyChanged(new(nameof(Values)));
    }

    private bool ValueIsEqual(TKey key, TValue val) =>
        _dictionary.TryGetValue(key, out var existing) && EqualityHelper.IsEqual(ref val, existing);

    private void UpdateWithNotification(TKey key, TValue value)
    {
        var (hasBeenReplaced, removedValue, newValue) = _dictionary.AddOrReplaceValue(key, () => value);

        if (hasBeenReplaced)
            OnReplaceInCollection(new(key, value), new(key, removedValue), -1);
        else
            OnAddToCollection(new(key, value), -1);
    }
}