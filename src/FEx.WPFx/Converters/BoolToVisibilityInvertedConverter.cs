using System;
using System.Globalization;

namespace FEx.WPFx.Converters;

/// <summary>
///     Bool to Visibility Invert Converter.
/// </summary>
public class BoolToVisibilityInvertedConverter : BoolToVisibilityConverter
{
    /// <summary>
    ///     Converts a value.
    /// </summary>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invertedValue = value as bool?;
        invertedValue = !invertedValue;

        return base.Convert(invertedValue, targetType, parameter, culture);
    }
}