using System;
using System.Collections.Generic;

namespace FEx.Agnostics.Collections;

public class SummarizedPaginatedList<T, TSum> : PaginatedList<T>
{
    public new static SummarizedPaginatedList<T, TSum> Empty => new(new List<T>(), 0, 0, 0, _ => default);

    public TSum Summary { get; }

    public SummarizedPaginatedList(IList<T> items,
                                   int totalItemsCount,
                                   int pageIndex,
                                   int totalPages,
                                   Func<IList<T>, TSum> sumFunc)
        : base(items, totalItemsCount, pageIndex, totalPages)
    {
        Summary = sumFunc(items);
    }
}