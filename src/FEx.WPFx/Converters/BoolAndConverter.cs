using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

public class BoolAndConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var booleans = values.Select(static x =>
            {
                var b = x as bool?;

                return b == true;
            })
            .ToArray();

        return booleans.All(static x => x);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("Cannot convert back");
}