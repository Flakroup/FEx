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
        var column = dataGrid.Columns[dataGrid.GetColumnIndex(columnName)];
        dataGrid.ApplySortDescriptions(column, column.GetSortPropertyName(), listSortDirection, clear);
    }

    public static void ApplySortDescriptions(this DataGrid dataGrid,
                                             DataGridColumn column,
                                             ListSortDirection listSortDirection,
                                             bool clear = true) =>
        dataGrid.ApplySortDescriptions(column, column.GetSortPropertyName(), listSortDirection, clear);

    public static string GetSortPropertyName(this DataGridColumn col) => col.SortMemberPath;

    public static int GetColumnIndex(this DataGrid dataGrid, string columnName)
    {
        try
        {
            if (dataGrid is not null)
                return dataGrid.Columns.Single(c => c.GetColumnHeader() == columnName).DisplayIndex;
        }
        catch (Exception ex)
        {
            ex.HandleException();
        }

        return -1;
    }

    public static string GetColumnHeader(this DataGridColumn col) => GetColumnHeader(col.Header);

    public static string GetColumnHeader(this GridViewColumn col) => GetColumnHeader(col.Header);

    public static string GetColumnHeader(object colHeader)
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
        var idx = dataGrid.GetColumnIndex(columnName);

        if (dataGrid is not null)
        {
            if (idx > 0
                && dataGrid.Columns[idx] is null)
                return;

            dataGrid.Columns[idx].Visibility = visible
                ? Visibility.Visible
                : Visibility.Hidden;
        }
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