using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.BaseObjects;
using FEx.MVVM.Abstractions;
using FEx.WPFx.Natives;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FEx.Imaging.Windows;

#pragma warning disable IDISP025 // may be subclassed
public class SizedImageCache : NotifyPropertyChanged, IDisposable
#pragma warning restore IDISP025
{
    private BitmapImage _cachedImage;
    private bool _isLoadingImage;
    private Action<BitmapImage> _imageUpdateAction;

    public CancellationTokenSource CancellationTokenSource { get; }

    public Action<BitmapImage> ImageUpdateAction
    {
        get => _imageUpdateAction;
        protected set => SetProperty(ref _imageUpdateAction, value);
    }

    public Task ImageUpdateActionTask { get; protected set; }

    public SemaphoreSlim LoadingSemaphore { get; }

    public BitmapImage CachedImage
    {
        get => _cachedImage;
        private set => SetProperty(ref _cachedImage, value);
    }

    public WidthAndHeight ImageSize { get; }

    public bool IsLoadingImage
    {
        get => _isLoadingImage;
        protected set => SetProperty(ref _isLoadingImage, value);
    }

    protected bool OwnCTS { get; }

    protected CachedImage Cache { get; }

    protected FileInfo CacheFile => Cache.Cache;

    private CancellationToken CancellationToken => CancellationTokenSource.Token;

    public SizedImageCache(CachedImage cachedImage,
                           WidthAndHeight size = null,
                           BitmapImage image = null,
                           CancellationTokenSource cancellationTokenSource = default)
    {
        if (cancellationTokenSource is not null)
        {
            CancellationTokenSource = cancellationTokenSource;
        }
        else
        {
            OwnCTS = true;
            CancellationTokenSource = new();
        }

        Cache = cachedImage;
        ImageSize = size ?? WidthAndHeight.Default;
        LoadingSemaphore = new(1, 1);

        if (image is not null)
            CachedImage = image;
    }

    public override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        OnChange(propertyName);
    }

    public void AddImageUpdateAction(Action<BitmapImage> updateAction) =>
        //todo implement list of observables
        ImageUpdateAction = updateAction;

    public void RemoveImageUpdateAction()
    {
        if (ImageUpdateAction is not null)
            ImageUpdateAction = null;
    }

    public async Task ReloadImageFromCacheFileAsync(bool refresh = false,
                                                    bool forceLoad = false,
                                                    bool forceMemoryStream = true)
    {
        try
        {
            CacheFile?.Refresh();

            if ((refresh || CachedImage is null)
                && CacheFile?.Exists == true)
            {
                await LoadingSemaphore.WaitAsync(CancellationToken);
                IsLoadingImage = true;

                CacheFile?.Refresh();

                if ((refresh || CachedImage is null)
                    && CacheFile?.Exists == true)
                    CachedImage =
                        await CommonWindowsImaging.GetBitmapImageFromFileAsync(CacheFile,
                            ImageSize,
                            forceLoad,
                            forceMemoryStream);
            }
        }
        catch
        {
            CacheFile?.Delete();
            CachedImage = null;

            throw;
        }
        finally
        {
            if (IsLoadingImage)
            {
                IsLoadingImage = false;
                LoadingSemaphore.Release();
            }
        }
    }

    private void OnChange(string propertyName)
    {
        if (CachedImage is not null
            && ImageUpdateAction is not null
            && propertyName.IsIn(nameof(CachedImage), nameof(ImageUpdateAction)))
            ImageUpdateActionTask = AsyncStatics.ExecuteOnThreadPoolAsync(() => ImageUpdateAction(CachedImage),
                AsyncOptions.ImmediateStart, CancellationToken);
    }

    #region IDisposable
    public void Dispose()
    {
        LoadingSemaphore?.Dispose();

        if (OwnCTS)
#pragma warning disable IDISP007 // conditional on OwnCTS ownership flag
            CancellationTokenSource.Dispose();
#pragma warning restore IDISP007
    }
    #endregion
}
