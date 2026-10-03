using FEx.Agnostics.Abstractions.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace FEx.Agnostics.Collections.Concurrent;

/// <summary>Debugger proxy that shows the items of a collection as a flat array.</summary>
/// <typeparam name="T">The type of the collection elements.</typeparam>
public sealed class CollectionDebugView<T>
{
    private readonly ICollection<T> _collection;

    /// <summary>Gets a snapshot copy of the collection items for display in the debugger.</summary>
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

    /// <summary>Initializes the proxy for the given collection.</summary>
    /// <param name="collection">The collection to display.</param>
    public CollectionDebugView(ICollection<T> collection) => _collection = collection.GuardProperty();
}