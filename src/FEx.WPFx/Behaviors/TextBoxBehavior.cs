using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FEx.WPFx.Behaviors;

public class TextBoxBehavior
{
    public static readonly DependencyProperty SelectAllTextOnFocusProperty =
        DependencyProperty.RegisterAttached("SelectAllTextOnFocus",
            typeof(bool),
            typeof(TextBoxBehavior),
            new UIPropertyMetadata(false, OnSelectAllTextOnFocusChanged));

    public static bool GetSelectAllTextOnFocus(TextBox textBox) => (bool)textBox.GetValue(SelectAllTextOnFocusProperty);

    public static void SetSelectAllTextOnFocus(TextBox textBox, bool value) => textBox.SetValue(SelectAllTextOnFocusProperty, value);

    private static void OnSelectAllTextOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            return;

        var b = e.NewValue as bool?;

        if (!b.HasValue)
            return;

        if (b.Value)
        {
            textBox.GotFocus += SelectAll;
            textBox.PreviewMouseDown += IgnoreMouseButton;
        }
        else
        {
            textBox.GotFocus -= SelectAll;
            textBox.PreviewMouseDown -= IgnoreMouseButton;
        }
    }

    private static void SelectAll(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox textBox)
            return;

        textBox.SelectAll();
    }

    private static void IgnoreMouseButton(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBox textBox
            || !textBox.IsReadOnly && textBox.IsKeyboardFocusWithin)
            return;

        e.Handled = true;
        textBox.Focus();
    }
}
