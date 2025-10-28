using Flurl;
using Flurl.Http;
using Flurl.Util;
using System.Collections.Generic;
using System.Net.Http;

namespace FEx.Flurlx.Extensions;

public static class FlurlExtensions
{
    public static IFlurlRequest FixBooleanQueryParameters(this IFlurlRequest req)
    {
        (req?.Url).FixBooleanQueryParameters();

        return req;
    }

    public static Url FixBooleanQueryParameters(this Url url)
    {
        if (url?.QueryParams?.Count > 0)
        {
            var toReplace = new Dictionary<string, string>();

            foreach (var (name, value) in url.QueryParams)
            {
                var b = value as bool?;

                if (b.HasValue)
                    toReplace.Add(name, b.Value.ToString().ToLower());
            }

            foreach (var v in toReplace)
                url.QueryParams.AddOrReplace(v.Key, v.Value);
        }

        return url;
    }

    public static HttpContent StripCharsetQuotes(this HttpContent content)
    {
        var contentType = content?.Headers?.ContentType;

        if (contentType?.CharSet is not null)
            contentType.CharSet = contentType.CharSet.StripQuotes();

        return content;
    }
}