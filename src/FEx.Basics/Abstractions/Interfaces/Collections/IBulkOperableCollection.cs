using System;
using System.Collections.Generic;

namespace FEx.Basics.Abstractions.Interfaces.Collections;

public interface IBulkOperableCollection<T>
{
    void AddRange(IEnumerable<T> collection);
    (bool hasRemovedAny, IList<T> removed) RemoveWhere(Func<T, bool> predicate);
}