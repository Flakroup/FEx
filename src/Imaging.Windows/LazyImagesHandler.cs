using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Models;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Collections.Concurrent;
using FEx.MVVM.Abstractions;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FEx.Imaging.Windows;

public class LazyImagesHandler
{
    public ConcurrentObservableDictionary<string, Task<bool>> ImagesTasks { get; }
    public IFilesCacheService Cache { get; }

    public LazyImagesHandler(IFilesCacheService filesCacheService)
    {
        ImagesTasks = [];
        Cache = filesCacheService;
    }

    public bool AnyOtherImageTaskIsRunning(string key) => ImagesTasks.Where(x => x.Key != key).Select(x => x.Value).Any(x => x.IsRunning());

    public async Task<bool> GetImageTaskAsync(string propertyName) => await ImagesTasks.TryGetKeyValue(propertyName);

    public bool AddEnsureImageTask(string propertyName,
                                   Uri imageUrl,
                                   Action<BitmapImage> imageSetAction,
                                   WidthAndHeight size = null,
                                   Action beforeAction = null,
                                   Action afterAction = null,
                                   WebRequestParams pars = null,
                                   bool refresh = false,
                                   bool forceLoad = true,
                                   bool forceMemoryStream = false)
    {
        if (imageUrl is not null
            && (!ImagesTasks.ContainsKey(propertyName) || ImagesTasks[propertyName]?.IsFinished() != false))
        {
            _ = ImagesTasks.AddOrUpdateValue(propertyName,
                () => Task.Run(() => EnsureImageAsync(imageUrl,
                    imageSetAction,
                    size,
                    beforeAction,
                    afterAction,
                    pars,
                    refresh,
                    forceLoad,
                    forceMemoryStream)));

            return true;
        }

        return false;
    }

    private async Task<bool> EnsureImageAsync(Uri imageUrl,
                                              Action<BitmapImage> imageSetAction,
                                              WidthAndHeight size,
                                              Action beforeAction,
                                              Action afterAction,
                                              WebRequestParams pars,
                                              bool refresh,
                                              bool forceLoad = true,
                                              bool forceMemoryStream = false)
    {
        try
        {
            beforeAction?.Invoke();
            BitmapImage img = await Cache.GetImageAsync(imageUrl, size, pars, refresh, forceLoad, forceMemoryStream);
            imageSetAction(img);

            return true;
        }
        catch (WebException ex)
        {
            if (ex.Status != WebExceptionStatus.ProtocolError)
                ex.HandleException(false);
        }
        catch (Exception ex)
        {
            ex.HandleException(false);
        }
        finally
        {
            afterAction?.Invoke();
        }

        return false;
    }
}
