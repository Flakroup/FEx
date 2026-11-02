using FEx.WPFx.Fluent.Shell;
using FEx.WPFx.Fluent.Testing;
using Shouldly;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

[Collection(WpfGlobal.Collection)]
public sealed class ShellLinkTests
{
    [Fact]
    public void OpenSettings_BubblesFromTheViewToWhoeverHandlesIt() =>
        Sta.Run(() =>
        {
            var view = new Border();
            var shell = new Border { Child = view };
            var raised = new List<object>();
            shell.AddHandler(ShellLink.OpenSettingsEvent, new RoutedEventHandler((_, e) => raised.Add(e.OriginalSource)));

            ShellLink.RaiseOpenSettings(view);

            raised.ShouldBe([view]);
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DatabaseReachability_BubblesWithTheVerdict(bool reachable) =>
        Sta.Run(() =>
        {
            var view = new Border();
            var shell = new Border { Child = view };
            var seen = new List<bool>();
            shell.AddHandler(ShellLink.DatabaseReachabilityEvent, new DatabaseReachabilityEventHandler((_, e) => seen.Add(e.Reachable)));

            ShellLink.RaiseDatabaseReachability(view, reachable);

            seen.ShouldBe([reachable]);
        });

    [Fact]
    public void WithoutAShellAroundIt_RaisingIsHarmless() =>
        Sta.Run(() =>
        {
            var view = new Border();

            Should.NotThrow(() => ShellLink.RaiseOpenSettings(view));
            Should.NotThrow(() => ShellLink.RaiseDatabaseReachability(view, reachable: false));
        });
}
