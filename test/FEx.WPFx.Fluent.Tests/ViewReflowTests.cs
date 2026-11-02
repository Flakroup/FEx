using FEx.WPFx.Fluent.Layout;
using FEx.WPFx.Fluent.Testing;
using Shouldly;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

/// <summary>The attached properties of <see cref="Reflow"/> on a real view: two panes that stack and a footer that wraps below the narrow threshold.</summary>
[Collection(WpfGlobal.Collection)]
public sealed class ViewReflowTests
{
    private const string ViewXaml = """
        <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:r="clr-namespace:FEx.WPFx.Fluent.Layout;assembly=FEx.WPFx.Fluent"
              r:Reflow.Track="True">
          <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
          </Grid.RowDefinitions>
          <ScrollViewer x:Name="area" VerticalScrollBarVisibility="Disabled" r:Reflow.TwoPane="True">
            <Grid x:Name="panes">
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="3*" MinWidth="200" />
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="2*" MinWidth="150" />
              </Grid.ColumnDefinitions>
              <Grid.RowDefinitions>
                <RowDefinition Height="*" />
                <RowDefinition Height="0" />
                <RowDefinition Height="0" />
              </Grid.RowDefinitions>
              <Border x:Name="first" Grid.Column="0" />
              <GridSplitter x:Name="splitter" Grid.Column="1" Width="8" />
              <Border x:Name="second" Grid.Column="2" />
            </Grid>
          </ScrollViewer>
          <Grid x:Name="footer" Grid.Row="1" r:Reflow.Footer="True">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto" />
              <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="*" />
              <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>
            <TextBlock x:Name="lead" Text="lead" />
            <Button x:Name="actions" Grid.Column="1" Content="go" />
          </Grid>
        </Grid>
        """;

    private static void WithView(double width, double height, System.Action<FrameworkElement> body) =>
        Sta.Run(() =>
        {
            var view = (FrameworkElement)XamlReader.Parse(ViewXaml);
            var box = new Border { Width = width, Height = height, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Child = view };
            using var host = Sta.Host(box, 2500, 1500);
            Settle(box);
            body(view);
        });

    private static void Settle(UIElement root)
    {
        for (var i = 0; i < 3; i++)
            root.UpdateLayout();
    }

    private static T Named<T>(FrameworkElement view, string name) where T : class => (T)view.FindName(name);

    [Fact]
    public void Wide_PanesSitSideBySide_AndTheFooterIsOneRow() =>
        WithView(1200, 700, view =>
        {
            Reflow.GetIsNarrow(view).ShouldBeFalse();
            Grid.GetColumn(Named<Border>(view, "second")).ShouldBe(2);
            Grid.GetRow(Named<Border>(view, "second")).ShouldBe(0);
            Grid.GetColumn(Named<UIElement>(view, "actions")).ShouldBe(1);
            Grid.GetRow(Named<UIElement>(view, "actions")).ShouldBe(0);
            Named<ScrollViewer>(view, "area").VerticalScrollBarVisibility.ShouldBe(ScrollBarVisibility.Disabled);
        });

    [Fact]
    public void Narrow_PanesStack_TheSplitterTurnsHorizontal_AndTheFooterWraps() =>
        WithView(704, 509, view =>
        {
            Reflow.GetIsNarrow(view).ShouldBeTrue();
            var second = Named<Border>(view, "second");
            Grid.GetRow(second).ShouldBe(2);
            Grid.GetColumn(second).ShouldBe(0);
            Grid.GetColumnSpan(second).ShouldBe(3);
            var splitter = Named<GridSplitter>(view, "splitter");
            splitter.ResizeDirection.ShouldBe(GridResizeDirection.Rows);
            splitter.Height.ShouldBe(Reflow.SplitterThickness);
            Named<ScrollViewer>(view, "area").VerticalScrollBarVisibility.ShouldBe(ScrollBarVisibility.Auto);
            Named<Grid>(view, "panes").Height.ShouldBeGreaterThanOrEqualTo(Reflow.StackedMinHeight);
            Grid.GetRow(Named<UIElement>(view, "actions")).ShouldBe(1);
            Grid.GetColumnSpan(Named<UIElement>(view, "actions")).ShouldBe(2);
        });

    [Fact]
    public void Narrow_NoPaneIsSmallerThanTheMinimum_EvenInAShortWindow() =>
        WithView(704, 300, view =>
        {
            Named<Border>(view, "first").ActualHeight.ShouldBeGreaterThanOrEqualTo(Reflow.MinPaneHeight);
            Named<Border>(view, "second").ActualHeight.ShouldBeGreaterThanOrEqualTo(Reflow.MinPaneHeight);
        });

