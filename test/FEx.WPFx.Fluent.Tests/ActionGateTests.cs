using FEx.WPFx.Fluent.Testing;
using FEx.WPFx.Fluent.Views;
using Shouldly;
using System.Windows.Controls;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

public sealed class ActionGateTests
{
    [Fact]
    public void DatabaseMissing_SaysSo_OnlyWithoutAConnection()
    {
        ActionGate.DatabaseMissing(false).ShouldNotBeNull().ShouldContain("Brak konfiguracji bazy");
        ActionGate.DatabaseMissing(true).ShouldBeNull();
    }

    [Fact]
    public void Only_ReturnsTheRow_WhenThereIsExactlyOne()
    {
        var single = new object();

        ActionGate.Only([single]).ShouldBeSameAs(single);
        ActionGate.Only<object>([]).ShouldBeNull();
        ActionGate.Only([new object(), new object()]).ShouldBeNull();
    }

    [Fact]
    public void Apply_DisablesTheButton_AndGivesTheReasonToItsTooltipAndHint()
    {
        Sta.Run(() =>
        {
            var button = new Button();
            var hint = new HintMark();

            ActionGate.Apply(button, hint, "Zaznacz osobę.");

            button.IsEnabled.ShouldBeFalse();
            button.ToolTip.ShouldBe("Zaznacz osobę.");
            ToolTipService.GetShowOnDisabled(button).ShouldBeTrue();
            hint.Text.ShouldBe("Zaznacz osobę.");
        });
    }

    [Fact]
    public void Apply_WithoutAHintMark_StillGatesTheButton() =>
        Sta.Run(() =>
        {
            var button = new Button();

            Should.NotThrow(() => ActionGate.Apply(button, null, "Zaznacz osobę."));

            button.IsEnabled.ShouldBeFalse();
            button.ToolTip.ShouldBe("Zaznacz osobę.");
        });

    [Fact]
    public void Apply_EnablesTheButton_AndClearsTheReason_WhenNothingIsMissing()
    {
        Sta.Run(() =>
        {
            var button = new Button();
            var hint = new HintMark();
            ActionGate.Apply(button, hint, "Zaznacz osobę.");

            ActionGate.Apply(button, hint, null);

            button.IsEnabled.ShouldBeTrue();
            button.ToolTip.ShouldBeNull();
            hint.Text.ShouldBeEmpty();
        });
    }

    [Fact]
    public void Apply_KeepsTheButtonDisabledWhileRunning_AndSaysNothing()
    {
        Sta.Run(() =>
        {
            var button = new Button();
            var hint = new HintMark();

            ActionGate.Apply(button, hint, null, busy: true);

            button.IsEnabled.ShouldBeFalse();
            button.ToolTip.ShouldBeNull();
            hint.Text.ShouldBeEmpty();
        });
    }
}
