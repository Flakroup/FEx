using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Collections.Concurrent;
using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.Controls;
using FEx.WPFx.Implementations;
using FEx.WPFx.Models;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;
using System.Windows.Media.Imaging;

namespace FEx.WPFx;

[Register(typeof(FExWpfx), Scope.SingleInstance, typeof(FExWpfx), typeof(IFExInitialize))]
[Register(typeof(FExWpfxModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(Splash), Scope.SingleInstance, typeof(Splash), typeof(IFExPriorityInitialize))]
[Register(typeof(FExMemoryCache<string, BitmapSource>),
    Scope.SingleInstance,
    typeof(IFExMemoryCache<string, BitmapSource>))]
[Register(typeof(DispatcherContextExecutor), typeof(IFExDispatcher))]
[Register(typeof(WpfMessagePopupService), typeof(IMessagePopupService))]
[Register(typeof(FileSystemIconsProvider))]
[Register(typeof(SplashScreenWindow))]
public class FExWpfxModule : InitializeModule<IFExWpfxContainer, IServiceCollection>
{
    [Instance]
    public static IAppConfig AppConfig { get; } = new AppConfig(); //todo make it configurable

    protected override void RegisterServices(IFExWpfxContainer container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IAppConfig>(container);
        services.AddSingletonServiceUsingContainer<IFExMemoryCache<string, BitmapSource>>(container);
        services.AddSingletonServiceUsingContainer<Splash>(container);

        services.AddTransientServiceUsingContainer<FileSystemIconsProvider>(container);
    }
}