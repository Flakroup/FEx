using System;
using FEx.Abstractions.Interfaces;

namespace FEx.Abstractions;

public abstract class ExceptionHandlerBase : IExceptionHandler
{
    public virtual void Handle(Exception exception, IExceptionHandlerOptions options = null) =>
        HandleException(exception, options);

    protected abstract void HandleException(Exception exception, IExceptionHandlerOptions options);
}