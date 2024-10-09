using FEx.Abstractions.Interfaces;
using FEx.Basics.Collections.Concurrent;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.Controls;
using FEx.WPFx.Implementations;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System.Windows.Media.Imaging;

namespace FEx.WPFx;

[Register(typeof(DispatcherContextExecutor), typeof(IFExDispatcher))]
[Register(typeof(WpfMessagePopupService), typeof(IMessagePopupService))]
[Register(typeof(FExWpfx), Scope.SingleInstance, typeof(IFExInitialize))]
[Register(typeof(Splash), Scope.SingleInstance, typeof(Splash), typeof(IFExPriorityInitialize))]
[Register(typeof(FileSystemIconsProvider))]
[Register(typeof(FExMemoryCache<string, BitmapSource>),
    Scope.SingleInstance,
    typeof(IFExMemoryCache<string, BitmapSource>))]
[Register(typeof(SplashScreenWindow))]
public class FExWpfxModule
{
    public static void AddServices(IFExWpfxContainer container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IAppConfig>(container);
        services.AddSingletonServiceUsingContainer<IFExMemoryCache<string, BitmapSource>>(container);
        services.AddSingletonServiceUsingContainer<Splash>(container);

        services.AddTransientServiceUsingContainer<FileSystemIconsProvider>(container);
    }
}