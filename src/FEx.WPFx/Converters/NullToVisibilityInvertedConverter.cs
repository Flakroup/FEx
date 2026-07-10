using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

public class NullToVisibilityInvertedConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}