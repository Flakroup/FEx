using FEx.Agnostics.Abstractions.Extensions;
using System.Collections.Generic;

namespace FEx.Agnostics.Collections;

/// <summary>Represents one page of results together with the paging information needed to navigate to others.</summary>
/// <typeparam name="T">The type of the items on the page.</typeparam>
public class PaginatedList<T>
{
    /// <summary>Gets a new page with no items and all counters set to zero.</summary>
    public static PaginatedList<T> Empty => new([], 0, 0, 0);

    /// <summary>Gets the items on this page.</summary>
    public IList<T> Items { get; }
    /// <summary>Gets the one-based index of this page.</summary>
    public int PageIndex { get; }
    /// <summary>Gets the total number of pages available.</summary>
    public int TotalPages { get; }
    /// <summary>Gets the total number of items across all pages.</summary>
    public int TotalItemsCount { get; }
    /// <summary>Gets a value indicating whether a page precedes this one.</summary>
    public bool HasPreviousPage => PageIndex > 1;
    /// <summary>Gets a value indicating whether a page follows this one.</summary>
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>Initializes a page with its items and paging information.</summary>
    /// <param name="items">The items on this page.</param>
    /// <param name="totalItemsCount">The total number of items across all pages.</param>
    /// <param name="pageIndex">The one-based index of this page.</param>
    /// <param name="totalPages">The total number of pages available.</param>
    public PaginatedList(IList<T> items, int totalItemsCount, int pageIndex, int totalPages)
    {
        PageIndex = pageIndex;
        TotalItemsCount = totalItemsCount;
        TotalPages = totalPages;
        Items = items.GuardProperty();
    }
}