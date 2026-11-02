using FEx.WPFx.Fluent.Shell;
using FEx.WPFx.Fluent.Testing;
using FEx.WPFx.Fluent.Themes;
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
using System.Windows.Media;
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
        public Harness(bool configured = true, Action<int[]>? show = null, Action? clear = null, string source = "Płatnika",
            Border? tab = null, Func<string>? emptyTitle = null)
        {
            Tab = tab ?? new Border();
            Overlay = new LoadOverlay { Source = source, ConnectionTarget = $"bazą {source}" };
            Indicator = new LoadIndicator { Source = source };
            Load = new DataLoad<int[]>(() => configured, rows => rows.Length == 0, Failures.Add, () => Noon)
            {
                Run = read => Task.FromResult(read()),
            };
            View = new ViewLoad<int[]>(Tab, Overlay, Indicator, Load, Plan, show ?? (rows => Shown = rows), clear ?? (() => Shown = null),
                emptyTitle ?? (() => "Brak testów"), "Wybierz inny rok.");
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

            overlay.ConnectionTarget.ShouldBeEmpty();
            overlay.ConnectionTarget = "bazą Kapera";
            overlay.Show(LoadState.Failed, string.Empty, null);
            overlay.lblTitle.Text.ShouldBe("Nie udało się połączyć z bazą Kapera");
            overlay.lblLoading.Text.ShouldBe("Wczytywanie danych z Kapera...", "the two sentences decline the name differently");
            overlay.ConnectionTarget = "bazą Asseco";
            overlay.lblTitle.Text.ShouldBe("Nie udało się połączyć z bazą Asseco", "an open error line follows a renamed target");

            overlay.ShowMissing("Brak biura", null);
            overlay.ConnectionTarget = "bazą Kapera";
            overlay.lblTitle.Text.ShouldBe("Brak biura", "a renamed target must not overwrite another state's line");

            overlay.Source = null!;
            overlay.Source.ShouldBeEmpty();
            overlay.ConnectionTarget = null!;
            overlay.ConnectionTarget.ShouldBeEmpty();
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
            harness.Overlay.lblTitle.Text.ShouldBe("Nie udało się połączyć z bazą Płatnika");
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
    public void AFailureWhileClearingAfterAFailedRead_StillReachesTheFailureSink() =>
        Sta.Run(() =>
        {
            Harness? built = null;
            // Throws only once the load has Failed, so it is the clear after the failed read that breaks - not the one on entering Loading.
            var harness = built = new Harness(clear: () => { if (built?.Load.State is LoadState.Failed) throw new InvalidOperationException("clear broke"); })
            {
                NextFailure = new InvalidOperationException("down"),
            };

            using var host = Open(harness);

            harness.Failures.Select(f => f.Message).ShouldBe(["down", "clear broke"]);
            harness.Load.State.ShouldBe(LoadState.Failed);
        });

    [Fact]
    public void AFailureSinkThatThrows_LeavesTheTabFailed_AndIsTriedOnceMoreByTheGuard() =>
        Sta.Run(() =>
        {
            var heard = new List<string>();
            var tab = new Border();
            var overlay = new LoadOverlay();
            var load = new DataLoad<int[]>(() => true, rows => rows.Length == 0, ex => { heard.Add(ex.Message); if (heard.Count == 1) throw new InvalidOperationException("sink broke"); }, () => Noon)
            {
                Run = read => Task.FromResult(read()),
            };
            _ = new ViewLoad<int[]>(tab, overlay, new LoadIndicator(), load, () => () => throw new InvalidOperationException("down"), _ => { }, () => { }, () => "x", null);

            using var host = Sta.Host(tab);

            load.State.ShouldBe(LoadState.Failed, "a sink that breaks must not leave the skeleton up for good");
            heard.ShouldBe(["down", "sink broke"]);
            overlay.btnRetry.Visibility.ShouldBe(Visibility.Visible);
        });

    [Fact]
    public void AClearThatThrowsOnEnteringLoading_ReachesTheSink_AndTheLoadIsNotStuck() =>
        Sta.Run(() =>
        {
            var armed = false;
            var harness = new Harness(clear: () => { if (armed) throw new InvalidOperationException("clear broke"); });
            armed = true;

            using var host = Open(harness);

            harness.Failures.Select(f => f.Message).ShouldContain("clear broke");
            harness.Load.State.ShouldNotBe(LoadState.Loading);
        });

    [Fact]
    public void AReadThatWorkedButCouldNotBeShown_SaysSo_AndNeverBlamesTheConnection() =>
        Sta.Run(() =>
        {
            var harness = new Harness(show: _ => throw new InvalidOperationException("grid broke"));
            var shell = new Border { Child = harness.Tab };
            var verdicts = new List<bool>();
            shell.AddHandler(ShellLink.DatabaseReachabilityEvent, new DatabaseReachabilityEventHandler((_, e) => verdicts.Add(e.Reachable)));
            using var host = Sta.Host(shell);

            harness.Failures.Select(f => f.Message).ShouldContain("grid broke");
            harness.Overlay.Visibility.ShouldBe(Visibility.Visible);
            harness.Overlay.lblTitle.Text.ShouldBe(LoadText.DisplayFailedTitle);
            harness.Overlay.lblHint.Text.ShouldBe(LoadText.DisplayFailedHint);
            harness.Overlay.btnRetry.Visibility.ShouldBe(Visibility.Visible);
            harness.Overlay.btnSettings.Visibility.ShouldBe(Visibility.Collapsed);
            harness.Overlay.pnlLoading.Visibility.ShouldBe(Visibility.Collapsed);
            harness.Indicator.lblFresh.Text.ShouldBeEmpty();
            AllTextOf(harness.Overlay).ShouldNotContain("połączyć");
            verdicts.ShouldNotBeEmpty();
            verdicts.ShouldAllBe(reachable => reachable);

            harness.Overlay.ConnectionTarget = "bazą Inną";
            harness.Overlay.lblTitle.Text.ShouldBe(LoadText.DisplayFailedTitle, "a renamed target must not overwrite it");
        });

    [Fact]
    public void AnEmptyTitleThatThrows_IsReportedAsADisplayFailure() =>
        Sta.Run(() =>
        {
            var harness = new Harness(emptyTitle: () => throw new InvalidOperationException("title broke")) { NextRows = [] };

            using var host = Open(harness);

            harness.Failures.Select(f => f.Message).ShouldContain("title broke");
            harness.Overlay.lblTitle.Text.ShouldBe(LoadText.DisplayFailedTitle);
        });

    [Fact]
    public void ATabBuiltWhileAlreadyVisible_LoadsAtOnce() =>
        Sta.Run(() =>
        {
            var tab = new Border();
            using var host = Sta.Host(tab);
            tab.IsVisible.ShouldBeTrue();

            var harness = new Harness(tab: tab);

            harness.Reads.ShouldBe(1);
            harness.Load.State.ShouldBe(LoadState.Loaded);
        });

    [Fact]
    public void TheEmptyTitle_IsReadWhenItIsShown_NotWhenTheTabIsBuilt() =>
        Sta.Run(() =>
        {
            var year = 2025;
            var harness = new Harness(emptyTitle: () => $"Brak za {year}") { NextRows = [] };
            using var host = Open(harness);
            harness.Overlay.lblTitle.Text.ShouldBe("Brak za 2025");

            year = 2024;
            Sta.Await(harness.View.ReloadAsync());

            harness.Overlay.lblTitle.Text.ShouldBe("Brak za 2024");
        });

    [Fact]
    public void TheFailedTitle_IsPaintedInTheDangerColour() =>
        Sta.Run(() =>
        {
            var harness = new Harness { NextFailure = new InvalidOperationException("down") };
            harness.Overlay.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(ThemeChoice.DictionaryPath(ThemeVariant.Light), UriKind.Relative) });
            using var host = Open(harness);

            ((SolidColorBrush)harness.Overlay.lblTitle.Foreground).Color.ShouldBe((Color)ColorConverter.ConvertFromString("#B3261E"));
        });

    [Fact]
    public void ABlankSource_StillGivesWholeSentences() =>
        Sta.Run(() =>
        {
            var overlay = new LoadOverlay();
            var indicator = new LoadIndicator();

            overlay.lblLoading.Text.ShouldBe("Wczytywanie danych...");
            indicator.btnRefresh.ToolTip.ShouldBe("Odśwież dane");
            overlay.Show(LoadState.Failed, string.Empty, null);
            overlay.lblTitle.Text.ShouldBe("Nie udało się połączyć z bazą danych");
        });

    [Fact]
    public void AConnectionChangedDuringTheRead_NeverLeavesTheOldRowsOnScreen() =>
        Sta.Run(() =>
        {
            var connection = "Server=a";
            var gate = new TaskCompletionSource<int[]>();
            var calls = 0;
            var tab = new Border();
            var overlay = new LoadOverlay();
            var indicator = new LoadIndicator();
            int[]? shown = null;
            var load = new DataLoad<int[]>(() => true, rows => rows.Length == 0, _ => { }, () => Noon, () => connection)
            {
                Run = read => ++calls == 1 ? gate.Task : Task.FromResult(read()),
            };
            _ = new ViewLoad<int[]>(tab, overlay, indicator, load, () => () => [2], rows => shown = rows, () => shown = null, () => "x", null);

            using var host = Sta.Host(tab);
            load.State.ShouldBe(LoadState.Loading);
            connection = "Server=b";
            gate.SetResult([1]);

            load.State.ShouldBe(LoadState.Loaded);
            shown.ShouldBe([2]);
        });

    private static string AllTextOf(DependencyObject root)
    {
        var text = new List<string>();
        Collect(root, text);
        return string.Join('\n', text);
    }

    private static void Collect(DependencyObject node, List<string> text)
    {
        if (node is TextBlock block)
            text.Add(block.Text);
        else if (node is ContentControl { Content: string content })
            text.Add(content);

        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
            Collect(child, text);
    }
}
