using FEx.Basics.Abstractions.Collections;
using FEx.Basics.Abstractions.Interfaces.Collections;
using FEx.Extensions;
using FEx.Extensions.Base.Helpers;
using FEx.Extensions.Collections.Dictionaries;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace FEx.Basics.Collections.Concurrent;

/// <summary>
///     Based on https://github.com/ChadBurggraf/parallel-extensions-extras
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <seealso cref="System.Collections.Generic.IDictionary{TKey, TValue}" />
/// <seealso cref="System.Collections.Specialized.INotifyCollectionChanged" />
/// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
[Serializable]
public class ObservableConcurrentDictionary<TKey, TValue> : BaseObservableCollection<TValue>, IChangeableCollection,
    IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>
{
    protected static readonly string[] PropertyChangedArgs = [nameof(Count), nameof(Keys), nameof(Values)];

    [NonSerialized] private readonly ConcurrentDictionary<TKey, TValue> _dictionary;

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

    private string DebuggerDisplay => $"Count={Count}";
    int ICollection<KeyValuePair<TKey, TValue>>.Count => Count;
    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => IsReadOnly;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    ICollection IDictionary.Values => (ICollection)Values;
    ICollection IDictionary.Keys => (ICollection)Keys;

    public ObservableConcurrentDictionary(bool notifyOnCreationContext,
                                          bool passIndexOfRemovedItem = false,
                                          bool sendAsyncEvents = true)
        : base(passIndexOfRemovedItem, sendAsyncEvents)
    {
        _dictionary = new ConcurrentDictionary<TKey, TValue>();
        SetNotifyOnCreationContext(notifyOnCreationContext);
    }

    public ObservableConcurrentDictionary()
        : this(false)
    {
    }

    public void CopyTo(Array array, int index)
    {
        ((ICollection)_dictionary).CopyTo(array, index);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        TryAdd(item.Key, item.Value);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Clear()
    {
        Clear();
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
        ValueIsEqual(item.Key, item.Value);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
    }

    public void Add(object key, object value)
    {
        TryAdd(key.GetObject<TKey>(), value.GetObject<TValue>());
    }

    public bool Contains(object key) => ContainsKey(key.GetObject<TKey>());

    public IDictionaryEnumerator GetEnumerator() => ((IDictionary)_dictionary).GetEnumerator();

    public void Remove(object key)
    {
        Remove(key.GetObject<TKey>());
    }

    public void Clear()
    {
        _dictionary.Clear();
        OnCollectionReset();
    }

    public void Add(TKey key, TValue value)
    {
        TryAdd(key, value);
    }

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool Remove(TKey key)
    {
        bool flag = _dictionary.TryRemove(key, out TValue val);

        if (flag)
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, val);

        return flag;
    }

    public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
        _dictionary.GetEnumerator();

    /// <summary>
    ///     Attempts to add the specified key and value to the <see cref="ConcurrentDictionary{TKey, TValue}" />.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">
    ///     The value of the element to add. The value can be a null reference (Nothing
    ///     in Visual Basic) for reference types.
    /// </param>
    /// <returns>
    ///     true if the key/value pair was added to the <see cref="ConcurrentDictionary{TKey, TValue}" />
    /// successfully; otherwise, false.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="key" /> is null reference
    ///     (Nothing in Visual Basic).
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The <see cref="ConcurrentDictionary{TKey, TValue}" />
    /// contains too many elements.
    /// </exception>
    public bool TryAdd(TKey key, TValue value)
    {
        bool flag = _dictionary.TryAdd(key, value);

        if (flag)
            OnCollectionChanged(NotifyCollectionChangedAction.Add, _dictionary[key]);

        return flag;
    }

    /// <summary>
    ///     Uses the specified functions to add a key/value pair to the
    /// <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key does not already exist, or to
    ///     update a key/value pair in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key
    ///     already exists.
    /// </summary>
    /// <param name="key">The key to be added or whose value should be updated</param>
    /// <param name="addValueFactory">The function used to generate a value for an absent key</param>
    /// <param name="updateValueFactory">
    ///     The function used to generate a new value for an existing key based on the key's
    ///     existing value
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="key" />, <paramref name="addValueFactory" />, or <paramref name="updateValueFactory" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
    /// <returns>
    ///     The new value for the key. This will be either be the result of <paramref name="addValueFactory" /> (if the
    ///     key was absent) or the result of <paramref name="updateValueFactory" /> (if the key was present).
    /// </returns>
    public TValue AddOrUpdate(TKey key,
                              Func<TKey, TValue> addValueFactory,
                              Func<TKey, TValue, TValue> updateValueFactory)
    {
        bool hasKey = _dictionary.TryGetValue(key, out TValue oldValue);
        TValue value = _dictionary.AddOrUpdate(key, addValueFactory, updateValueFactory);

        if (hasKey)
            OnCollectionChanged(NotifyCollectionChangedAction.Replace, value, oldValue);
        else
            OnCollectionChanged(NotifyCollectionChangedAction.Add, value);

        return value;
    }

    protected override string[] GetPropertyChangedArgs() => PropertyChangedArgs;

    private bool ValueIsEqual(TKey key, TValue val) =>
        _dictionary.ContainsKey(key) && EqualityHelper.IsEqual(ref val, _dictionary[key]);

    private void UpdateWithNotification(TKey key, TValue value)
    {
        (bool hasBeenReplaced, TValue removedValue, TValue newValue) = _dictionary.AddOrReplaceValue(key, () => value);

        OnCollectionChanged(hasBeenReplaced
                ? NotifyCollectionChangedAction.Replace
                : NotifyCollectionChangedAction.Add,
            newValue,
            removedValue);
    }
}