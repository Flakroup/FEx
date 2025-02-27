using System.Collections.Generic;

namespace FEx.Basics.Collections;

public class PaginatedList<T>
{
    public static PaginatedList<T> Empty => new([], 0, 0, 0);

    public IList<T> Items { get; }
    public int PageIndex { get; }
    public int TotalPages { get; }
    public int TotalItemsCount { get; }
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PaginatedList(IList<T> items, int totalItemsCount, int pageIndex, int totalPages)
    {
        PageIndex = pageIndex;
        TotalItemsCount = totalItemsCount;
        TotalPages = totalPages;

        if (items is not null)
            Items = items;
    }
}