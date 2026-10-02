using FEx.WPFx.Controls;
using Shouldly;
using System.Threading;
using System.Windows.Threading;
using Xunit;

namespace FEx.WPFx.Tests;

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
}
