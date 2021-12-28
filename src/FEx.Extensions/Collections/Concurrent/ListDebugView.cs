using System.Diagnostics;

namespace FEx.Extensions.Collections.Concurrent;

public sealed class ListDebugView<T>
{
    private readonly IList<T> _collection;

    // The constructor for the type proxy class must have a
    // constructor that takes the target type as a parameter.
    public ListDebugView(IList<T> collection)
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