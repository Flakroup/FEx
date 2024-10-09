using FEx.Extensions.DateTimes;
using System;
using System.Globalization;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

public class LongToTimeStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
            return null;

        var time = System.Convert.ToDouble(value);

        return time.GetTime();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}