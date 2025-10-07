using System.Collections.Generic;
using System.Windows;

namespace FEx.WPFx.Converters;

/// <summary>
/// Bool to visibility converter.
/// </summary>
public class BoolToVisibilityConverter : BaseMappingConverter<bool?, Visibility>
{
    /// <summary>
    /// Gets the default value.
    /// </summary>
    protected override Visibility DefaultValue => Visibility.Collapsed;

    /// <summary>
    /// Initializes the mappings.
    /// </summary>
    protected override Dictionary<bool?, Visibility> InitializeMappings() =>
        new()
        {
            [true] = Visibility.Visible
        };
}