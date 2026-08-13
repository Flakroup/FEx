using FEx.Agnostics.Abstractions.Interfaces;
using System.Collections.Generic;

namespace FEx.Core.Abstractions.Implementations;

public class ExceptionHandlerOptions : IExceptionHandlerOptions
{
    public bool InformUser { get; set; }
    public bool Wait { get; set; }
    public bool DoNotReport { get; set; }
    // IExceptionHandlerOptions.Custom is non-null-annotated in L0 but is optional (null when unset).
    public IDictionary<string, object> Custom { get; set; } = null!;
}