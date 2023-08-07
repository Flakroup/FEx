using FEx.Abstractions;
using System;
using System.Diagnostics;

namespace FEx.Extensions.Base.Implementations;

public class DebugExceptionHandler : ExceptionHandlerBase
{
    protected override void HandleException(Exception exception)
    {
        Debug.WriteLine(exception.ToString());
    }
}