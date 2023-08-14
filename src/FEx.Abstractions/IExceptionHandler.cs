using System;

namespace FEx.Abstractions;

public interface IExceptionHandler
{
    void Handle(Exception exception, IExceptionHandlerOptions options = null);
}