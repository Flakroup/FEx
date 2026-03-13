using System;

namespace FEx.Agnostics.Abstractions.Helpers;

public static class HashHelper
{
    public static string GetHashString(this byte[] hash,
                                       bool removeDashes = true,
                                       bool toLower = true,
                                       bool asBase64String = false)
    {
        string hashString = null;

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