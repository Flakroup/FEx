using FEx.WPFx.Extensions;
using System.Windows;
using System.Windows.Controls;

namespace FEx.WPFx.Controls;

/// <summary>
/// Interaction logic for BusyIndicator.xaml
/// </summary>
public partial class BusyIndicator : ContentControl
{
    public static readonly DependencyProperty BusyContentProperty =
        DependencyPropertyExtensions.CreateDependencyProperty<BusyIndicator, object>(view => view.BusyContent);

    public static readonly DependencyProperty IsBusyProperty =
        DependencyPropertyExtensions.CreateDependencyProperty<BusyIndicator, bool>(view => view.IsBusy,
            propertyChanged: t => t.view.Visibility = SetVisibility(t.newValue));

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public object BusyContent
    {
        get => GetValue(BusyContentProperty);
        set => SetValue(BusyContentProperty, value);
    }

    public BusyIndicator()
    {
        InitializeComponent();
    }

    private static Visibility SetVisibility(bool isBusy) =>
        isBusy
            ? Visibility.Visible
            : Visibility.Collapsed;
}