using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.Basics.Implementations;
using System;
using System.Linq;

namespace FEx.Basics.Extensions;

public static class ExceptionExtensions
{
    public static void HandleException(this Exception exception) => FExFoundation.ExceptionHandler.Handle(exception);

    public static void HandleException(this Exception exception, IExceptionHandlerOptions options) =>
        FExFoundation.ExceptionHandler.Handle(exception, options);

    public static void HandleException(this Exception ex,
                                       bool informUser = false,
                                       bool wait = false,
                                       bool doNotReport = false,
                                       params (string, object)[] custom)
    {
        var options = new ExceptionHandlerOptions
        {
            InformUser = informUser,
            Wait = wait,
            DoNotReport = doNotReport,
            Custom = custom?.ToDictionary(x => x.Item1, x => x.Item2)
        };

        ex.HandleException(options);
    }
}