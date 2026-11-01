using FEx.WPFx.Fluent.Shell;
using FEx.WPFx.Fluent.Testing;
using FEx.WPFx.Fluent.Views;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

/// <summary>
/// The glue that makes a tab load when it is opened, on real controls: what the overlay and the freshness text
/// say in each state, that opening loads once, that switching back does not, and that the buttons do what they say.
/// </summary>
[Collection(WpfGlobal.Collection)]
public sealed class ViewLoadTests
{
    private static readonly DateTime Noon = new(2026, 10, 21, 14, 5, 0);

    private sealed class Harness
    {
        public Harness(bool configured = true, Action<int[]>? show = null, Action? clear = null, string source = "Płatnika")
        {
            Tab = new Border();
            Overlay = new LoadOverlay { Source = source };
            Indicator = new LoadIndicator { Source = source };
            Load = new DataLoad<int[]>(() => configured, rows => rows.Length == 0, Failures.Add, () => Noon)
            {
                Run = read => Task.FromResult(read()),
            };
            View = new ViewLoad<int[]>(Tab, Overlay, Indicator, Load, Plan, show ?? (rows => Shown = rows), clear ?? (() => Shown = null),
                () => "Brak testów", "Wybierz inny rok.");
        }

        public Border Tab { get; }
        public LoadOverlay Overlay { get; }
        public LoadIndicator Indicator { get; }
        public DataLoad<int[]> Load { get; }
        public ViewLoad<int[]> View { get; }
        public List<Exception> Failures { get; } = [];
        public int[]? Shown { get; private set; }
        public int Reads { get; private set; }
        public int[] NextRows { get; set; } = [1, 2];
        public Exception? NextFailure { get; set; }

        private Func<int[]> Plan() => () =>
        {
            Reads++;
            return NextFailure is { } failure ? throw failure : NextRows;
        };
    }

    private static HwndSource Open(Harness harness) => Sta.Host(harness.Tab);

    [Fact]
    public void ANewTab_ShowsTheSkeleton_UntilItIsOpened() =>
        Sta.Run(() =>
        {
            var harness = new Harness();

            harness.Overlay.Visibility.ShouldBe(Visibility.Visible);
            harness.Overlay.pnlLoading.Visibility.ShouldBe(Visibility.Visible);
            harness.Overlay.lblLoading.Text.ShouldBe("Wczytywanie danych z Płatnika...");
            harness.Overlay.pnlMessage.Visibility.ShouldBe(Visibility.Collapsed);
            harness.Indicator.lblFresh.Text.ShouldBeEmpty();
            harness.Reads.ShouldBe(0);
        });

    [Fact]
    public void TheSkeleton_IsSixBars_AndAWordedLine() =>
        Sta.Run(() =>
        {
            var harness = new Harness();

            harness.Overlay.pnlLoading.Children.OfType<Border>().Count().ShouldBe(6);
            harness.Overlay.pnlLoading.Children.OfType<TextBlock>().Single().ShouldBeSameAs(harness.Overlay.lblLoading);
        });

    [Fact]
    public void OpeningTheTab_LoadsOnce_WithoutAClick_AndShowsTheRowsAndTheFreshness() =>
        Sta.Run(() =>
        {
            var harness = new Harness();

            using var host = Open(harness);

            harness.Reads.ShouldBe(1);
            harness.Load.State.ShouldBe(LoadState.Loaded);
            harness.Shown.ShouldBe([1, 2]);
            harness.Overlay.Visibility.ShouldBe(Visibility.Collapsed);
            harness.Indicator.lblFresh.Text.ShouldBe("Wczytano o 14:05");
        });

    [Fact]
    public void SwitchingAwayAndBack_ShowsTheCachedData_AndDoesNotReadAgain() =>
        Sta.Run(() =>
        {
            var harness = new Harness();
            using var host = Open(harness);

            harness.Tab.Visibility = Visibility.Collapsed;
            harness.Tab.Visibility = Visibility.Visible;

            harness.Reads.ShouldBe(1);
            harness.Overlay.Visibility.ShouldBe(Visibility.Collapsed);
            harness.Shown.ShouldBe([1, 2]);
        });

