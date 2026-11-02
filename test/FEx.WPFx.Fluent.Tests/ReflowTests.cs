using FEx.WPFx.Fluent.Layout;
using FEx.WPFx.Fluent.Shell;
using Shouldly;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

/// <summary>The numbers behind the view reflow, as pure functions.</summary>
public sealed class ReflowTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(720, true)]
    [InlineData(768, true)]
    [InlineData(899.9, true)]
    [InlineData(900, false)]
    [InlineData(904, false)]
    [InlineData(1366, false)]
    public void IsNarrow_SwitchesAtNineHundredDip(double contentWidth, bool expected) =>
        Reflow.IsNarrow(contentWidth).ShouldBe(expected);

    [Fact]
    public void StackedMinHeight_IsTwoPanesAndTheirSplitter() =>
        Reflow.StackedMinHeight.ShouldBe(2 * 240 + 8);

    [Theory]
    [InlineData(100, 488)]
    [InlineData(488, 488)]
    [InlineData(700, 700)]
    public void StackedHeight_FillsTheViewport_ButNeverGivesAPaneLessThanItsMinimum(double viewport, double expected) =>
        Reflow.StackedHeight(viewport).ShouldBe(expected);

    [Fact]
    public void ThresholdMatchesTheLeastContentWidthOfTheIconRail()
    {
        // The shell's rail threshold is the window width at which a view first fits two grids side by side.
        Reflow.NarrowBelow.ShouldBe(MenuLayout.RailFromWidth);
        // 960 DIP window - 56 DIP rail = 904 DIP content: still wide, as the design's acceptance list says.
        Reflow.IsNarrow(960 - MenuLayout.RailWidth).ShouldBeFalse();
        // 768 DIP window, menu behind the burger: the whole width is content, and narrow.
        Reflow.IsNarrow(768).ShouldBeTrue();
    }
}
