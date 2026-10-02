using FEx.WPFx.Controls;
using Shouldly;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace FEx.WPFx.Tests;

[Collection(WpfTestCollection.Name)]
public class ControlsHandlerTests
{
    private static DataGrid CreateGrid() => new()
    {
        Columns =
        {
            new DataGridTextColumn { Header = "First" },
            new DataGridTextColumn { Header = "Second" }
        }
    };

    [Fact]
    public void SetColumnVisibility_UnknownColumn_IsNoOp() =>
        StaTestRunner.Run(() =>
        {
            var grid = CreateGrid();

            Should.NotThrow(() => grid.SetColumnVisibility("Missing", false));

            grid.Columns.ShouldAllBe(c => c.Visibility == Visibility.Visible);
        });

    [Fact]
    public void SetColumnVisibility_FirstColumn_IsToggled() =>
        StaTestRunner.Run(() =>
        {
            var grid = CreateGrid();

            grid.SetColumnVisibility("First", false);

            grid.Columns[0].Visibility.ShouldBe(Visibility.Hidden);
            grid.Columns[1].Visibility.ShouldBe(Visibility.Visible);
        });

    [Fact]
    public void SetColumnVisibility_ReorderedColumns_TogglesMatchingColumn() =>
        StaTestRunner.Run(() =>
        {
            var grid = CreateGrid();
            grid.Columns[0].DisplayIndex = 1;
            grid.Columns[1].DisplayIndex = 0;

            grid.SetColumnVisibility("Second", false);

            grid.Columns[1].Visibility.ShouldBe(Visibility.Hidden);
            grid.Columns[0].Visibility.ShouldBe(Visibility.Visible);
        });

    [Fact]
    public void SetColumnVisibility_AmbiguousColumn_IsNoOp() =>
        StaTestRunner.Run(() =>
        {
            var grid = new DataGrid
            {
                Columns = { new DataGridTextColumn { Header = "A" }, new DataGridTextColumn { Header = "A" } }
            };

            Should.NotThrow(() => grid.SetColumnVisibility("A", false));

            grid.Columns.ShouldAllBe(c => c.Visibility == Visibility.Visible);
        });

    [Fact]
    public void GetColumnIndex_ReturnsCollectionIndexNotDisplayIndex() =>
        StaTestRunner.Run(() =>
        {
            var grid = CreateGrid();
            grid.Columns[0].DisplayIndex = 1;
            grid.Columns[1].DisplayIndex = 0;

            grid.GetColumnIndex("Second").ShouldBe(1);
            grid.GetColumnIndex("Missing").ShouldBe(-1);
        });

    [Fact]
    public void ApplySortDescriptions_UnknownColumn_IsNoOp() =>
        StaTestRunner.Run(() =>
        {
            var grid = CreateGrid();

            Should.NotThrow(() => grid.ApplySortDescriptions("Missing", ListSortDirection.Ascending));
        });
}
