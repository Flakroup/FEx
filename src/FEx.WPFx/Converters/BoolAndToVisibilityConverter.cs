using System;
using System.Globalization;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

public class BoolAndToVisibilityConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var bac = new BoolAndConverter();
        var btv = new BoolToVisibilityConverter();

        return btv.Convert(bac.Convert(values, typeof(bool), parameter, culture), targetType, parameter, culture);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("Cannot convert back");
}