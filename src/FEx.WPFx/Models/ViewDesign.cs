using FEx.Agnostics.BaseObjects;
using FEx.WPFx.Abstractions.Interfaces;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows.Media;

namespace FEx.WPFx.Models;

public class ViewDesign : NotifyPropertyChanged, IViewDesign
{
    private readonly Func<Brush>? _backgroundFactory;
    private readonly Func<Brush>? _controlBackgroundFactory;
    private readonly Func<Brush>? _foregroundFactory;
    private readonly Func<Brush>? _headerBackgroundFactory;
    private readonly Func<Brush>? _borderBackgroundFactory;

    private double _fontSize;

    // Assigned in the constructor via the FontFamily setter (SetProperty ref-assign the compiler cannot track).
    private FontFamily _fontFamily = null!;
    private Brush? _background;
    private Brush? _controlBackground;
    private Brush? _foreground;
    private Brush? _headerBackground;
    private Brush? _borderBackground;

    /// <summary>
    /// Gets or sets the size of the font.
    /// </summary>
    /// <value>
    /// The size of the font.
    /// </value>
    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, value);
    }

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set => SetProperty(ref _fontFamily, value);
    }

    /// <summary>
    /// Gets or sets the background.
    /// </summary>
    /// <value>
    /// The background.
    /// </value>
    public Brush? Background
    {
        get => _background;
        set => SetProperty(ref _background, value);
    }

    /// <summary>
    /// Gets or sets the control background.
    /// </summary>
    /// <value>
    /// The control background.
    /// </value>
    public Brush? ControlBackground
    {
        get => _controlBackground;
        set => SetProperty(ref _controlBackground, value);
    }

    /// <summary>
    /// Gets or sets the foreground.
    /// </summary>
    /// <value>
    /// The foreground.
    /// </value>
    public Brush? Foreground
    {
        get => _foreground;
        set => SetProperty(ref _foreground, value);
    }

    /// <summary>
    /// Gets or sets the header background.
    /// </summary>
    /// <value>
    /// The header background.
    /// </value>
    public Brush? HeaderBackground
    {
        get => _headerBackground;
        set => SetProperty(ref _headerBackground, value);
    }

    public Brush? BorderBackground
    {
        get => _borderBackground;
        set => SetProperty(ref _borderBackground, value);
    }

    public ViewDesign()
        : this(null, null, null, null, null, null, 13)
    {
    }

    public ViewDesign(Func<Brush>? backgroundFactory,
                      Func<Brush>? controlBackgroundFactory,
                      Func<Brush>? foregroundFactory,
                      Func<Brush>? headerBackgroundFactory,
                      Func<Brush>? borderBackgroundFactory)
        : this(backgroundFactory,
            controlBackgroundFactory,
            foregroundFactory,
            headerBackgroundFactory,
            borderBackgroundFactory,
            null,
            13)
    {
    }

    public ViewDesign(Func<Brush>? backgroundFactory,
                      Func<Brush>? controlBackgroundFactory,
                      Func<Brush>? foregroundFactory,
                      Func<Brush>? headerBackgroundFactory,
                      Func<Brush>? borderBackgroundFactory,
                      FontFamily? fontFamily,
                      double fontSize)
    {
        _backgroundFactory = backgroundFactory;
        _controlBackgroundFactory = controlBackgroundFactory;
        _foregroundFactory = foregroundFactory;
        _headerBackgroundFactory = headerBackgroundFactory;
        _borderBackgroundFactory = borderBackgroundFactory;

        // MaterialDesignFontExtension.ProvideValue tolerates a null service provider at construction time.
        FontFamily = fontFamily
                     ?? new MaterialDesignFontExtension().ProvideValue(null!) as FontFamily
                     ?? new FontFamily("Segoe UI");

        FontSize = fontSize;
    }

    public void Initialize()
    {
        Background = _backgroundFactory?.Invoke();
        ControlBackground = _controlBackgroundFactory?.Invoke();
        Foreground = _foregroundFactory?.Invoke();
        HeaderBackground = _headerBackgroundFactory?.Invoke();
        BorderBackground = _borderBackgroundFactory?.Invoke();
    }
}