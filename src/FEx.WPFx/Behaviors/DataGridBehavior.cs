using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FEx.WPFx.Behaviors;

public static class DataGridBehavior
{
    #region DisplayRowNumber
    public static readonly DependencyProperty DisplayRowNumberProperty =
        DependencyProperty.RegisterAttached("DisplayRowNumber",
            typeof(bool),
            typeof(DataGridBehavior),
            new FrameworkPropertyMetadata(false, OnDisplayRowNumberChanged));

    public static bool GetDisplayRowNumber(DependencyObject target) =>
        (bool)(target.GetValue(DisplayRowNumberProperty) ?? false);

    public static void SetDisplayRowNumber(DependencyObject target, bool value) =>
        target.SetValue(DisplayRowNumberProperty, value);

    private static void OnDisplayRowNumberChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not DataGrid dataGrid
            || !(bool)e.NewValue)
            return;

        EventHandler<DataGridRowEventArgs>? loadedRowHandler = null;

        loadedRowHandler = (_, ea) =>
        {
            if (!GetDisplayRowNumber(dataGrid))
            {
                dataGrid.LoadingRow -= loadedRowHandler;

                return;
            }

            ea.Row.Header = ea.Row.GetIndex();
        };

        dataGrid.LoadingRow += loadedRowHandler;

        ItemsChangedEventHandler? itemsChangedHandler = null;

        itemsChangedHandler = (_, _) =>
        {
            if (!GetDisplayRowNumber(dataGrid))
            {
                dataGrid.ItemContainerGenerator.ItemsChanged -= itemsChangedHandler;

                return;
            }

            dataGrid.GetVisualChildCollection<DataGridRow>().ForEach(d => d.Header = d.GetIndex());
        };

        dataGrid.ItemContainerGenerator.ItemsChanged += itemsChangedHandler;
    }
    #endregion

    #region Get Visuals
    public static List<T> GetVisualChildCollection<T>(this DependencyObject parent) where T : Visual
    {
        var visualCollection = new List<T>();
        parent.GetVisualChildCollection(visualCollection);

        return visualCollection;
    }

    private static void GetVisualChildCollection<T>(this DependencyObject parent, ICollection<T> visualCollection)
        where T : Visual
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);

        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T visual)
                visualCollection.Add(visual);

            child?.GetVisualChildCollection(visualCollection);
        }
    }
    #endregion
}