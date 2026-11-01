using FEx.WPFx.Fluent.Views;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Xunit;

namespace FEx.WPFx.Fluent.Testing;

/// <summary>The one place tests get a WPF-capable thread, shared by every test that builds a visual.</summary>
public static class Sta
{
    /// <summary>The message-only window class parent: a host with it has no window on screen at all.</summary>
    private static readonly IntPtr MessageOnlyParent = new(-3);

    /// <summary>
    /// Warms up pack:// loading once, before any test body runs. Loading a ResourceDictionary by component URI on a
    /// cold process failed with "The URI prefix is not recognized" (measured: the same two tests, every run, when
    /// they were the first to touch it). Building a XAML control first goes through Application.LoadComponent, which
    /// is what registers the scheme. The static constructor runs once and every <see cref="Run"/> waits for it.
    /// </summary>
    // ponytail: the failure branch needs a WPF runtime that cannot load, which no test can arrange; the success path runs on every use.
    [ExcludeFromCodeCoverage]
    static Sta()
    {
        Exception? caught = null;
        var warmUp = new Thread(() =>
        {
            try
            {
                _ = new LoadIndicator();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        warmUp.SetApartmentState(ApartmentState.STA);
        warmUp.Start();
        warmUp.Join();

        if (caught is not null)
            throw new InvalidOperationException("Could not warm up WPF component loading.", caught);
    }

    /// <summary>
    /// Skips a test that needs a window to really count as visible. A CI runner installed as a service lives in a
    /// non-interactive window station, where a window created with WS_VISIBLE still reads as hidden.
    /// </summary>
    public static void RequireInteractiveDesktop() =>
        Assert.SkipUnless(Environment.UserInteractive, "No interactive desktop: windows of a service never count as visible.");

    /// <summary>
    /// One STA body at a time. Application.LoadComponent keeps a per-package list of open streams that is not thread
    /// safe: two threads loading XAML together failed with an ArgumentOutOfRangeException from
    /// <c>PackagePart.CleanUpRequestedStreamsList</c> (measured on a full suite with a dozen view-building classes in
    /// flight). Serialising the bodies removes the race instead of retrying around it.
    /// </summary>
    private static readonly object OneAtATime = new();

    /// <summary>
    /// Waits for a task that a test arranged to finish inline (a load whose runner is <c>Task.FromResult</c>). There is
    /// no dispatcher pumping in a test, so a task that really needed a thread would hang here - hence the check.
    /// </summary>
    public static void Await(Task task)
    {
        if (!task.IsCompleted)
            throw new InvalidOperationException("The task did not complete inline; set the view's Load.Run before using it.");
#pragma warning disable VSTHRD002
        task.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    /// <summary>Runs a body on a fresh STA thread - WPF visuals refuse the MTA thread the runner gives us - and rethrows what it threw.</summary>
    public static void Run(Action body)
    {
        lock (OneAtATime)
        {
            RunLocked(body);
        }
    }

    private static void RunLocked(Action body)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            finally
            {
                // Without this every thread that touched a DataGrid or a ComboBox leaves a foreground thread behind,
                // and the runner times out waiting for them once a few dozen views have been built.
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (caught is not null)
            throw new InvalidOperationException($"{caught.GetType().Name}: {caught.Message}", caught);
    }

    /// <summary>
    /// Puts <paramref name="root"/> under a real presentation source - which is what makes it visible, so its
    /// <c>IsVisibleChanged</c> fires and a grid generates its rows - without showing anything: the host is a
    /// message-only window. Call on the STA thread; dispose to detach.
    /// </summary>
    public static HwndSource Host(UIElement root, double width = 1000, double height = 600)
    {
        var source = new HwndSource(new HwndSourceParameters("fex-fluent-test", (int)width, (int)height) { ParentWindow = MessageOnlyParent })
        {
            RootVisual = root,
        };
        root.Measure(new Size(width, height));
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();
        return source;
    }
}
