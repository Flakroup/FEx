using System;
using System.Globalization;

namespace FEx.WPFx.Helpers;

public static class ResourceIdHelper
{
    public static string GetResourceIdFromRelativePath(string relPath)
    {
        var baseUri = new Uri("http://foo/");
        var sourceUri = new Uri(baseUri, relPath.Replace("#", "%23"));

        return GetResourceIdFromUri(baseUri, sourceUri);
    }

    private static string GetResourceIdFromUri(Uri baseUri, Uri sourceUri)
    {
        var str = string.Empty;

        if (!baseUri.IsAbsoluteUri
            || !sourceUri.IsAbsoluteUri
            || baseUri.Scheme != sourceUri.Scheme
            || baseUri.Host != sourceUri.Host)
            return str;

        var components1 = baseUri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
        var components2 = sourceUri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
        var lower1 = components1.ToLower(CultureInfo.InvariantCulture);
        var lower2 = components2.ToLower(CultureInfo.InvariantCulture);

        if (lower2.StartsWith(lower1, StringComparison.OrdinalIgnoreCase))
            str = lower2.Substring(lower1.Length);

        return str;
    }
}