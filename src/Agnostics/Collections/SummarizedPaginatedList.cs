using System;
using System.Collections.Generic;

namespace FEx.Agnostics.Collections;

public class SummarizedPaginatedList<T, TSum> : PaginatedList<T>
{
    // Empty sentinel: default(TSum) is the intended empty summary (may be null for reference TSum).
    public new static SummarizedPaginatedList<T, TSum> Empty => new(new List<T>(), 0, 0, 0, static _ => default!);

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