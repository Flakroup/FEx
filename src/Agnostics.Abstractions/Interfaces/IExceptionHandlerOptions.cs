using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>Options controlling how exceptions raised by background work are handled.</summary>
public interface IExceptionHandlerOptions
{
    /// <summary>Gets or sets a value indicating whether the user should be informed about the exception.</summary>
    bool InformUser { get; set; }
    /// <summary>Gets or sets a value indicating whether the handler should wait for the exception to be handled.</summary>
    bool Wait { get; set; }
    /// <summary>Gets or sets a value indicating whether the exception should be excluded from reporting.</summary>
    bool DoNotReport { get; set; }
    /// <summary>Gets or sets custom, handler-specific options.</summary>
    IDictionary<string, object> Custom { get; set; }
}