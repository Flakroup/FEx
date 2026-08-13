using FEx.Agnostics.Abstractions.Interfaces;
using FEx.WPFx.Controls;
using StrongInject;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media.Imaging;

namespace FEx.WPFx.Abstractions.Interfaces;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExWpfxContainer : IContainer<FExWpfx>, IContainer<IAppConfig>, IContainer<Splash>,
    IContainer<IFExMemoryCache<string, BitmapSource>>, IContainer<SplashScreenWindow>,
    IContainer<FileSystemIconsProvider>
{
}