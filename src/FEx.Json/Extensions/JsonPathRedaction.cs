using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FEx.Json.Extensions;

/// <summary>
/// Strips data from a Newtonsoft.Json path by walking it against the target type's contracts. A dictionary key is
/// data, but Newtonsoft quotes a key only when it holds special characters - <c>password123</c> is written exactly
/// like a property name - so the path text alone cannot tell the two apart; the contract can. Dictionary keys
/// become <see cref="Placeholder" />, array indices and declared property names stay, and from the first segment
/// the contracts cannot vouch for (a <c>JToken</c> or <c>object</c> member, a converter, an unknown name) every
/// remaining segment is redacted. A converter is one from an attribute (on the type, the member or the containing
/// type's items), one from the serializer settings that can convert the type, or a built-in one the contract resolver
/// assigned, exactly as Newtonsoft.Json picks it.
/// </summary>
internal static class JsonPathRedaction
{
    internal const string Placeholder = "[*]";

    internal static string Redact(string path, Type targetType, JsonSerializerSettings? settings)
    {
        var builder = new StringBuilder(path.Length);

        try
        {
            // The serializer JsonConvert builds for these settings: JsonConvert.DefaultSettings, then the settings.
            var serializer = JsonSerializer.CreateDefault(settings);
            var contract = serializer.ContractResolver.ResolveContract(targetType);
            var position = 0;

            while (position < path.Length)
            {
                if (!TryReadSegment(path, ref position, out var segment))
                {
                    builder.Append(Placeholder);

                    break;
                }

                contract = contract is null
                    ? Redacted(builder)
                    : Append(builder, segment, contract, serializer);
            }
        }
        catch (Exception)
        {
            // A resolver or converter that throws here would replace the caller's exception with this one; fail
            // closed instead.
            return Placeholder;
        }

        return builder.ToString();
    }

    private static JsonContract? Append(StringBuilder builder,
                                        Segment segment,
                                        JsonContract contract,
                                        JsonSerializer serializer)
    {
        // Converter, then the settings' converters, then InternalConverter: the order Newtonsoft.Json reads with. The
        // last is a built-in converter (DataTable, DataSet, XmlNode, KeyValuePair, ...) the resolver picked.
        if (contract.Converter is not null
            || serializer.Converters.Any(converter => converter.CanConvert(contract.UnderlyingType))
            || contract.InternalConverter is not null)
        {
            return Redacted(builder);
        }

        var child = AppendSegment(builder, segment, contract, serializer.ContractResolver);

        // A container's item converter reads every item or member below it, so its contracts vouch for nothing there.
        return contract is JsonContainerContract { ItemConverter: not null } ? null : child;
    }

    private static JsonContract? AppendSegment(StringBuilder builder,
                                               Segment segment,
                                               JsonContract contract,
                                               IContractResolver contractResolver)
    {
        if (segment.Index is { } index)
        {
            if (contract is not JsonArrayContract arrayContract)
                return Redacted(builder);

            builder.Append('[').Append(index.ToString(CultureInfo.InvariantCulture)).Append(']');

            return contractResolver.ResolveContract(arrayContract.CollectionItemType ?? typeof(object));
        }

        switch (contract)
        {
            case JsonDictionaryContract dictionaryContract:
                builder.Append(Placeholder);

                return contractResolver.ResolveContract(dictionaryContract.DictionaryValueType ?? typeof(object));
            case JsonObjectContract objectContract:
                var property = objectContract.Properties.GetClosestMatchProperty(segment.Name!);

                if (property is null || property.Ignored)
                    return Redacted(builder);

                if (builder.Length > 0 && !segment.IsQuoted)
                    builder.Append('.');

                builder.Append(segment.Text);

                return property.Converter is null && property.ItemConverter is null && property.PropertyType is not null
                    ? contractResolver.ResolveContract(property.PropertyType)
                    : null;
            default:
                return Redacted(builder);
        }
    }

    private static JsonContract? Redacted(StringBuilder builder)
    {
        builder.Append(Placeholder);

        return null;
    }

    // Newtonsoft writes a property as `name` (`.name` after another segment), or as `['name']` with JavaScript
    // escapes when it holds special characters, and an array item as `[index]`.
    private static bool TryReadSegment(string path, ref int position, [NotNullWhen(true)] out Segment? segment)
    {
        segment = null;

        if (path[position] == '.')
            position++;

        var start = position;

        if (position + 1 < path.Length && path[position] == '[' && path[position + 1] == '\'')
            return TryReadQuotedName(path, ref position, out segment);

        if (position < path.Length && path[position] == '[')
        {
            var end = path.IndexOf(']', position);

            if (end < 0
                || !int.TryParse(path.Substring(position + 1, end - position - 1),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var index))
            {
                return false;
            }

            position = end + 1;
            segment = new(path.Substring(start, position - start), null, index, false);

            return true;
        }

        while (position < path.Length && path[position] != '.' && path[position] != '[')
            position++;

        if (position == start)
            return false;

        var name = path.Substring(start, position - start);
        segment = new(name, name, null, false);

        return true;
    }

    private static bool TryReadQuotedName(string path,
                                          ref int position,
                                          [NotNullWhen(true)] out Segment? segment)
    {
        segment = null;
        var start = position;
        var i = position + 2;

        while (i < path.Length)
        {
            if (path[i] == '\\')
            {
                i += 2;

                continue;
            }

            if (path[i] != '\'')
            {
                i++;

                continue;
            }

            if (i + 1 >= path.Length || path[i + 1] != ']')
                return false;

            position = i + 2;

            segment = new(path.Substring(start, position - start),
                Regex.Unescape(path.Substring(start + 2, i - start - 2)),
                null,
                true);

            return true;
        }

        return false;
    }

    private sealed class Segment(string text, string? name, int? index, bool isQuoted)
    {
        public string Text { get; } = text;

        public string? Name { get; } = name;

        public int? Index { get; } = index;

        public bool IsQuoted { get; } = isQuoted;
    }
}
