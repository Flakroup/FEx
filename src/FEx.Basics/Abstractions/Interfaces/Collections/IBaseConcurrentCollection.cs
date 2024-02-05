using System;
using System.Collections;
using System.Collections.Generic;

namespace FEx.Basics.Abstractions.Interfaces.Collections;

public interface IBaseConcurrentCollection<out TColl, T> : IList<T>, IBulkOperableCollection<T>, IUniqueCollection<T>,
    IReadOnlyList<T>, IList where TColl : class, IList<T>
{
    void DoBulkOperation(Action<TColl> action, Func<TColl, bool> triggerCollectionChanged);
    TR DoBulkOperation<TR>(Func<TColl, TR> func, Func<TColl, bool> triggerCollectionChanged);
    int Replace(int index, T item);
}