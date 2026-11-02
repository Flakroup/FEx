using FEx.WPFx.Fluent.Testing;
using Shouldly;
using System;
using System.Runtime.InteropServices;
using System.Text;
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
            ClassNameOf(GetAncestor(host.Handle, ParentAncestor)).ShouldBe("Message", "the host hangs off the message-only parent, so no window reaches the desktop");
        });

    [Fact]
    public void Run_LetsASkipThrough_AsASkip_NotAsAFailure()
    {
        var thrown = Should.Throw<Exception>(() => Sta.Run(() => Assert.Skip("no desktop")));

        thrown.ShouldNotBeOfType<InvalidOperationException>();
        thrown.GetType().Name.ShouldBe("SkipException");
        thrown.Message.ShouldContain("no desktop");
    }

    private const uint ParentAncestor = 1;

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder text, int max);

    private static string ClassNameOf(IntPtr hwnd)
    {
        var text = new StringBuilder(64);
        _ = GetClassName(hwnd, text, text.Capacity);
        return text.ToString();
    }

    [Fact]
    public void RequireInteractiveDesktop_SkipsOnlyWithoutOne()
    {
        if (Environment.UserInteractive)
            Should.NotThrow(Sta.RequireInteractiveDesktop);
        else
            Should.Throw<Exception>(Sta.RequireInteractiveDesktop);
    }
}
