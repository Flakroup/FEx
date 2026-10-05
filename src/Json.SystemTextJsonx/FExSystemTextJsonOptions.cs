using FEx.Json.SystemTextJsonx.Converters;
using FEx.Json.SystemTextJsonx.Resolvers;
using System;
#if NET
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace FEx.Json.SystemTextJsonx;

/// <summary>Builds the <see cref="JsonSerializerOptions" /> <see cref="FExSystemTextJsonSerializer" /> runs on.</summary>
public static class FExSystemTextJsonOptions
{
#if NET
    private const string ReflectionMessage =
        "Uses the reflection-based DefaultJsonTypeInfoResolver; trimmed and Native AOT applications pass their source-generated JsonSerializerContext to CreateDefault(IJsonTypeInfoResolver) instead.";
#endif

    /// <summary>
    /// <see cref="CreateDefault(IJsonTypeInfoResolver)" /> with the reflection-based
    /// <see cref="DefaultJsonTypeInfoResolver" />, which handles any type but is not trimming or Native AOT safe.
    /// </summary>
#if NET
    // Only .NET has the trimming annotations public; down-level targets are never trimmed.
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
#endif
    public static JsonSerializerOptions CreateDefault() => CreateDefault(new DefaultJsonTypeInfoResolver());

    /// <summary>
    /// Options matching the FEx.Json Newtonsoft defaults where System.Text.Json can: nulls are not written, property
    /// names match case-insensitively on read, and <see cref="double" /> goes through
    /// <see cref="ParseStringToDoubleConverter" />. Type metadata comes from <paramref name="typeInfoResolver" />, for
    /// example a source-generated <see cref="JsonSerializerContext" />.
    /// </summary>
    public static JsonSerializerOptions CreateDefault(IJsonTypeInfoResolver typeInfoResolver) =>
        new()
        {
            TypeInfoResolver = typeInfoResolver ?? throw new ArgumentNullException(nameof(typeInfoResolver)),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            Converters = { ParseStringToDoubleConverter.Singleton }
        };

    /// <summary>
    /// Copies <paramref name="options" /> and adds <paramref name="modifier" /> to its type info resolver, so the
    /// options passed in stay usable (and unlocked) on their own.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="options" /> has no <see cref="JsonSerializerOptions.TypeInfoResolver" />.</exception>
    public static JsonSerializerOptions WithDIConstruction(JsonSerializerOptions options, DIJsonTypeInfoModifier modifier)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (modifier is null)
            throw new ArgumentNullException(nameof(modifier));

        var resolver = options.TypeInfoResolver
                       ?? throw new ArgumentException(
                           "The options have no TypeInfoResolver: use FExSystemTextJsonOptions.CreateDefault, or set a source-generated JsonSerializerContext.",
                           nameof(options));

        return new(options)
        {
            TypeInfoResolver = resolver.WithAddedModifier(modifier.Modify)
        };
    }
}