    [Fact]
    public void TheRefreshButton_ReadsAgain_AndUpdatesTheFreshness() =>
        Sta.Run(() =>
        {
            var harness = new Harness();
            using var host = Open(harness);
            harness.NextRows = [9];

            harness.Indicator.btnRefresh.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            harness.Reads.ShouldBe(2);
            harness.Shown.ShouldBe([9]);
        });

    [Fact]
    public void TheRefreshButton_IsTheIconButtonOfTheDesign_AndNamesTheSource() =>
        Sta.Run(() =>
        {
            var indicator = new LoadIndicator { Source = "bazy KaperCheck" };

            indicator.btnRefresh.Width.ShouldBe(36);
            indicator.btnRefresh.Height.ShouldBe(32);
            indicator.btnRefresh.ToolTip.ShouldBe("Odśwież dane z bazy KaperCheck");
            AutomationProperties.GetName(indicator.btnRefresh).ShouldBe("Odśwież dane z bazy KaperCheck");
            indicator.btnRefresh.Content.ShouldBe("");
        });

    [Fact]
    public void TheSource_CanBeChangedAfterConstruction_AndTheSentencesFollow() =>
        Sta.Run(() =>
        {
            var overlay = new LoadOverlay();
            var indicator = new LoadIndicator();
            overlay.Source.ShouldBeEmpty();

            overlay.Source = "Kapera";
            indicator.Source = "Kapera";

            overlay.lblLoading.Text.ShouldBe("Wczytywanie danych z Kapera...");
            indicator.btnRefresh.ToolTip.ShouldBe("Odśwież dane z Kapera");

            overlay.Show(LoadState.Failed, string.Empty, null);
            overlay.lblTitle.Text.ShouldBe("Nie udało się połączyć z Kapera");
            overlay.Source = "Asseco";
            overlay.lblTitle.Text.ShouldBe("Nie udało się połączyć z Asseco", "an open error line follows a renamed source");
            overlay.Source = null!;
            overlay.Source.ShouldBeEmpty();
            indicator.Source = null!;
            indicator.Source.ShouldBeEmpty();
        });

    [Fact]
    public void AParameterChange_ReloadsATabThatWasOpened_ButNotOneThatNeverWas() =>
        Sta.Run(() =>
        {
            var harness = new Harness();

            harness.View.ParameterChanged();
            harness.Reads.ShouldBe(0);

            using var host = Open(harness);
            harness.View.ParameterChanged();

            harness.Reads.ShouldBe(2);
        });

    [Fact]
    public void ALoadThatFindsNothing_ShowsTheTabsOwnEmptyLine_AndTheHint_AndNoButtons() =>
        Sta.Run(() =>
        {
            var harness = new Harness { NextRows = [] };
            using var host = Open(harness);

            harness.Load.State.ShouldBe(LoadState.Empty);
            harness.Overlay.Visibility.ShouldBe(Visibility.Visible);
            harness.Overlay.lblTitle.Text.ShouldBe("Brak testów");
            harness.Overlay.lblHint.Text.ShouldBe("Wybierz inny rok.");
            harness.Overlay.pnlButtons.Visibility.ShouldBe(Visibility.Collapsed);
            harness.Shown.ShouldBeNull();
        });

    [Fact]
    public void AnEmptyListOfATabWithoutAParameter_HasNoHint() =>
        Sta.Run(() =>
        {
            var overlay = new LoadOverlay();

            overlay.Show(LoadState.Empty, "Brak płatników", null);

            overlay.lblTitle.Text.ShouldBe("Brak płatników");
            overlay.lblHint.Visibility.ShouldBe(Visibility.Collapsed);
        });

