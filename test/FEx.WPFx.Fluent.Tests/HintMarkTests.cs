using FEx.WPFx.Fluent.Testing;
using FEx.WPFx.Fluent.Views;
using Shouldly;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

/// <summary>The "?" mark that replaces the coloured explanation lines: it is there only with a sentence, and the sentence is not lost to a screen reader.</summary>
[Collection(WpfGlobal.Collection)]
public sealed class HintMarkTests
{
    private const string Sentence = "Wpisz kwotę w kolumnie Dochód / przychód.";

    [Fact]
    public void WithoutASentence_TheMarkIsGone() =>
        Sta.Run(() =>
        {
            var mark = new HintMark();

            mark.Visibility.ShouldBe(Visibility.Collapsed);
            mark.Text.ShouldBeEmpty();
        });

    [Fact]
    public void ASentence_ShowsTheMark_AndGoesToTheTooltipAndTheAutomationName() =>
        Sta.Run(() =>
        {
            var mark = new HintMark { Text = Sentence };

            mark.Visibility.ShouldBe(Visibility.Visible);
            mark.ToolTip.ShouldBeOfType<ToolTip>().Content.ShouldBe(Sentence);
            AutomationProperties.GetName(mark).ShouldBe(Sentence);
            ToolTipService.GetShowOnDisabled(mark).ShouldBeTrue();
        });

    [Fact]
    public void ClearingTheSentence_HidesTheMarkAgain() =>
        Sta.Run(() =>
        {
            var mark = new HintMark { Text = Sentence };

            mark.Text = string.Empty;

            mark.Visibility.ShouldBe(Visibility.Collapsed);
        });

    [Fact]
    public void TheMark_IsReachableFromTheKeyboard_AndOpensItsTooltipOnFocus() =>
        Sta.Run(() =>
        {
            var mark = new HintMark { Text = Sentence };
            mark.Focusable.ShouldBeTrue();
            mark.IsTabStop.ShouldBeTrue();
            var tip = mark.ToolTip.ShouldBeOfType<ToolTip>();

            mark.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, mark) { RoutedEvent = Keyboard.GotKeyboardFocusEvent });
            tip.IsOpen.ShouldBeTrue();

            mark.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, mark, null) { RoutedEvent = Keyboard.LostKeyboardFocusEvent });
            tip.IsOpen.ShouldBeFalse();
        });

    [Fact]
    public void TheGate_GivesItsReasonToTheMark_AndTakesItBackWhenNothingIsMissing() =>
        Sta.Run(() =>
        {
            var button = new Button();
            var mark = new HintMark();

            ActionGate.Apply(button, mark, Sentence);
            mark.Text.ShouldBe(Sentence);
            mark.Visibility.ShouldBe(Visibility.Visible);
            button.ToolTip.ShouldBe(Sentence);
            button.IsEnabled.ShouldBeFalse();

            ActionGate.Apply(button, mark, null);
            mark.Visibility.ShouldBe(Visibility.Collapsed);
            button.IsEnabled.ShouldBeTrue();

            ActionGate.Apply(button, mark, Sentence, busy: true);
            mark.Visibility.ShouldBe(Visibility.Collapsed, "while running the reason would be noise");
        });
}
