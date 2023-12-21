using FEx.Abstractions;
using System.Collections.Generic;

namespace FEx.Extensions.Base.Models;

public class ExceptionHandlerOptions : IExceptionHandlerOptions
{
    public bool InformUser { get; set; }
    public bool Wait { get; set; }
    public bool DoNotReport { get; set; }
    public IDictionary<string, object> Custom { get; set; }
}