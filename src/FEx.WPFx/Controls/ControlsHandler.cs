using FEx.Core.Abstractions.Extensions;
using FEx.WPFx.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FEx.WPFx.Controls;

public static class ControlsHandler
{
    public static void ApplySortDescriptions(this DataGrid dataGrid,
                                             DataGridColumn col,
                                             string sortPropertyName,
                                             ListSortDirection listSortDirection,
                                             bool clear = true)
    {
        col.InvokeOnDispatcherContext(() =>
        {
            if (dataGrid is not null)
            {
                if (clear)
                    dataGrid.Items.SortDescriptions.Clear();

                dataGrid.Items.SortDescriptions.Add(new(sortPropertyName, listSortDirection));
            }
        });

        dataGrid.ApplySortDirection(col, listSortDirection, clear);
        dataGrid.InvokeOnDispatcherContext(dataGrid.Items.Refresh);
    }

    public static void ApplySortDescriptions(this DataGrid dataGrid,
                                             string columnName,
                                             ListSortDirection listSortDirection,
                                             bool clear = true)
    {
        // An unknown (or ambiguous) column name is a no-op.
        var column = dataGrid.FindColumn(columnName);

        if (column is null)
            return;

        dataGrid.ApplySortDescriptions(column, column.GetSortPropertyName(), listSortDirection, clear);
    }

    public static void ApplySortDescriptions(this DataGrid dataGrid,
                                             DataGridColumn column,
                                             ListSortDirection listSortDirection,
                                             bool clear = true) =>
        dataGrid.ApplySortDescriptions(column, column.GetSortPropertyName(), listSortDirection, clear);

    public static string GetSortPropertyName(this DataGridColumn col) => col.SortMemberPath;

    /// <summary>
    /// Gets the index of the column in <see cref="DataGrid.Columns"/> (not its display index), or -1
    /// when no column, or more than one column, has the given header.
    /// </summary>
    public static int GetColumnIndex(this DataGrid dataGrid, string columnName)
    {
        var column = dataGrid?.FindColumn(columnName);

        return column is null
            ? -1
            : dataGrid!.Columns.IndexOf(column);
    }

    private static DataGridColumn? FindColumn(this DataGrid dataGrid, string columnName)
    {
        var matches = dataGrid.Columns.Where(c => c.GetColumnHeader() == columnName).Take(2).ToList();

        return matches.Count == 1
            ? matches[0]
            : null;
    }

    public static string? GetColumnHeader(this DataGridColumn col) => GetColumnHeader(col.Header);

    public static string? GetColumnHeader(this GridViewColumn col) => GetColumnHeader(col.Header);

    public static string? GetColumnHeader(object colHeader)
    {
        if (colHeader is TextBlock block)
            return block.Text;

        return colHeader is string
            ? colHeader.ToString()
            : null;
    }

    public static void ClearSortDirections(this DataGrid dataGrid) =>
        dataGrid.InvokeOnDispatcherContext(() =>
        {
            if (dataGrid is not null)
                foreach (var c in dataGrid.Columns)
                    c.SortDirection = null;
        });

    public static void SetColumnVisibility(this DataGrid dataGrid, string columnName, bool visible)
    {
        // An unknown (or ambiguous) column name is a no-op.
        var column = dataGrid?.FindColumn(columnName);

        if (column is null)
            return;

        column.Visibility = visible
            ? Visibility.Visible
            : Visibility.Hidden;
    }

    private static void ApplySortDirection(this DataGrid dataGrid,
                                           DataGridColumn col,
                                           ListSortDirection listSortDirection,
                                           bool clear = true)
    {
        if (clear)
            dataGrid.ClearSortDirections();

        col.InvokeOnDispatcherContext(() => col.SortDirection = listSortDirection);
    }
}