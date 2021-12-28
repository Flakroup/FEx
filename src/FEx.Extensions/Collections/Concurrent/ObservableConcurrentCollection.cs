using FEx.Extensions.Collections.Concurrent.Notifiers;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace FEx.Extensions.Collections.Concurrent;

/// <summary>
///     Provides a thread-safe, concurrent collection for use with data binding.
///     Based on https://github.com/ChadBurggraf/parallel-extensions-extras
/// </summary>
/// <typeparam name="T">Specifies the type of the elements in this collection.</typeparam>
/// <seealso cref="System.Collections.Specialized.INotifyCollectionChanged" />
/// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(ListDebugView<>))]
[Serializable]
public class ObservableConcurrentCollection<T> : ConcurrentCollection<T>, IChangeableCollection
{
    /// <summary>
    ///     Initializes an instance of the ObservableConcurrentCollection class with the specified
    ///     collection as the underlying data structure.
    /// </summary>
    public ObservableConcurrentCollection(
        IEnumerable<T> collection = null,
        bool useBaseConstructor = true,
        bool notifyOnCreationContext = false,
        bool passIndexOfRemovedItem = false,
        bool useResetOnBulkOperations = true)
        : base(collection, useBaseConstructor, passIndexOfRemovedItem, useResetOnBulkOperations)
    {
        SetNotifyOnCreationContext(notifyOnCreationContext);
    }

    /// <summary>
    ///     For serialization purposes
    /// </summary>
    public ObservableConcurrentCollection()
        : this(notifyOnCreationContext: false)
    {
    }

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
}