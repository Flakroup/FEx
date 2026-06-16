using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace FEx.WPFx.Controls;

public class VerticalTextBlock : TextBlock
{
    public new static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text),
        typeof(string),
        typeof(VerticalTextBlock),
        new(string.Empty, TextChangeHandler));

    public new string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void TextChangeHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var prop = d as VerticalTextBlock;
        var str = e.NewValue as string;

        if (str is not null
            && prop is not null)
        {
            var inlines = str.Select(x => new Run(x + Environment.NewLine));
            prop.Inlines.Clear();
            prop.Inlines.AddRange(inlines);
        }
    }
}