    [Fact]
    public void AFailedLoad_ShowsTheErrorState_WithBothButtons_AndNeverTheExceptionText() =>
        Sta.Run(() =>
        {
            var harness = new Harness { NextFailure = new InvalidOperationException("Dla 90010112345 baza nie odpowiada") };
            using var host = Open(harness);

            harness.Load.State.ShouldBe(LoadState.Failed);
            harness.Failures.Count.ShouldBe(1);
            harness.Overlay.lblTitle.Text.ShouldBe("Nie udało się połączyć z Płatnika");
            harness.Overlay.lblHint.Text.ShouldBe("Sprawdź połączenie w Ustawieniach i odśwież.");
            harness.Overlay.btnRetry.Visibility.ShouldBe(Visibility.Visible);
            harness.Overlay.btnRetry.Content.ShouldBe("Odśwież");
            harness.Overlay.btnSettings.Visibility.ShouldBe(Visibility.Visible);
            harness.Overlay.btnSettings.Content.ShouldBe("Otwórz Ustawienia");
            harness.Indicator.lblFresh.Text.ShouldBeEmpty();
            AllTextOf(harness.Overlay).ShouldNotContain("90010112345");
        });

    [Fact]
    public void AFailedLoad_IsNotRetriedBySwitchingBack_ButRecoversThroughTheRetryButton() =>
        Sta.Run(() =>
        {
            var harness = new Harness { NextFailure = new InvalidOperationException() };
            using var host = Open(harness);
            harness.Tab.Visibility = Visibility.Collapsed;
            harness.Tab.Visibility = Visibility.Visible;
            harness.Reads.ShouldBe(1);

            harness.NextFailure = null;
            harness.Overlay.btnRetry.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            harness.Load.State.ShouldBe(LoadState.Loaded);
            harness.Overlay.Visibility.ShouldBe(Visibility.Collapsed);
        });

    [Fact]
    public void WithoutAConnection_TheTabSaysSo_OffersOnlyTheSettings_AndReadsNothing() =>
        Sta.Run(() =>
        {
            var harness = new Harness(configured: false);
            using var host = Open(harness);

            harness.Load.State.ShouldBe(LoadState.Missing);
            harness.Reads.ShouldBe(0);
            harness.Overlay.lblTitle.Text.ShouldBe("Brak konfiguracji bazy");
            harness.Overlay.lblHint.Text.ShouldBe("Uzupełnij połączenie w Ustawieniach.");
            harness.Overlay.btnRetry.Visibility.ShouldBe(Visibility.Collapsed);
            harness.Overlay.btnSettings.Visibility.ShouldBe(Visibility.Visible);
        });

    [Fact]
    public void ShowMissing_SaysATabsOwnSentence_AndOffersOnlyTheSettings() =>
        Sta.Run(() =>
        {
            var overlay = new LoadOverlay();

            overlay.ShowMissing("Brak folderu", "Wskaż folder.");

            overlay.Visibility.ShouldBe(Visibility.Visible);
            overlay.pnlLoading.Visibility.ShouldBe(Visibility.Collapsed);
            overlay.lblTitle.Text.ShouldBe("Brak folderu");
            overlay.lblHint.Text.ShouldBe("Wskaż folder.");
            overlay.btnRetry.Visibility.ShouldBe(Visibility.Collapsed);
            overlay.btnSettings.Visibility.ShouldBe(Visibility.Visible);
        });

    [Fact]
    public void TheSettingsButton_RaisesItsEvent() =>
        Sta.Run(() =>
        {
            var overlay = new LoadOverlay();
            var settings = 0;
            overlay.OpenSettingsRequested += (_, _) => settings++;

            overlay.btnSettings.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            settings.ShouldBe(1);
        });

    [Fact]
    public void ThePhrases_AreExactlyTheOnesOfTheDesign()
    {
        LoadText.Loading("Płatnika").ShouldBe("Wczytywanie danych z Płatnika...");
        LoadText.Refresh("Płatnika").ShouldBe("Odśwież dane z Płatnika");
        LoadText.ErrorTitle("bazą Płatnika").ShouldBe("Nie udało się połączyć z bazą Płatnika");
        LoadText.Blocked.ShouldBe("Najpierw muszą się wczytać dane");
        LoadText.ErrorHint.ShouldBe("Sprawdź połączenie w Ustawieniach i odśwież.");
        LoadText.Retry.ShouldBe("Odśwież");
        LoadText.OpenSettings.ShouldBe("Otwórz Ustawienia");
        LoadText.MissingTitle.ShouldBe("Brak konfiguracji bazy");
        LoadText.MissingHint.ShouldBe("Uzupełnij połączenie w Ustawieniach.");
        LoadText.OtherYearHint.ShouldBe("Wybierz inny rok.");
        LoadText.OtherMonthHint.ShouldBe("Wybierz inny miesiąc.");
        LoadText.Fresh(new DateTime(2026, 10, 21, 9, 3, 0)).ShouldBe("Wczytano o 09:03");
    }

