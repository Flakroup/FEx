using FEx.Legacy.Web;
using System.Collections.Generic;

namespace FEx.AzureDevOpsx;

/// <summary>
///     Wrapper class for handling TFS REST API according to this documentation:
///     <see href="https://www.visualstudio.com/en-us/docs/integrate/api/overview" />
///     Methods reserved for Git have prefix Git. Those for Tfvc have prefix Tfvc. Other are universal.
/// </summary>
public static class TfsWebApi
{
    static TfsWebApi()
    {
        WebServices.StatusCodes = new Dictionary<int, string>
        {
            { 200, "Success, and there is a response body." },
            { 201, "Success, when creating resources." },
            { 204, "Success, and there is no response body. For example, you'll get this when you delete a resource." },
            { 400, "The parameters in the URL or in the request body aren't valid." },
            { 403, "The authenticated user doesn't have permission to perform the operation." },
            { 404, "The resource doesn't exist, or the authenticated user doesn't have permission to see that it exists." },
            { 409, "There's a conflict between the request and the state of the data on the server. For example, if you attempt to submit a pull request and there is already a pull request for the commits." }
        };
    }
}
