using FEx.Agnostics.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Agnostics.Extensions;

/// <summary>Contains extension methods for <see cref="IList{T}"/>.</summary>
public static class ListExtensions
{
    /// <summary>Extracts one page from a list and wraps it with paging information.</summary>
    /// <typeparam name="T">The type of the list elements.</typeparam>
    /// <param name="items">The full list to page.</param>
    /// <param name="itemsPerPage">The maximum number of items on a page.</param>
    /// <param name="page">The one-based page number to extract.</param>
    /// <returns>The requested page, or <see langword="null"/> when the list is not empty and the page lies outside it.</returns>
    public static PaginatedList<T>? MakePaginatedList<T>(this IList<T> items, int itemsPerPage, int page)
    {
        var itemsToSkip = (page - 1) * itemsPerPage;

        if (items.Count > 0
            && (itemsToSkip > items.Count || itemsToSkip < 0))
            return null;

        var totalPagesCount = (int)Math.Ceiling(items.Count / (double)itemsPerPage);
        IList<T> itemsForThisPage = [.. items.Skip(itemsToSkip).Take(itemsPerPage)];

        return new(itemsForThisPage, items.Count, page, totalPagesCount);
    }
}