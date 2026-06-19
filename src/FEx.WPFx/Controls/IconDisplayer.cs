using FEx.Agnostics.Abstractions.Extensions;
using FEx.WPFx.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FEx.WPFx.Controls;

/// <summary>
///     Icon displayer control.
/// </summary>
public class IconDisplayer : TextBlock
{
    /// <summary>
    ///     Internal cache of available icons
    /// </summary>
    private static readonly ConcurrentDictionary<KeyValuePair<Type, Enum>, char> IconsCache = new();

    /// <summary>
    ///     The label property.
    /// </summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(IconDisplayer), new(TextChanged));

    /// <summary>
    ///     The icon property.
    /// </summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(Enum), typeof(IconDisplayer), new(TextChanged));

    /// <summary>
    ///     Gets or sets the label.
    /// </summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    ///     Gets or sets the icon.
    /// </summary>
    public Enum Icon
    {
        get => (Enum)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    ///     Gets the copy text.
    /// </summary>
    public string CopyText => $"[{Icon.GetAltText()}] {Label}";

    /// <summary>
    ///     Initializes a new instance of the <see cref="IconDisplayer" /> class.
    /// </summary>
    public IconDisplayer()
    {
        Loaded += IconDisplayerLoaded;
    }

    /// <summary>
    ///     Icons the changed handler.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The DependencyPropertyChangedEventArgs instance containing the event data.</param>
    public static void TextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var iconDisplayer = (IconDisplayer)sender;
        iconDisplayer.UpdateText();
    }

    /// <summary>
    ///     Updates the text.
    /// </summary>
    public void UpdateText()
    {
        var key = new KeyValuePair<Type, Enum>(null, Icon);

        if (Icon is not null)
            key = new(Icon.GetType(), Icon);

        if (!IconsCache.TryGetValue(key, out var charcode))
        {
            charcode = Icon.GetIconCharacter();
            IconsCache.AddOrUpdateValue(key, charcode);
        }

        Text = $"{charcode}{Label}";
    }

    /// <summary>
    ///     Handles the Loaded event of the IconDisplayer control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
    private void IconDisplayerLoaded(object sender, RoutedEventArgs e) => UpdateText();
}
