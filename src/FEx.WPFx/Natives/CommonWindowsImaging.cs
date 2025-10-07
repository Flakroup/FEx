using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.Extensions.Base.Enums;
using FEx.MVVM.Abstractions;
using FEx.Platforms;
using FEx.Platforms.Abstractions.Interfaces;
using FEx.WPFx.Extensions;
using FEx.WPFx.SVG;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FEx.WPFx.Natives;

public static class CommonWindowsImaging
{
    private static ISynchronizedAccessService LockSrv => FExFoundation.SynchronizedAccessService;
    private static IRegistryService RegistrySrv => FExPlatforms.RegistryService;

    /// <summary>
    /// Converts the byte array to bitmap image.
    /// </summary>
    /// <param name="imageStream">The image stream.</param>
    /// <returns></returns>
    public static async Task<BitmapImage> ConvertStreamToBitmapImageAsync(Stream imageStream)
    {
        using var image = Image.FromStream(imageStream, true, false);

        return await image.ToBitmapImageAsync();
    }

    public static async Task<BitmapImage> GetBitmapImageFromFileAsync(string filePath,
                                                                      WidthAndHeight size = null,
                                                                      bool forceLoad = false,
                                                                      bool forceMemoryStream = true,
                                                                      bool lockOnFile = true) =>
        await GetBitmapImageFromFileAsync(new FileInfo(filePath), size, forceLoad, forceMemoryStream, lockOnFile);

    public static async Task<BitmapImage> GetBitmapImageFromFileAsync(FileInfo file,
                                                                      WidthAndHeight size = null,
                                                                      bool forceLoad = false,
                                                                      bool forceMemoryStream = true,
                                                                      bool lockOnFile = true)
    {
        BitmapImage res = null;

        file?.Refresh();

        if (file is not null)
            try
            {
                if (lockOnFile)
                    await LockSrv.WaitAsync(file.FullName);

                if (file.Exists
                    && file.Length > 0)
                {
                    Stream stream;

                    if (Path.GetExtension(file.FullName) == RegistrySrv.GetDefaultExtension(MediaTypes.ImageSvgXml))
                        stream = SvgCommon.ConvertSvgFileToDrawingImage(file.FullName).ToMemoryStream();
                    else
                        stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

                    //ToBitmapImageAsync handles stream disposal if needed
                    res = await stream.ToBitmapImageAsync(forceLoad,
                        forceMemoryStream,
                        size?.Height ?? 0,
                        size?.Width ?? 0);
                }
            }
            finally
            {
                if (lockOnFile)
                    LockSrv.Release(file.FullName);
            }

        return res;
    }
}