    // ---- The shell link ----

    [Fact]
    public void TheSettingsButton_AsksTheShellAroundTheTab_ToOpenSettings() =>
        Sta.Run(() =>
        {
            var harness = new Harness(configured: false);
            var shell = new Border { Child = harness.Tab };
            var asked = 0;
            shell.AddHandler(ShellLink.OpenSettingsEvent, new RoutedEventHandler((_, _) => asked++));
            using var host = Sta.Host(shell);

            harness.Overlay.btnSettings.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            asked.ShouldBe(1);
        });

    [Fact]
    public void AFailedRead_TellsTheShellTheDatabaseIsUnreachable_AndASuccessfulOneThatItIs() =>
        Sta.Run(() =>
        {
            var harness = new Harness { NextFailure = new InvalidOperationException("down") };
            var shell = new Border { Child = harness.Tab };
            var verdicts = new List<bool>();
            shell.AddHandler(ShellLink.DatabaseReachabilityEvent, new DatabaseReachabilityEventHandler((_, e) => verdicts.Add(e.Reachable)));
            using var host = Sta.Host(shell);

            verdicts.Last().ShouldBeFalse();

            harness.NextFailure = null;
            Sta.Await(harness.View.ReloadAsync());

            verdicts.Last().ShouldBeTrue();
        });

    [Fact]
    public void AnEmptyResult_CountsAsAReachableDatabase() =>
        Sta.Run(() =>
        {
            var harness = new Harness { NextRows = [] };
            var shell = new Border { Child = harness.Tab };
            var verdicts = new List<bool>();
            shell.AddHandler(ShellLink.DatabaseReachabilityEvent, new DatabaseReachabilityEventHandler((_, e) => verdicts.Add(e.Reachable)));
            using var host = Sta.Host(shell);

            verdicts.Last().ShouldBeTrue();
        });

    [Fact]
    public void ATabWithNoConnection_DoesNotReportReachability() =>
        Sta.Run(() =>
        {
            var harness = new Harness(configured: false);
            var shell = new Border { Child = harness.Tab };
            var verdicts = new List<bool>();
            shell.AddHandler(ShellLink.DatabaseReachabilityEvent, new DatabaseReachabilityEventHandler((_, e) => verdicts.Add(e.Reachable)));
            using var host = Sta.Host(shell);

            verdicts.ShouldBeEmpty();
        });

    [Fact]
    public void AFailureWhileShowingTheRows_GoesToTheFailureSink_InsteadOfEndingTheProcess() =>
        Sta.Run(() =>
        {
            var harness = new Harness(show: _ => throw new InvalidOperationException("grid broke"));

            using var host = Open(harness);

            harness.Failures.Select(f => f.Message).ShouldContain("grid broke");
        });

    [Fact]
    public void AFailureWhileClearingAfterAFailure_StillReachesTheFailureSink() =>
        Sta.Run(() =>
        {
            var armed = false; // the view clears once while it is built
            var harness = new Harness(show: _ => throw new InvalidOperationException("grid broke"), clear: () => { if (armed) throw new InvalidOperationException("clear broke"); });
            armed = true;

            using var host = Open(harness);

            harness.Failures.Select(f => f.Message).ShouldContain("clear broke");
        });

    private static string AllTextOf(DependencyObject root)
    {
        var text = new List<string>();
        Collect(root, text);
        return string.Join('\n', text);
    }

    private static void Collect(DependencyObject node, List<string> text)
    {
        switch (node)
        {
            case TextBlock block:
                text.Add(block.Text);
                break;
            case ContentControl { Content: string content }:
                text.Add(content);
                break;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
            Collect(child, text);
    }
}
