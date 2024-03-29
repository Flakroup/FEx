using System.Net;
using System.Net.Http;

namespace FEx.Webx.Extensions;

public static class HttpResponseExtensions
{
    public static string GetDefaultExtension(this HttpResponseMessage response) =>
        MimeTypesUtility.GetDefaultExtension(response.Content.Headers.ContentType.MediaType, response.GetFileName());

    public static string GetDefaultExtension(this HttpWebResponse response) =>
        MimeTypesUtility.GetDefaultExtension(response.ContentType.Split(';')[0], response.GetFileName());
}