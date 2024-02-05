using System.Collections.Generic;

namespace FEx.Basics.Abstractions.Interfaces.Collections;

public interface IUniqueCollection<in T>
{
    bool AddUnique(T item, bool notifyOfChange = true);
    void AddUniqueRange(IEnumerable<T> collection);
}