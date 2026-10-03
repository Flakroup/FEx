using FEx.Agnostics.Abstractions.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace FEx.Agnostics.Collections.Concurrent;

/// <summary>Debugger proxy that shows the items of a list as a flat array.</summary>
/// <typeparam name="T">The type of the list elements.</typeparam>
public sealed class ListDebugView<T>
{
    private readonly IList<T> _collection;

    /// <summary>Gets a snapshot copy of the list items for display in the debugger.</summary>
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
    /// <summary>Initializes the proxy for the given list.</summary>
    /// <param name="collection">The list to display.</param>
    public ListDebugView(IList<T> collection) => _collection = collection.GuardProperty();
}