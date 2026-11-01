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
    public void GrowingBackToWide_RestoresTheColumnsAsAuthored_IncludingASplitterDraggedMeanwhile() =>
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
            columns[0].Width.ShouldBe(new GridLength(3, GridUnitType.Star));
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
}
