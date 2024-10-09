using System;
using System.Globalization;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

public class EnumToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null
            ? null
            : (object)Enum.GetValues(value.GetType()).GetValue(System.Convert.ToInt32(value)).ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}