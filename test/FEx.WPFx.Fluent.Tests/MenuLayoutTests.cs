using FEx.WPFx.Fluent.Shell;
using Shouldly;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

public sealed class MenuLayoutTests
{
    [Theory]
    [InlineData(1920, MenuMode.Full)]
    [InlineData(1536, MenuMode.Full)]
    [InlineData(1366, MenuMode.Full)]
    [InlineData(1200, MenuMode.Full)]
    [InlineData(1199.5, MenuMode.Rail)]
    [InlineData(960, MenuMode.Rail)]
    [InlineData(900, MenuMode.Rail)]
    [InlineData(899.5, MenuMode.Flyout)]
    [InlineData(768, MenuMode.Flyout)]
    public void ForWidth_FollowsTheThresholdsOfTheDesign(double width, MenuMode expected) =>
        MenuLayout.ForWidth(width).ShouldBe(expected);

    [Theory]
    [InlineData(MenuMode.Full, 248, 248)]
    [InlineData(MenuMode.Rail, 56, 56)]
    [InlineData(MenuMode.Flyout, 280, 0)]
    public void Sizes_AreThoseOfTheDesign_AndTheFlyoutReservesNothing(MenuMode mode, double panel, double reserved)
    {
        MenuLayout.PanelWidth(mode).ShouldBe(panel);
        MenuLayout.ReservedWidth(mode).ShouldBe(reserved);
    }

    [Theory]
    [InlineData(MenuMode.Full, "Zwiń menu do ikon")]
    [InlineData(MenuMode.Rail, "Rozwiń menu")]
    [InlineData(MenuMode.Flyout, "Zamknij menu")]
    public void ToggleLabel_NamesWhatThePressDoes(MenuMode mode, string expected) =>
        MenuLayout.ToggleLabel(mode).ShouldBe(expected);

    [Theory]
    [InlineData(MenuMode.Full)]
    [InlineData(MenuMode.Rail)]
    [InlineData(MenuMode.Flyout)]
    public void ToggleGlyph_IsTheHamburger_InEveryMode(MenuMode mode) =>
        MenuLayout.ToggleGlyph(mode).ShouldBe("\uE700");

    [Theory]
    [InlineData(1200, false)]
    [InlineData(1199, true)]
    [InlineData(768, true)]
    public void IsCompactBar_FollowsTheWidthAlone(double width, bool compact) =>
        MenuLayout.IsCompactBar(width).ShouldBe(compact);

    [Fact]
    public void ManualCollapse_IsKeptWhileTheWidthStaysInItsBand_AndDropsWhenAThresholdIsCrossed()
    {
        var state = new MenuLayoutState(1536, storedOverride: null);
        state.Mode.ShouldBe(MenuMode.Full);

        state.ToggleCollapse();
        state.Mode.ShouldBe(MenuMode.Rail);
        state.Override.ShouldBe("Rail");

        state.Resize(1400).ShouldBeFalse("same band: the manual state holds");
        state.Mode.ShouldBe(MenuMode.Rail);

        state.Resize(1000); // crosses 1200: the width decides again, and a rail is what it asks for
        state.Override.ShouldBeEmpty();
        state.Mode.ShouldBe(MenuMode.Rail);

        state.Resize(1500).ShouldBeTrue();
        state.Mode.ShouldBe(MenuMode.Full);
    }

    [Fact]
    public void Expand_OnAMidWidthWindow_ShowsTheFullMenu_UntilTheWindowCrossesAThreshold()
    {
        var state = new MenuLayoutState(1000, storedOverride: null);

        state.ToggleCollapse();

        state.Mode.ShouldBe(MenuMode.Full);
        state.Resize(1100);
        state.Mode.ShouldBe(MenuMode.Full);
        state.Resize(800);
        state.Mode.ShouldBe(MenuMode.Flyout);
        state.Override.ShouldBeEmpty();
    }

    [Fact]
    public void InTheFlyoutBand_ToggleCollapseDoesNothing()
    {
        var state = new MenuLayoutState(700, storedOverride: "Rail");

        state.Mode.ShouldBe(MenuMode.Flyout);
        state.ToggleCollapse();

        state.Mode.ShouldBe(MenuMode.Flyout);
    }

    [Theory]
    [InlineData("Rail", MenuMode.Rail)]
    [InlineData("Full", MenuMode.Full)]
    [InlineData("", MenuMode.Full)]
    [InlineData("Flyout", MenuMode.Full)] // the flyout is never a stored state
    [InlineData("nonsense", MenuMode.Full)]
    public void StoredOverride_IsRestoredAtStart_AndAnythingElseIsIgnored(string stored, MenuMode expected) =>
        new MenuLayoutState(1920, stored).Mode.ShouldBe(expected);
}
