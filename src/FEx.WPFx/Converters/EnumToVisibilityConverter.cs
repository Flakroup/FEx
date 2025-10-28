using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

/// <summary>
/// Enum to visibility converter.
/// </summary>
public class EnumToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a value.
    /// </summary>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// A converted value. If the method returns null, the valid null value is used.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var result = value is not null
                     && parameter is not null
                     && value.ToString().Equals(parameter.ToString(), StringComparison.InvariantCultureIgnoreCase);

        return result
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <summary>
    /// Not implemented
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}