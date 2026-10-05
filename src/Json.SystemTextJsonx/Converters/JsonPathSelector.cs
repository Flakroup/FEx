using System;
using System.Globalization;
using System.Text.Json;

namespace FEx.Json.SystemTextJsonx.Converters;

/// <summary>
/// The subset of JSONPath <see cref="JsonPathConverter{T}" /> evaluates: an optional <c>$</c> root, dotted member names
/// (<c>a.b</c>), quoted members (<c>['a b']</c>) and array indexes (<c>items[0]</c>). Like Newtonsoft's
/// <c>SelectToken</c>, a path that matches nothing selects nothing. Wildcards, recursive descent, slices, unions and
/// filters select more than one token, which a single property cannot hold, and throw <see cref="NotSupportedException" />.
/// </summary>
public static class JsonPathSelector
{
    public static bool TrySelect(JsonElement root, string path, out JsonElement result)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        result = root;
        var index = path.StartsWith("$", StringComparison.Ordinal) ? 1 : 0;

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
                    var close = path.IndexOf(']', index);

                    if (close < 0)
                        throw Unsupported(path);

                    var segment = path.Substring(index + 1, close - index - 1).Trim();
                    index = close + 1;

                    if (!TryStep(ref result, segment, path))
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

    private static bool TryStep(ref JsonElement current, string segment, string path)
    {
        if (segment.Length >= 2
            && segment[0] is '\'' or '"'
            && segment[segment.Length - 1] == segment[0])
            return TryGetMember(ref current, segment.Substring(1, segment.Length - 2));

        if (!int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var position))
            throw Unsupported(path);

        if (current.ValueKind != JsonValueKind.Array
            || position >= current.GetArrayLength())
            return false;

        current = current[position];

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
