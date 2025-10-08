using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Interfaces;

public interface IExceptionHandlerOptions
{
    bool InformUser { get; set; }
    bool Wait { get; set; }
    bool DoNotReport { get; set; }
    IDictionary<string, object> Custom { get; set; }
}