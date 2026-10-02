using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Common.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.Controls;
using NSubstitute;
using Shouldly;
using System;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace FEx.WPFx.Tests;

[Collection(WpfTestCollection.Name)]
public class SplashScreenWindowTests
{
    private static int CloseItCount() => SplashScreenWindow.CloseIt?.GetInvocationList().Length ?? 0;

    [Fact]
    public void Close_UnsubscribesFromStaticEventAndStatusHub_SoWindowCanBeCollected() =>
        StaTestRunner.Run(() =>
        {
            // The window raises property changes through the static dispatcher; use one that retains nothing.
            FExCoreStatics.Configure(dispatcherFactory: static () => new InlineDispatcher());

            try
            {
                var hub = new FakeStatusHub();
                var baseline = CloseItCount();

                var window = CreateAndCloseSplash(hub, baseline);

                CloseItCount().ShouldBe(baseline);
                hub.HandlerCount.ShouldBe(0);

                // Dispatcher work queued while the window was built (data bindings, ...) references it
                // until the queue is processed.
#pragma warning disable VSTHRD001 // synchronous drain on the test's own STA thread
                Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.SystemIdle);
#pragma warning restore VSTHRD001

                for (var i = 0; i < 5; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                window.IsAlive.ShouldBeFalse();
            }
            finally
            {
                FExCoreStatics.SetDefaults();
            }
        });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndCloseSplash(FakeStatusHub hub, int baseline)
    {
        var appInfoProvider = Substitute.For<IAppInfoProvider>();
        appInfoProvider.EntryAssembly.Returns(typeof(SplashScreenWindowTests).Assembly);
        var appConfig = Substitute.For<IAppConfig>();
        appConfig.SplashDesign.FontFamily.Returns(new FontFamily("Segoe UI"));
        var asyncHelper = Substitute.For<IAsyncHelper>();

        var window = new SplashScreenWindow(appInfoProvider, asyncHelper, Substitute.For<IStatusService>(), appConfig);

        // The substitute recorded the window's own Initialize delegate; forget it so only the window's
        // own wiring is under test.
        asyncHelper.ClearReceivedCalls();

        window.AttachToStatusHub(hub);
        CloseItCount().ShouldBe(baseline + 1);
        hub.HandlerCount.ShouldBe(3);

        window.Close();

        return new(window);
    }
}
