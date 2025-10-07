using FEx.Abstractions.CustomEventArgs;
using FEx.Abstractions.Interfaces;
using System;
using System.Diagnostics;

namespace FEx.Abstractions.Implementations;

public class DebugExceptionHandler : ExceptionHandlerBase
{
    /// <inheritdoc />
    public override event EventHandler<ExceptionEventArgs> ExceptionOccured;

    protected override void HandleException(Exception exception, IExceptionHandlerOptions options) =>
        Debug.WriteLine(exception.ToString());
}