    [Fact]
    public void GrowingBackToWide_RestoresTheColumnMinimumsAndTheLayoutAsAuthored() =>
        Sta.Run(() =>
        {
            var view = (FrameworkElement)XamlReader.Parse(ViewXaml);
            var box = new Border { Width = 704, Height = 509, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Child = view };
            using var host = Sta.Host(box, 2500, 1500);
            Settle(box);
            Reflow.GetIsNarrow(view).ShouldBeTrue();

            box.Width = 1200;
            Settle(box);

            Reflow.GetIsNarrow(view).ShouldBeFalse();
            var columns = Named<Grid>(view, "panes").ColumnDefinitions;
            columns[0].MinWidth.ShouldBe(200);
            columns[2].MinWidth.ShouldBe(150);
            Grid.GetColumn(Named<Border>(view, "second")).ShouldBe(2);
            Named<GridSplitter>(view, "splitter").ResizeDirection.ShouldBe(GridResizeDirection.Columns);
            Named<Grid>(view, "panes").Height.ShouldBe(double.NaN);
        });

    [Fact]
    public void AHeightOnlyChange_DoesNotFlipTheLayout() =>
        Sta.Run(() =>
        {
            var view = (FrameworkElement)XamlReader.Parse(ViewXaml);
            var box = new Border { Width = 1200, Height = 700, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Child = view };
            using var host = Sta.Host(box, 2500, 1500);
            Settle(box);

            box.Height = 400;
            Settle(box);

            Reflow.GetIsNarrow(view).ShouldBeFalse();
        });

    [Fact]
    public void TurningTrackingOff_StopsFollowingTheWidth() =>
        Sta.Run(() =>
        {
            var view = (FrameworkElement)XamlReader.Parse(ViewXaml);
            var box = new Border { Width = 1200, Height = 700, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Child = view };
            using var host = Sta.Host(box, 2500, 1500);
            Settle(box);

            Reflow.SetTrack(view, false);
            box.Width = 704;
            Settle(box);

            Reflow.GetTrack(view).ShouldBeFalse();
            Reflow.GetIsNarrow(view).ShouldBeFalse();
        });

    [Fact]
    public void AFooterSwitchedOnWhileAlreadyNarrow_WrapsAtOnce() =>
        Sta.Run(() =>
        {
            var host = new Grid();
            Reflow.SetIsNarrow(host, true);
            var footer = new Grid();
            footer.RowDefinitions.Add(new RowDefinition());
            footer.RowDefinitions.Add(new RowDefinition());
            footer.ColumnDefinitions.Add(new ColumnDefinition());
            footer.ColumnDefinitions.Add(new ColumnDefinition());
            var actions = new Button();
            footer.Children.Add(new TextBlock());
            footer.Children.Add(actions);
            host.Children.Add(footer);

            Reflow.SetFooter(footer, true);

            Grid.GetRow(actions).ShouldBe(1);
            Reflow.GetFooter(footer).ShouldBeTrue();
        });

    [Fact]
    public void ShapesTheLayoutDoesNotKnow_AreLeftAlone() =>
        Sta.Run(() =>
        {
            // Not a FrameworkElement: nothing to subscribe to.
            Should.NotThrow(() => Reflow.SetTrack(new System.Windows.Documents.Run(), true));

            // A two-pane area that does not hold the three-child grid, and a footer that is not two children.
            var area = new ScrollViewer { Content = new Grid() };
            Reflow.SetTwoPane(area, true);
            Reflow.SetIsNarrow(area, true);
            Reflow.GetTwoPane(area).ShouldBeTrue();

            var footer = new Grid();
            footer.Children.Add(new Button());
            Reflow.SetFooter(footer, true);
            Reflow.SetIsNarrow(footer, true);
            Grid.GetRow(footer.Children[0]).ShouldBe(0);

            // Something that is neither: narrowness is just recorded.
            var other = new Border();
            Reflow.SetIsNarrow(other, true);
            Reflow.GetIsNarrow(other).ShouldBeTrue();

            // A TwoPane switched on a non-ScrollViewer does nothing.
            Should.NotThrow(() => Reflow.SetTwoPane(new Border(), true));
        });

    [Fact]
    public void ASplitterDraggedInTheWideState_SurvivesTheNextRoundTripThroughTheNarrowState() =>
        Sta.Run(() =>
        {
            var view = (FrameworkElement)XamlReader.Parse(ViewXaml);
            var box = new Border { Width = 1200, Height = 700, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Child = view };
            using var host = Sta.Host(box, 2500, 1500);
            Settle(box);
            var columns = Named<Grid>(view, "panes").ColumnDefinitions;

            box.Width = 704;
            Settle(box);
            box.Width = 1200;
            Settle(box);
            columns[0].Width = new GridLength(500);
            columns[0].MinWidth = 120;
            box.Width = 704;
            Settle(box);
            box.Width = 1200;
            Settle(box);

            columns[0].Width.ShouldBe(new GridLength(500));
            columns[0].MinWidth.ShouldBe(120, "a minimum set in the wide state is the authored one the next time");
        });

