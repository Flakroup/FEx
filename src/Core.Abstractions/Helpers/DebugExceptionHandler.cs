using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions.CustomEventArgs;
using System;
using System.Diagnostics;

namespace FEx.Core.Abstractions.Helpers;

public class DebugExceptionHandler : ExceptionHandlerBase
{
    /// <inheritdoc />
    public override event EventHandler<ExceptionEventArgs> ExceptionOccured;

    protected override void HandleException(Exception exception, IExceptionHandlerOptions options) =>
        Debug.WriteLine(exception.ToString());
}