using FEx.Agnostics.Abstractions.Extensions;
using System.Globalization;
using System.Security.Principal;

namespace FEx.Platforms.Windows.Extensions;

/// <summary>
/// Extension to extract various parts of the identity name.
/// </summary>
public static class IdentityExtensions
{
    /// <summary>
    /// Extracts the SAMAccountName part of the full identity name
    /// </summary>
    /// <param name="user">The identity</param>
    /// <returns>The SAMAccountName</returns>
    public static string SamAccountName(this IIdentity user)
    {
        var name = user.Name.Guard(nameof(user.Name));
        var i = name.IndexOf('\\');

        return i > -1
            ? name.Substring(i + 1)
            : name;
    }

    /// <summary>
    /// Extracts the domain name part of the full identity name
    /// </summary>
    /// <param name="user">The identity</param>
    /// <returns>The domain name</returns>
    public static string Domain(this IIdentity user)
    {
        var name = user.Name.Guard(nameof(user.Name));
        var i = name.IndexOf('\\');

        return i > -1
            ? name.Substring(0, i)
            : string.Empty;
    }

    /// <summary>
    /// Extracts the SAMAccountName part of the full identity name and formats it the way the activity log wants it.
    /// </summary>
    /// <param name="user">The identity</param>
    /// <returns>The formatted SAMAccountName</returns>
    public static string LogFormattedName(this IIdentity user) =>
        user.SamAccountName().ToUpper(CultureInfo.InvariantCulture);
}