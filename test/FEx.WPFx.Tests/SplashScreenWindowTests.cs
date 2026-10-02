using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Common.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.Controls;
using NSubstitute;
using Shouldly;
using System.Windows.Media;
using Xunit;

namespace FEx.WPFx.Tests;

[Collection(WpfTestCollection.Name)]
public class SplashScreenWindowTests
{
    private static int CloseItCount() => SplashScreenWindow.CloseIt?.GetInvocationList().Length ?? 0;

    [Fact]
    public void Close_UnsubscribesFromStaticEventAndStatusHub() =>
        StaTestRunner.Run(() =>
        {
            // The window raises property changes through the static dispatcher, which needs a main thread.
            FExCoreStatics.Configure(dispatcherFactory: static () => new InlineDispatcher());

            try
            {
                var appInfoProvider = Substitute.For<IAppInfoProvider>();
                appInfoProvider.EntryAssembly.Returns(typeof(SplashScreenWindowTests).Assembly);
                var appConfig = Substitute.For<IAppConfig>();
                appConfig.SplashDesign.FontFamily.Returns(new FontFamily("Segoe UI"));
                var hub = new FakeStatusHub();
                var baseline = CloseItCount();

                var window = new SplashScreenWindow(appInfoProvider,
                    Substitute.For<IAsyncHelper>(),
                    Substitute.For<IStatusService>(),
                    appConfig);
                window.AttachToStatusHub(hub);

                CloseItCount().ShouldBe(baseline + 1);
                hub.HandlerCount.ShouldBe(3);

                window.Close();

                CloseItCount().ShouldBe(baseline);
                hub.HandlerCount.ShouldBe(0);
            }
            finally
            {
                FExCoreStatics.SetDefaults();
            }
        });
}
