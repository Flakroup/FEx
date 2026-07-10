using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions.CustomEventArgs;
using System;
using System.Diagnostics;

namespace FEx.Core.Abstractions.Helpers;

public class DebugExceptionHandler : ExceptionHandlerBase
{
    /// <inheritdoc />
#pragma warning disable CS0067 // Event required by base class contract but not raised in debug handler
    public override event EventHandler<ExceptionEventArgs>? ExceptionOccured;
#pragma warning restore CS0067

    protected override void HandleException(Exception exception, IExceptionHandlerOptions? options) =>
        Debug.WriteLine(exception.ToString());
}