using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Common.Abstractions.Interfaces;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.Controls;
using NSubstitute;
using Shouldly;
using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace FEx.WPFx.Tests;

public class SplashScreenWindowTests
{
    private static int CloseItCount() => SplashScreenWindow.CloseIt?.GetInvocationList().Length ?? 0;

    [Fact]
    public void Close_UnsubscribesFromStaticEventAndStatusHub_SoWindowCanBeCollected() =>
        StaTestRunner.Run(() =>
        {
            var hub = new FakeStatusHub();
            var baseline = CloseItCount();

            var window = CreateAndCloseSplash(hub, baseline);

            CloseItCount().ShouldBe(baseline);
            hub.HandlerCount.ShouldBe(0);

            for (var i = 0; i < 5; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            window.IsAlive.ShouldBeFalse();
        });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndCloseSplash(FakeStatusHub hub, int baseline)
    {
        var appInfoProvider = Substitute.For<IAppInfoProvider>();
        appInfoProvider.EntryAssembly.Returns(typeof(SplashScreenWindowTests).Assembly);
        var asyncHelper = Substitute.For<IAsyncHelper>();

        var window = new SplashScreenWindow(appInfoProvider,
            asyncHelper,
            Substitute.For<IStatusService>(),
            Substitute.For<IAppConfig>());

        // The substitutes record the window's own delegates; forget them so only the window's wiring is under test.
        asyncHelper.ClearReceivedCalls();

        window.AttachToStatusHub(hub);
        CloseItCount().ShouldBe(baseline + 1);
        hub.HandlerCount.ShouldBe(3);

        window.Close();

        return new(window);
    }
}
