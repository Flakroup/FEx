using System;
using System.Collections.Generic;

namespace FEx.Agnostics.Collections;

/// <summary>A page of results that also carries a summary value computed from the page items.</summary>
/// <typeparam name="T">The type of the items on the page.</typeparam>
/// <typeparam name="TSum">The type of the summary value.</typeparam>
public class SummarizedPaginatedList<T, TSum> : PaginatedList<T>
{
    // Empty sentinel: default(TSum) is the intended empty summary (may be null for reference TSum).
    /// <summary>Gets a new page with no items and a default summary.</summary>
    public new static SummarizedPaginatedList<T, TSum> Empty => new(new List<T>(), 0, 0, 0, static _ => default!);

    /// <summary>Gets the summary computed from the items on this page.</summary>
    public TSum Summary { get; }

    /// <summary>Initializes a page and computes its summary from the items.</summary>
    /// <param name="items">The items on this page.</param>
    /// <param name="totalItemsCount">The total number of items across all pages.</param>
    /// <param name="pageIndex">The one-based index of this page.</param>
    /// <param name="totalPages">The total number of pages available.</param>
    /// <param name="sumFunc">Computes the summary from <paramref name="items"/>.</param>
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