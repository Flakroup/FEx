using FEx.Agnostics.Abstractions.Extensions;
using System.IO;
using System.Windows.Media;

namespace FEx.WPFx.SVG.SvgConverter;

public class ConvertedSvgData
{
    private string? _xaml;
    private string? _svg;
    private string? _objectName;
    private DrawingImage? _convertedObj;

    // Set by the ConvertSvg factory methods before any Xaml/Svg/ConvertedObj access.
    public string Filepath { get; set; } = null!;

    public string Xaml
    {
        get => _xaml ??= ConverterLogic.SvgObjectToXaml(ConvertedObj, false, _objectName, false);
        set => _xaml = value;
    }

    public string Svg
    {
        get => _svg ??= File.ReadAllText(Filepath);
        set => _svg = value;
    }

    public DrawingImage ConvertedObj
    {
        get =>
            _convertedObj ??=
                (ConverterLogic.ConvertSvgToObject(this, ResultMode.DrawingImage, null, out _objectName, new()) as
                    DrawingImage).Guard(nameof(ConvertedObj));
        set => _convertedObj = value;
    }
}