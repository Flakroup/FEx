using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace FEx.WPFx.Converters;

/// <summary>
/// Base class for mapping converters.
/// </summary>
/// <typeparam name="TIn">The type of the input value.</typeparam>
/// <typeparam name="TOut">The type of the output value.</typeparam>
public abstract class BaseMappingConverter<TIn, TOut> : IValueConverter
{
    /// <summary>
    /// The default parameter.
    /// </summary>
    private const int DefaultParameter = 0;

    /// <summary>
    /// The mappings;
    /// </summary>
    private Dictionary<object, Dictionary<TIn, TOut>> _mappings;

    /// <summary>
    /// Gets the default value.
    /// </summary>
    protected virtual TOut DefaultValue => default;

    /// <summary>
    /// Gets the mappings.
    /// </summary>
    private Dictionary<object, Dictionary<TIn, TOut>> Mappings
    {
        get
        {
            if (_mappings is null)
            {
                _mappings = new()
                {
                    [DefaultParameter] = InitializeMappings()
                };

                _mappings ??= InitializeParametrizedMappings();
            }

            return _mappings;
        }
    }

    /// <summary>
    /// Converts a value.
    /// </summary>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not null)
        {
            var casted = (TIn)value;

            var selectedMappings = Mappings[parameter ?? DefaultParameter];

            if (selectedMappings.TryGetValue(casted, out var result))
                return result;
        }

        return DefaultValue;
    }

    /// <summary>
    /// Converts a value.
    /// </summary>
    /// <param name="value">The value that is produced by the binding target.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        IDictionary<TIn, TOut> selectedMappings = Mappings[parameter ?? DefaultParameter];
        var selectedPair = selectedMappings.FirstOrDefault(sm => sm.Value.Equals((TOut)value));

        return selectedPair.Key;
    }

    /// <summary>
    /// Initializes the mappings.
    /// </summary>
    /// <returns>Init mappings.</returns>
    protected virtual Dictionary<TIn, TOut> InitializeMappings() => [];

    /// <summary>
    /// Initializes the parametrized mappings.
    /// </summary>
    protected virtual Dictionary<object, Dictionary<TIn, TOut>> InitializeParametrizedMappings() => [];
}