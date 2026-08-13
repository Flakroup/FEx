using FEx.Agnostics.Abstractions.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

/// <summary>
/// Represents the converter that converts the inverse of a Boolean values to and from System.Windows.Visibility
/// enumeration values.
/// </summary>
/// <seealso cref="IValueConverter" />
public class CollectionIsNotNullOrEmptyToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts a Boolean value to a System.Windows.Visibility enumeration value.
    /// </summary>
    /// <param name="value">
    /// The Boolean value to convert. This value can be a standard Boolean value
    /// or a nullable Boolean value.
    /// </param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// System.Windows.Visibility.Visible if value is false; otherwise, System.Windows.Visibility.Collapsed.
    /// </returns>
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<object> res)
            return res.IsNotNullOrEmptyEnumerable();

        return false;
    }

    /// <summary>
    /// Converts a System.Windows.Visibility enumeration value to a Boolean value.
    /// </summary>
    /// <param name="value">The value that is produced by the binding target. A System.Windows.Visibility enumeration value.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// A converted value. False if value is System.Windows.Visibility.Visible; otherwise, true.
    /// </returns>
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => false;
}