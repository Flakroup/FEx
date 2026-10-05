using System;
using System.Globalization;
using System.Text.Json;

namespace FEx.Json.SystemTextJsonx.Converters;

/// <summary>
/// The subset of JSONPath <see cref="JsonPathConverter{T}" /> evaluates: a <c>$</c> root (only when a <c>.</c>, a
/// <c>[</c> or the end of the path follows it, so <c>$schema</c> is a member name, as in Newtonsoft's
/// <c>SelectToken</c>), dotted member names (<c>a.b</c>), quoted members (<c>['a b']</c>, which may contain <c>]</c>)
/// and array indexes (<c>items[0]</c>). Like <c>SelectToken</c>, a path that matches nothing selects nothing, and so
/// does an index that is negative or out of range. Wildcards, recursive descent, slices, unions and filters select more
/// than one token, which a single property cannot hold, and throw <see cref="NotSupportedException" />.
/// </summary>
public static class JsonPathSelector
{
    public static bool TrySelect(JsonElement root, string path, out JsonElement result)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        result = root;
        var index = IsRoot(path) ? 1 : 0;

        while (index < path.Length)
        {
            switch (path[index])
            {
                case '.' when index + 1 < path.Length && path[index + 1] == '.':
                    throw Unsupported(path);
                case '.':
                    index++;

                    break;
                case '[':
                    if (!TryStep(ref result, path, ref index))
                        return false;

                    break;
                default:
                    var end = path.IndexOfAny(['.', '['], index);

                    if (end < 0)
                        end = path.Length;

                    var name = path.Substring(index, end - index);
                    index = end;

                    if (name == "*")
                        throw Unsupported(path);

                    if (!TryGetMember(ref result, name))
                        return false;

                    break;
            }
        }

        return true;
    }

    private static bool IsRoot(string path) =>
        path.Length > 0 && path[0] == '$' && (path.Length == 1 || path[1] is '.' or '[');

    /// <summary>Evaluates the bracket segment starting at <paramref name="index" /> and moves past it.</summary>
    private static bool TryStep(ref JsonElement current, string path, ref int index)
    {
        var open = index + 1;

        if (open < path.Length
            && path[open] is '\'' or '"')
        {
            // A quoted member ends at its closing quote, so it may contain ']'.
            var closeQuote = path.IndexOf(path[open], open + 1);

            if (closeQuote < 0
                || closeQuote + 1 >= path.Length
                || path[closeQuote + 1] != ']')
                throw Unsupported(path);

            index = closeQuote + 2;

            return TryGetMember(ref current, path.Substring(open + 1, closeQuote - open - 1));
        }

        var close = path.IndexOf(']', open);

        if (close < 0)
            throw Unsupported(path);

        var segment = path.Substring(open, close - open).Trim();
        index = close + 1;

        if (!IsInteger(segment))
            throw Unsupported(path);

        // A negative index or one past int range cannot exist in any array: it selects nothing.
        if (!int.TryParse(segment, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var position)
            || position < 0
            || current.ValueKind != JsonValueKind.Array
            || position >= current.GetArrayLength())
            return false;

        current = current[position];

        return true;
    }

    private static bool IsInteger(string segment)
    {
        var start = segment.StartsWith("-", StringComparison.Ordinal) ? 1 : 0;

        if (segment.Length == start)
            return false;

        for (var i = start; i < segment.Length; i++)
        {
            if (segment[i] is < '0' or > '9')
                return false;
        }

        return true;
    }

    private static bool TryGetMember(ref JsonElement current, string name)
    {
        if (current.ValueKind != JsonValueKind.Object
            || !current.TryGetProperty(name, out var member))
            return false;

        current = member;

        return true;
    }

    private static NotSupportedException Unsupported(string path) =>
        new($"The JSON path '{path}' is not supported: only member names, quoted members and array indexes are.");
}
