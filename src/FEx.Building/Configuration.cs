using System;
using System.ComponentModel;
using System.Globalization;
using Nuke.Common.Tooling;

namespace FEx.Building;

[TypeConverter(typeof(ConfigurationTypeConverter))]
public class Configuration : Enumeration
{
    public static readonly Configuration Debug = new() { Value = nameof(Debug) };
    public static readonly Configuration Release = new() { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration) => configuration.Value;
}

public class ConfigurationTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            return stringValue.ToLowerInvariant() switch
            {
                "debug" => Configuration.Debug,
                "release" => Configuration.Release,
                _ => throw new ArgumentException($"Unknown configuration: {stringValue}")
            };
        }
        return base.ConvertFrom(context, culture, value);
    }
}
