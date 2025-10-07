using FEx.Basics.Helpers.Strings;
using System.Linq;

namespace FEx.Basics.Extensions;

/// <summary>
/// String extensions class.
/// </summary>
public static class StringExtensions
{
    public static string ToProperCase(this string input)
    {
        if (input.IsAllUpperOrAllLower())
            // fix the ALL UPPERCASE or all lowercase names
            return string.Join(" ", input.Split(' ').Select(ProperCaseHelper.WordToProperCase));

        // leave the CamelCase or Propercase names alone
        return input;
    }
}