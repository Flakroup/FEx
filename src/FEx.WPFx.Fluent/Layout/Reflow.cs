using System;
using System.Windows;
using System.Windows.Controls;

namespace FEx.WPFx.Fluent.Layout;

/// <summary>
/// How a module view reflows inside the content area . Two states, decided by the width of the view
/// alone: wide (two grids side by side, action footer in one row) and narrow (grids stacked, footer in two rows).
/// The menu shell has its own thresholds (<c>MenuLayout</c>); this one is about what the view does with the space
/// the shell leaves it.
/// <para>
/// Wiring, all in XAML: <c>Track</c> on the view's root measures it and sets the inherited <c>IsNarrow</c>;
/// <c>TwoPane</c> on a <see cref="ScrollViewer"/> holding a three-child grid (pane, splitter, pane) and
/// <c>Footer</c> on a two-child grid (lead text, actions) swap their layout when <c>IsNarrow</c> changes.
/// The XAML is authored in the wide state; the narrow state is applied on top and undone from a saved copy.
/// </para>
/// </summary>
public static class Reflow
{
    /// <summary>The content width, in DIP, below which a view switches to the narrow layout.</summary>
    public const double NarrowBelow = 900;

    /// <summary>Least height of a stacked pane.</summary>
    public const double MinPaneHeight = 240;

    public const double SplitterThickness = 8;

    /// <summary>Height of the stacked two-pane area at its smallest; the area scrolls below it.</summary>
    public const double StackedMinHeight = 2 * MinPaneHeight + SplitterThickness;

    public static bool IsNarrow(double contentWidth) => contentWidth < NarrowBelow;

    /// <summary>The height of the stacked grid: the viewport when it is tall enough, otherwise enough for two panes.</summary>
    public static double StackedHeight(double viewportHeight) => Math.Max(viewportHeight, StackedMinHeight);

    public static readonly DependencyProperty TrackProperty =
        DependencyProperty.RegisterAttached("Track", typeof(bool), typeof(Reflow), new PropertyMetadata(false, OnTrackChanged));

    public static bool GetTrack(DependencyObject d) => (bool)d.GetValue(TrackProperty);

    public static void SetTrack(DependencyObject d, bool value) => d.SetValue(TrackProperty, value);

