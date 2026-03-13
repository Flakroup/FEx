using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions.Interfaces;
using FEx.FileSystem;
using FEx.WPFx.Natives;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace FEx.WPFx;

public class FileSystemIconsProvider
{
    private readonly ISynchronizedAccessService _lockSrv;
    private readonly IFExMemoryCache<string, BitmapSource> _iconsCache;
    private readonly IFExDispatcher _dispatcher;

    public FileSystemIconsProvider(ISynchronizedAccessService synchronizedAccessService,
                                   IFExMemoryCache<string, BitmapSource> iconsCache,
                                   IFExDispatcher dispatcher)
    {
        _lockSrv = synchronizedAccessService;
        _iconsCache = iconsCache;
        _dispatcher = dispatcher;
    }

    public async Task<BitmapSource> GetFileIconAsync(string filePath, bool isIconAttachedToFile = true)
    {
        var key = isIconAttachedToFile
            ? Path.GetExtension(filePath)
            : filePath;

        if (key is null)
            return null;

        if (_iconsCache.TryGetValue(key, out var res)
            && res is not null)
            return res;

        await _lockSrv.WaitAsync(key);

        try
        {
            res = _iconsCache.TryGetKeyValue(key);

            if (res is null)
            {
                res = await _dispatcher.InvokeOnMainThreadAsync(() =>
                    GetBitmapSourceAsync(filePath, isIconAttachedToFile));

                _iconsCache.AddOrUpdateValue(key, res);
            }

            return res;
        }
        finally
        {
            _lockSrv.Release(key);
        }
    }

    private static async Task<BitmapSource> GetBitmapSourceAsync(string filePath, bool isIconAttachedToFile)
    {
        if (!isIconAttachedToFile)
            return await CommonWindowsImaging.GetBitmapImageFromFileAsync(filePath, new(16, 16));

        var fileResult = FileSystemUtilities.IsPathFile(filePath);
        var isFile = fileResult.IsSuccess && fileResult.Data;

        var icon = isFile!
            ? Icon.ExtractAssociatedIcon(filePath)
            : ShellIcon.GetLargeFolderIcon(); //todo cache large folder icon

        if (icon is null)
            return null;

        var bitmap = icon.ToBitmap();
        var hBitmap = bitmap.GetHbitmap();

        var res = Imaging.CreateBitmapSourceFromHBitmap(hBitmap,
            IntPtr.Zero,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        res.Freeze();

        return res;
    }
}