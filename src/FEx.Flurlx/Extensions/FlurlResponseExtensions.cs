using Flurl.Http;
using System.Net;

namespace FEx.Flurlx.Extensions;

public static class FlurlResponseExtensions
{
    /// <summary>Gets a value that indicates if the HTTP response was successful.</summary>
    /// <returns>
    /// <see langword="true" /> if <see cref="P:Flurl.Http.IFlurlResponse.StatusCode" /> was in the range
    /// 200-299; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsSuccessStatusCode(this IFlurlResponse response) =>
        response?.StatusCode is >= (int)HttpStatusCode.OK and <= 299;
}