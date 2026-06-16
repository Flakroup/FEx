using FEx.MVVM.Abstractions;
using FEx.WPFx.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FEx.WPFx.Models;

public class AppConfig : IAppConfig
{
    public IViewDesign MainDesign { get; protected set; }
    public IViewDesign SplashDesign { get; protected set; }
    public string SplashResourceName { get; protected set; }
    public WidthAndHeight SplashSize { get; protected set; }
    public string ApplicationLogoResourceName { get; protected set; }
    public string WindowIconName { get; protected set; }
    public ImageSource ApplicationLogo { get; protected set; }
    public Image WindowIcon { get; protected set; }
    public string ChangelogWindowHeader { get; protected set; }

    public AppConfig()
        : this(null, null, null, null, null, null, null)
    {
    }

    public AppConfig(IViewDesign mainDesign, IViewDesign splashDesign)
        : this(mainDesign, splashDesign, null, null, null, null, null)
    {
    }

    public AppConfig(IViewDesign mainDesign,
                     IViewDesign splashDesign,
                     string splashResourceName,
                     WidthAndHeight splashSize,
                     string applicationLogoResourceName,
                     string windowIconName,
                     string changelogWindowHeader)
    {
        MainDesign = mainDesign ?? new ViewDesign();
        SplashDesign = splashDesign ?? new ViewDesign();
        SplashResourceName = splashResourceName;
        SplashSize = splashSize;
        ApplicationLogoResourceName = applicationLogoResourceName;
        WindowIconName = windowIconName;
        ChangelogWindowHeader = changelogWindowHeader;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        ApplicationLogo = GetApplicationLogo();
        WindowIcon = GetWindowIcon();
    }

    protected virtual TResource GetApplicationResource<TResource>(string resourceName,
                                                                  Func<object, TResource> converter)
        where TResource : class
    {
        var resource = Application.Current.TryFindResource(resourceName);

        return resource is null
            ? null
            : converter(resource);
    }

    protected virtual BitmapSource GetDefaultApplicationLogo() =>
        BitmapSource.Create(2,
            2,
            96,
            96,
            PixelFormats.Indexed1,
            new(new List<Color>
            {
                Colors.Transparent
            }),
            new byte[] { 0, 0, 0, 0 },
            1);

    protected Image GetWindowIcon() =>
        WindowIconName is not null
            ? GetApplicationResource(WindowIconName, GetImage)
            : null;

    protected ImageSource GetApplicationLogo() =>
        ApplicationLogoResourceName is not null
            ? GetApplicationResource(ApplicationLogoResourceName, GetImageSource)
            : GetDefaultApplicationLogo();

    private static ImageSource GetImageSource(object resource) =>
        resource switch
        {
            ImageSource imageSource => imageSource,
            Image image => image.Source,
            _ => null
        };

    private static Image GetImage(object resource) =>
        resource switch
        {
            ImageSource imageSource => new()
            {
                Source = imageSource
            },
            Image image => image,
            _ => null
        };
}