    private static (ScrollViewer Area, Grid Panes, GridSplitter Splitter, Border Second) BuildPanes(int rows)
    {
        var splitter = new GridSplitter { Width = 4, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Height = 30 };
        var second = new Border();
        var panes = new Grid { Height = 900 };
        panes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star), MinWidth = 100 });
        panes.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 50 });
        for (var i = 0; i < rows; i++)
            panes.RowDefinitions.Add(new RowDefinition { Height = i == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(0), MinHeight = i });
        Grid.SetColumn(splitter, 1);
        Grid.SetColumn(second, 2);
        panes.Children.Add(new Border());
        panes.Children.Add(splitter);
        panes.Children.Add(second);
        return (new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, Content = panes }, panes, splitter, second);
    }

    [Fact]
    public void AThreeChildGridWithoutThreeRows_IsLeftAlone_NarrowAndWide() =>
        Sta.Run(() =>
        {
            var (area, panes, _, second) = BuildPanes(rows: 2);
            Reflow.SetTwoPane(area, true);

            Should.NotThrow(() => Reflow.SetIsNarrow(area, true));
            Grid.GetRow(second).ShouldBe(0);
            Grid.GetColumn(second).ShouldBe(2);
            panes.Height.ShouldBe(900);
            area.VerticalScrollBarVisibility.ShouldBe(ScrollBarVisibility.Hidden);

            Should.NotThrow(() => Reflow.SetIsNarrow(area, false));
            Grid.GetColumn(second).ShouldBe(2);
        });

    [Fact]
    public void AFooterWithoutTwoRows_IsLeftAlone() =>
        Sta.Run(() =>
        {
            var footer = new Grid();
            footer.ColumnDefinitions.Add(new ColumnDefinition());
            footer.ColumnDefinitions.Add(new ColumnDefinition());
            var actions = new Button();
            footer.Children.Add(new TextBlock());
            footer.Children.Add(actions);
            Grid.SetColumn(actions, 1);
            Reflow.SetFooter(footer, true);

            Reflow.SetIsNarrow(footer, true);

            Grid.GetRow(actions).ShouldBe(0);
            Grid.GetColumn(actions).ShouldBe(1);
            Grid.GetColumnSpan(actions).ShouldBe(1);

        });

    [Fact]
    public void ThePanes_StackWithAStretchingSplitter_AndTheAuthoredValuesComeBackExactly() =>
        Sta.Run(() =>
        {
            var (area, panes, splitter, _) = BuildPanes(rows: 3);
            Reflow.SetTwoPane(area, true);

            Reflow.SetIsNarrow(area, true);
            splitter.HorizontalAlignment.ShouldBe(HorizontalAlignment.Stretch, "an authored Right or Center splitter would collapse to zero width");
            panes.RowDefinitions[1].Height.ShouldBe(new GridLength(Reflow.SplitterThickness));

            Reflow.SetIsNarrow(area, false);
            splitter.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
            splitter.VerticalAlignment.ShouldBe(VerticalAlignment.Top);
            splitter.Width.ShouldBe(4);
            splitter.Height.ShouldBe(30);
            area.VerticalScrollBarVisibility.ShouldBe(ScrollBarVisibility.Hidden);
            panes.Height.ShouldBe(900);
            panes.RowDefinitions[0].Height.ShouldBe(new GridLength(1, GridUnitType.Star));
            panes.RowDefinitions[1].Height.ShouldBe(new GridLength(0));
            panes.RowDefinitions[1].MinHeight.ShouldBe(1);
            panes.RowDefinitions[2].MinHeight.ShouldBe(2);
            panes.ColumnDefinitions[0].MinWidth.ShouldBe(100);
            panes.ColumnDefinitions[2].MinWidth.ShouldBe(50);
        });

    [Fact]
    public void TwoPaneSwitchedOnWhileAlreadyNarrow_StacksAtOnce() =>
        Sta.Run(() =>
        {
            var (area, _, _, second) = BuildPanes(rows: 3);
            Reflow.SetIsNarrow(area, true);

            Reflow.SetTwoPane(area, true);

            Grid.GetRow(second).ShouldBe(2);
            Grid.GetColumn(second).ShouldBe(0);
        });

    [Fact]
    public void TwoPaneSwitchedOffAndOnWhileNarrow_KeepsTheFirstSnapshot() =>
        Sta.Run(() =>
        {
            var (area, panes, _, _) = BuildPanes(rows: 3);
            Reflow.SetIsNarrow(area, true);
            Reflow.SetTwoPane(area, true);

            Reflow.SetTwoPane(area, false);
            Reflow.SetTwoPane(area, true);
            Reflow.SetIsNarrow(area, false);

            panes.Height.ShouldBe(900, "the second narrow application must not snapshot the narrow values as authored");
            panes.ColumnDefinitions[0].MinWidth.ShouldBe(100);
        });

    [Fact]
    public void TwoPaneSwitchedOff_StopsRefittingTheStackedHeight() =>
        Sta.Run(() =>
        {
            var view = (FrameworkElement)XamlReader.Parse(ViewXaml);
            var box = new Border { Width = 704, Height = 509, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Child = view };
            using var host = Sta.Host(box, 2500, 1500);
            Settle(box);
            var area = Named<ScrollViewer>(view, "area");
            var panes = Named<Grid>(view, "panes");

            box.Height = 1000;
            Settle(box);
            panes.Height.ShouldBe(Reflow.StackedHeight(area.ActualHeight), "still following the height while on");

            Reflow.SetTwoPane(area, false);
            panes.Height = 5;
            box.Height = 600;
            Settle(box);

            panes.Height.ShouldBe(5);
        });
}
