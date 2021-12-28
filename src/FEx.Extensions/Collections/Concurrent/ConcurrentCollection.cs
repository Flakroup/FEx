using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FEx.Extensions.Collections.Concurrent;

[DebuggerDisplay("Count={" + nameof(Count) + "}")]
[DebuggerTypeProxy(typeof(ListDebugView<>))]
[Serializable]
public class ConcurrentCollection<T> : BaseConcurrentCollection<Collection<T>, T>
{
    public ConcurrentCollection(
        IEnumerable<T> collection = null,
        bool useBaseConstructor = true,
        bool passIndexOfRemovedItem = false,
        bool useResetOnBulkOperations = true)
        : base(collection, useBaseConstructor, passIndexOfRemovedItem, useResetOnBulkOperations)
    {
    }

    public ConcurrentCollection()
        : this(null)
    {
    }
}