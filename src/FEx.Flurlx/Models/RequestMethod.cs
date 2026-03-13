using System.Diagnostics.CodeAnalysis;

namespace FEx.Flurlx.Models;

/// <summary>HTTP method to use when making requests</summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum RequestMethod
{
    GET,
    POST,
    PUT,
    DELETE,
    HEAD,
    OPTIONS,
    PATCH,
    MERGE,
    COPY
}