    public static readonly DependencyProperty IsNarrowProperty =
        DependencyProperty.RegisterAttached("IsNarrow", typeof(bool), typeof(Reflow),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnIsNarrowChanged));

    public static bool GetIsNarrow(DependencyObject d) => (bool)d.GetValue(IsNarrowProperty);

    public static void SetIsNarrow(DependencyObject d, bool value) => d.SetValue(IsNarrowProperty, value);

    public static readonly DependencyProperty TwoPaneProperty =
        DependencyProperty.RegisterAttached("TwoPane", typeof(bool), typeof(Reflow), new PropertyMetadata(false, OnTwoPaneChanged));

    public static bool GetTwoPane(DependencyObject d) => (bool)d.GetValue(TwoPaneProperty);

    public static void SetTwoPane(DependencyObject d, bool value) => d.SetValue(TwoPaneProperty, value);

    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.RegisterAttached("Footer", typeof(bool), typeof(Reflow), new PropertyMetadata(false, OnFooterChanged));

    public static bool GetFooter(DependencyObject d) => (bool)d.GetValue(FooterProperty);

    public static void SetFooter(DependencyObject d, bool value) => d.SetValue(FooterProperty, value);

    private static readonly DependencyProperty SavedProperty =
        DependencyProperty.RegisterAttached("Saved", typeof(SavedColumns), typeof(Reflow));

    private static void OnTrackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement view)
            return;

        if ((bool)e.NewValue)
            view.SizeChanged += TrackedSizeChanged;
        else
            view.SizeChanged -= TrackedSizeChanged;
    }

    private static void TrackedSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
            SetIsNarrow((DependencyObject)sender, IsNarrow(e.NewSize.Width));
    }

    private static void OnIsNarrowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var narrow = (bool)e.NewValue;
        if (d is ScrollViewer area && GetTwoPane(area))
            ApplyTwoPane(area, narrow);
        else if (d is Grid footer && GetFooter(footer))
            ApplyFooter(footer, narrow);
    }

    private static void OnTwoPaneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer area && (bool)e.NewValue)
            area.SizeChanged += (_, _) => FitStackedHeight(area);
    }

    private static void OnFooterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Grid footer && (bool)e.NewValue && GetIsNarrow(footer))
            ApplyFooter(footer, true);
    }

    /// <summary>The columns as the XAML authored them, kept so that leaving the narrow state restores them.</summary>
    private sealed record SavedColumns(GridLength[] Widths, double[] MinWidths);

    private static void ApplyTwoPane(ScrollViewer area, bool narrow)
    {
        if (area.Content is not Grid { Children.Count: 3 } grid || grid.Children[1] is not GridSplitter splitter)
            return;

        UIElement first = grid.Children[0], second = grid.Children[2];
        var columns = grid.ColumnDefinitions;
        var rows = grid.RowDefinitions;

        if (narrow)
        {
            // Snapshot on every entry, not once: a splitter dragged in the wide state must survive the next round trip.
            grid.SetValue(SavedProperty, new SavedColumns(
                [.. Array.ConvertAll([.. columns], c => c.Width)],
                [.. Array.ConvertAll([.. columns], c => c.MinWidth)]));

            foreach (var column in columns)
                column.MinWidth = 0;

            rows[0].Height = new GridLength(1, GridUnitType.Star);
            rows[0].MinHeight = MinPaneHeight;
            rows[1].Height = new GridLength(SplitterThickness);
            rows[2].Height = new GridLength(1, GridUnitType.Star);
            rows[2].MinHeight = MinPaneHeight;

            Place(first, 0, 0, 3);
            Place(splitter, 1, 0, 3);
            Place(second, 2, 0, 3);
            splitter.ResizeDirection = GridResizeDirection.Rows;
            splitter.Width = double.NaN;
            splitter.Height = SplitterThickness;
            splitter.VerticalAlignment = VerticalAlignment.Center;
            area.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            FitStackedHeight(area);
        }
        else
        {
            if (grid.GetValue(SavedProperty) is SavedColumns saved)
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    columns[i].Width = saved.Widths[i];
                    columns[i].MinWidth = saved.MinWidths[i];
                }
            }

            rows[0].Height = new GridLength(1, GridUnitType.Star);
            rows[0].MinHeight = 0;
            rows[1].Height = new GridLength(0);
            rows[2].Height = new GridLength(0);
            rows[2].MinHeight = 0;

            Place(first, 0, 0, 1);
            Place(splitter, 0, 1, 1);
            Place(second, 0, 2, 1);
            splitter.ResizeDirection = GridResizeDirection.Columns;
            splitter.Width = SplitterThickness;
            splitter.Height = double.NaN;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            area.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            grid.Height = double.NaN;
        }
    }

    private static void FitStackedHeight(ScrollViewer area)
    {
        if (GetIsNarrow(area) && area.Content is Grid grid)
            grid.Height = StackedHeight(area.ActualHeight);
    }

    private static void ApplyFooter(Grid footer, bool narrow)
    {
        if (footer.Children.Count != 2)
            return;

        UIElement lead = footer.Children[0], actions = footer.Children[1];
        if (narrow)
        {
            Place(lead, 0, 0, 2);
            Place(actions, 1, 0, 2);
        }
        else
        {
            Place(lead, 0, 0, 1);
            Place(actions, 0, 1, 1);
        }
    }

    private static void Place(UIElement element, int row, int column, int columnSpan)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        Grid.SetColumnSpan(element, columnSpan);
    }
}
