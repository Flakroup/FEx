using System;
using System.Globalization;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

public class SubtractionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is null)
            return value;

        var subBy = int.Parse((string)parameter);
        var input = System.Convert.ToDouble(value);

        return input - subBy;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is null)
            return value;

        var sumBy = int.Parse((string)parameter);
        var input = System.Convert.ToDouble(value);

        return input + sumBy;
    }
}