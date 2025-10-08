using System.Collections.Generic;
using System.Diagnostics;

namespace FEx.Agnostics.Collections.Concurrent;

public sealed class CollectionDebugView<T>
{
    private readonly ICollection<T> _collection;

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

    public CollectionDebugView(ICollection<T> collection)
    {
        if (collection is not null)
            _collection = collection;
    }
}