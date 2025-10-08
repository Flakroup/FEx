using FEx.Agnostics.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEx.Agnostics.Extensions;

public static class ListExtensions
{
    public static PaginatedList<T> MakePaginatedList<T>(this IList<T> items, int itemsPerPage, int page)
    {
        int itemsToSkip = (page - 1) * itemsPerPage;

        if (items?.Count > 0
            && (itemsToSkip > items.Count || itemsToSkip < 0))
            return null;

        var totalPagesCount = (int)Math.Ceiling(items.Count / (double)itemsPerPage);
        IList<T> itemsForThisPage = items.Skip(itemsToSkip).Take(itemsPerPage).ToArray();

        return new(itemsForThisPage, items.Count, page, totalPagesCount);
    }
}