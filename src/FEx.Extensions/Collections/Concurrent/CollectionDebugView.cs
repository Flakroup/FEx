using System.Diagnostics;

namespace FEx.Extensions.Collections.Concurrent;

public sealed class CollectionDebugView<T>
{
    private readonly ICollection<T> _collection;

    public CollectionDebugView(ICollection<T> collection)
    {
        if (collection != null)
        {
            _collection = collection;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var items = new T[_collection.Count];
            _collection.CopyTo(items, 0);
            return items;
        }
    }
}