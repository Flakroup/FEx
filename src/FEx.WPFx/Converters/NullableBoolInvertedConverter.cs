using System;
using System.Globalization;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

public class NullableBoolInvertedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
            return false;

        var b = value as bool?;

        return b.HasValue
            ? !b
            : throw new InvalidOperationException("The target must be a boolean");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("Cannot convert back");
}