using System.Collections.Generic;
using System.Diagnostics;

namespace FEx.Agnostics.Collections.Concurrent;

public sealed class ListDebugView<T>
{
    private readonly IList<T> _collection;

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

    // The constructor for the type proxy class must have a
    // constructor that takes the target type as a parameter.
    public ListDebugView(IList<T> collection)
    {
        if (collection is not null)
            _collection = collection;
    }
}