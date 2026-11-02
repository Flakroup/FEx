using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;

namespace FEx.WPFx.Fluent.Views;

/// <summary>
/// The "?" beside a button or field that says why it is unavailable or what it means - in place of a coloured
/// sentence in the layout. It is there only while it has a sentence (<see cref="Text"/>), and it is a tab stop that
/// opens its tooltip on keyboard focus, so it works without a mouse.
/// </summary>
public partial class HintMark : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(HintMark),
            new PropertyMetadata(string.Empty, (d, _) => ((HintMark)d).Refresh()));

    private readonly ToolTip _tip = new() { Placement = PlacementMode.Bottom, StaysOpen = true };

    public HintMark()
    {
        InitializeComponent();
        _tip.PlacementTarget = this;
        ToolTip = _tip;
        GotKeyboardFocus += (_, _) =>
        {
            ring.SetResourceReference(Shape.StrokeProperty, "Accent");
            _tip.IsOpen = Visibility == Visibility.Visible;
        };
        LostKeyboardFocus += (_, _) =>
        {
            ring.SetResourceReference(Shape.StrokeProperty, "Muted");
            _tip.IsOpen = false;
        };
    }

    /// <summary>The sentence; empty hides the mark.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    private void Refresh()
    {
        var text = (string?)GetValue(TextProperty) ?? string.Empty; // a binding may hand over null past the CLR setter
        var shown = !string.IsNullOrWhiteSpace(text);
        _tip.Content = text;
        AutomationProperties.SetName(this, text);
        Visibility = shown ? Visibility.Visible : Visibility.Collapsed;
        if (!shown)
            _tip.IsOpen = false;
    }
}
