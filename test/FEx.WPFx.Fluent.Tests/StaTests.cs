using FEx.WPFx.Fluent.Testing;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

[Collection(WpfGlobal.Collection)]
public sealed class StaTests
{
    [Fact]
    public void Run_UsesAnStaThread() =>
        Sta.Run(() => Thread.CurrentThread.GetApartmentState().ShouldBe(ApartmentState.STA));

    [Fact]
    public void Run_RethrowsWhatTheBodyThrew_WithTheOriginalAsTheInnerException()
    {
        var boom = new InvalidOperationException("boom");

        var thrown = Should.Throw<InvalidOperationException>(() => Sta.Run(() => throw boom));

        thrown.Message.ShouldBe("InvalidOperationException: boom");
        thrown.InnerException.ShouldBeSameAs(boom);
    }

    [Fact]
    public void Await_ReturnsForATaskThatAlreadyFinished() =>
        Should.NotThrow(() => Sta.Await(Task.CompletedTask));

    [Fact]
    public void Await_RethrowsTheFailureOfAFinishedTask() =>
        Should.Throw<InvalidOperationException>(() => Sta.Await(Task.FromException(new InvalidOperationException("late"))))
            .Message.ShouldBe("late");

    [Fact]
    public void Await_RefusesATaskThatWouldNeedAPumpingDispatcher()
    {
        var pending = new TaskCompletionSource();

        Should.Throw<InvalidOperationException>(() => Sta.Await(pending.Task));
    }

    [Fact]
    public void Host_GivesTheRootAPresentationSource_WithoutAWindow() =>
        Sta.Run(() =>
        {
            var root = new Border();

            using var host = Sta.Host(root, 300, 200);

            host.RootVisual.ShouldBeSameAs(root);
            root.IsVisible.ShouldBeTrue();
            root.ActualWidth.ShouldBe(300);
            root.ActualHeight.ShouldBe(200);
        });

    [Fact]
    public void RequireInteractiveDesktop_SkipsOnlyWithoutOne()
    {
        if (Environment.UserInteractive)
            Should.NotThrow(Sta.RequireInteractiveDesktop);
        else
            Should.Throw<Exception>(Sta.RequireInteractiveDesktop);
    }
}
