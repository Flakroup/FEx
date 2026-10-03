using System;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Agnostics.Abstractions.Helpers;

/// <summary>Helpers for formatting hash bytes.</summary>
public static class HashHelper
{
    /// <summary>Formats hash bytes as text.</summary>
    /// <param name="hash">The hash bytes.</param>
    /// <param name="removeDashes">Whether to remove the dashes between hex bytes.</param>
    /// <param name="toLower">Whether to lower-case the hex text.</param>
    /// <param name="asBase64String">Whether to return Base64 instead of hex.</param>
    /// <returns>The formatted hash, or null when <paramref name="hash" /> is null.</returns>
    [return: NotNullIfNotNull(nameof(hash))]
    public static string? GetHashString(this byte[]? hash,
                                        bool removeDashes = true,
                                        bool toLower = true,
                                        bool asBase64String = false)
    {
        string? hashString = null;

        if (hash is not null)
        {
            if (asBase64String)
            {
                hashString = Convert.ToBase64String(hash);
            }
            else
            {
                hashString = BitConverter.ToString(hash);

                if (removeDashes)
                    hashString = hashString.Replace("-", string.Empty);

                if (toLower)
                    hashString = hashString.ToLower();
            }
        }

        return hashString;
    }
}