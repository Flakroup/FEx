using FEx.WPFx.Controls;
using Shouldly;
using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace FEx.WPFx.Tests;

[Collection(WpfTestCollection.Name)]
public class ZoomBorderTests
{
    [Fact]
    public void ScaleChanged_IsRaisedOnUiThread() =>
        StaTestRunner.Run(() =>
        {
            var uiThreadId = Thread.CurrentThread.ManagedThreadId;
            int? handlerThreadId = null;
            var zoomBorder = new ZoomBorder();
            zoomBorder.SetOnScaleChanged((_, _) => handlerThreadId = Thread.CurrentThread.ManagedThreadId);

            zoomBorder.Scale = 2;
            // Drain the dispatcher queue so the marshalled callback runs.
            // VSTHRD001: synchronous drain on the test's own STA thread.
#pragma warning disable VSTHRD001
            zoomBorder.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001

            handlerThreadId.ShouldBe(uiThreadId);
        });

    [Fact]
    public void Child_Reassigned_DetachesSizeChangedFromPreviousChild() =>
        StaTestRunner.Run(() =>
        {
            var first = new Border();
            var second = new Border();
            var zoomBorder = new ZoomBorder { Child = first };
            SizeChangedHandlerCount(first).ShouldBe(1);

            zoomBorder.Child = second;

            SizeChangedHandlerCount(first).ShouldBe(0);
            SizeChangedHandlerCount(second).ShouldBe(1);
        });

    [Fact]
    public void Child_SetToNull_DetachesSizeChangedFromRemovedChild() =>
        StaTestRunner.Run(() =>
        {
            var child = new Border();
            var zoomBorder = new ZoomBorder { Child = child };

            zoomBorder.Child = null!;

            SizeChangedHandlerCount(child).ShouldBe(0);
        });

    // EventHandlersStore is internal to WPF; it is the only way to observe routed-event subscriptions.
    private static int SizeChangedHandlerCount(FrameworkElement element)
    {
        var storeProperty = typeof(UIElement).GetProperty("EventHandlersStore",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var store = storeProperty.GetValue(element);

        if (store is null)
            return 0;

#pragma warning disable REFL009 // internal WPF type
        var getHandlers = store.GetType().GetMethod("GetRoutedEventHandlers",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
#pragma warning restore REFL009

        return (getHandlers.Invoke(store, [FrameworkElement.SizeChangedEvent]) as Array)?.Length ?? 0;
    }
}
