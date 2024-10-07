using FEx.WPFx.SVG.SvgConverter;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FEx.WPFx.SVG;

public static class SvgCommon
{
    /// <summary>
    ///     Converts the SVG file to <see cref="DrawingImage" />.
    /// </summary>
    /// <param name="filepath">The path to SVG file.</param>
    /// <returns>
    ///     <see cref="DrawingImage" />
    /// </returns>
    public static DrawingImage ConvertSvgFileToDrawingImage(string filepath)
    {
        DrawingImage imgSrc = ConverterLogic.ConvertSvg(filepath).ConvertedObj;
        imgSrc.Freeze();

        return imgSrc;
    }

    /// <summary>
    ///     Converts the SVG to image source.
    /// </summary>
    /// <param name="svg">The SVG.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <returns>
    ///     ImageSource
    /// </returns>
    public static DrawingImage ConvertSvgToDrawingImage(string svg, string fileName)
    {
        DrawingImage imgSrc = ConverterLogic.ConvertSvg(svg, fileName).ConvertedObj;
        imgSrc.Freeze();

        return imgSrc;
    }

    public static MemoryStream ToMemoryStream(this DrawingImage drawingImage)
    {
        var target = drawingImage.ToRenderTargetBitmap();

        var stream = new MemoryStream();
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(target));
        encoder.Save(stream);

        return stream;
    }

    public static RenderTargetBitmap ToRenderTargetBitmap(this DrawingImage source)
    {
        var drawingVisual = new DrawingVisual();

        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            drawingContext.DrawImage(source, new(new(0, 0), new Size(source.Width, source.Height)));

        var bmp = new RenderTargetBitmap((int)source.Width, (int)source.Height, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(drawingVisual);

        return bmp;
    }

    //public static async Task<BitmapImage> ToBitmapImage(this DrawingImage source, bool forceLoad = false, int decodePixelHeight = 0, int decodePixelWidth = 0)
    //{
    //    return await source.ToMemoryStream().ToBitmapImage(forceLoad, false, decodePixelHeight, decodePixelWidth);
    //}
}