using System;
using System.Globalization;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

public class DivisionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is null)
            return value;

        var divBy = int.Parse((string)parameter);
        var input = System.Convert.ToDouble(value);

        return input / divBy;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is null)
            return value;

        var mulBy = int.Parse((string)parameter);
        var input = System.Convert.ToDouble(value);

        return input * mulBy;
    }
}