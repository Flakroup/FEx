using FEx.Agnostics.Abstractions.Extensions;
using FEx.MVVM.Abstractions;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
#if NETFRAMEWORK
using FEx.WPFx.Natives;
#endif

namespace FEx.WPFx.Extensions;

public static class BitmapExtensions
{
    public static Task<BitmapImage> ToBitmapImageAsync(this Stream stream) => stream.ToBitmapImageAsync(false);

    public static Task<BitmapImage> ToBitmapImageAsync(this Stream stream, bool forceLoad) =>
        stream.ToBitmapImageAsync(forceLoad, true);

    public static Task<BitmapImage> ToBitmapImageAsync(this Stream stream, bool forceLoad, bool forceMemoryStream) =>
        stream.ToBitmapImageAsync(forceLoad, forceMemoryStream, 0, 0);

    public static async Task<BitmapImage> ToBitmapImageAsync(this Stream stream,
                                                             bool forceLoad,
                                                             bool forceMemoryStream,
                                                             int decodePixelHeight,
                                                             int decodePixelWidth)
    {
        if (forceMemoryStream && stream is not MemoryStream)
            stream = await stream.CopyToMemoryStreamAsync(true);

        return stream.ToBitmapImage(forceLoad, decodePixelHeight, decodePixelWidth);
    }

    public static BitmapImage ToBitmapImage(this Stream stream) => stream.ToBitmapImage(false);

    public static BitmapImage ToBitmapImage(this Stream stream, bool forceLoad) =>
        stream.ToBitmapImage(forceLoad, 0, 0);

    public static BitmapImage ToBitmapImage(this Stream stream,
                                            bool forceLoad,
                                            int decodePixelHeight,
                                            int decodePixelWidth)
    {
        if (stream.CanSeek
            && stream.Position != 0)
            stream.Seek(0, SeekOrigin.Begin);

        var result = new BitmapImage();
        result.BeginInit();

        if (decodePixelHeight > 0
            || decodePixelWidth > 0)
        {
            var originalSize = GetSize(stream);

            if (decodePixelHeight > 0)
                result.DecodePixelHeight = Math.Min(decodePixelHeight, originalSize.Height);

            if (decodePixelWidth > 0)
                result.DecodePixelWidth = Math.Min(decodePixelWidth, originalSize.Width);
        }

        if (forceLoad)
            // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
            // Force the bitmap to load right now so we can dispose the stream.
            result.CacheOption = BitmapCacheOption.OnLoad;

        result.StreamSource = stream;
        result.EndInit();

        if (result.CanFreeze)
            result.Freeze();

#pragma warning disable IDISP007 // intentional dispose after BitmapCacheOption.OnLoad
        if (forceLoad)
            stream.Dispose();
#pragma warning restore IDISP007

        // if you dispose of the memory stream here, the image will be toast (burnt toast)
        // (as the dispatcher won't have run yet).
        return result;
    }

    /// <summary>
    /// To the bitmap source.
    /// </summary>
    /// <param name="bitmap">The bitmap.</param>
    /// <returns></returns>
    public static BitmapSource ToBitmapSource(this Bitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();

        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(handle,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
#if NETFRAMEWORK
            NativeImagingMethods.DeleteBitmapObject(handle);
#endif
            // ReSharper disable RedundantAssignment
            handle = IntPtr.Zero;
            // ReSharper restore RedundantAssignment
        }
    }

    /// <summary>
    /// Saves to file.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="filePath">The file path.</param>
    public static void SaveToFile(this BitmapSource image, string filePath)
    {
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var fileStream = new FileStream(filePath, FileMode.Create);
        encoder.Save(fileStream);
    }

    public static Task<BitmapImage> ToBitmapImageAsync(this byte[] array) => array.ToBitmapImageAsync(false);

    public static async Task<BitmapImage> ToBitmapImageAsync(this byte[] array, bool forceLoad)
    {
        using var ms = new MemoryStream(array);

        return await ms.ToBitmapImageAsync(forceLoad);
    }

    /// <summary>
    /// Takes a bitmap and converts it to an image that can be handled by WPF ImageBrush
    /// </summary>
    /// <param name="image">A bitmap image</param>
    /// <param name="imageFormat">The format.</param>
    /// <param name="forceLoad">if set to <c>true</c> [force load].</param>
    /// <returns>
    /// The image as a BitmapImage for WPF
    /// </returns>
    public static Task<BitmapImage> ToBitmapImageAsync(this Image image) => image.ToBitmapImageAsync(null, false);

    public static Task<BitmapImage> ToBitmapImageAsync(this Image image, ImageFormat imageFormat) =>
        image.ToBitmapImageAsync(imageFormat, false);

    public static async Task<BitmapImage> ToBitmapImageAsync(this Image image, ImageFormat imageFormat, bool forceLoad)
    {
        //https://stackoverflow.com/questions/25326137/converting-bitmap-to-imagesource-made-my-images-background-black
        using var stream = new MemoryStream();
        imageFormat ??= ImageFormat.Png;

        image.Save(stream, imageFormat);

        return await stream.ToBitmapImageAsync(forceLoad);
    }

    public static WidthAndHeight GetSize(Stream originalStream)
    {
        using var stream = new MemoryStream();
        originalStream.CopyTo(stream);
        originalStream.Seek(0, SeekOrigin.Begin);

        var addedPhoto = new BitmapImage();
        addedPhoto.BeginInit();
        stream.Seek(0, SeekOrigin.Begin);
        addedPhoto.StreamSource = stream;
        addedPhoto.EndInit();

        return new(addedPhoto.PixelWidth, addedPhoto.PixelHeight);
    }
}