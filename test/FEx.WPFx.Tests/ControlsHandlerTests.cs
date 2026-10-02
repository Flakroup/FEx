using FEx.WPFx.Controls;
using Shouldly;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace FEx.WPFx.Tests;

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
}
