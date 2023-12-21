using FEx.Abstractions;
using FEx.Extensions.Base;
using FEx.Extensions.Base.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace FEx.Extensions;

public static class ExceptionExtensions
{
    private static readonly Func<Exception, StackTrace, Exception> SetStackTraceFunc =
        new Func<Func<Exception, StackTrace, Exception>>(() =>
        {
            ParameterExpression target = Expression.Parameter(typeof(Exception));
            ParameterExpression stack = Expression.Parameter(typeof(StackTrace));
            Type traceFormatType = typeof(StackTrace).GetNestedType("TraceFormat", BindingFlags.NonPublic);

            MethodInfo toString = typeof(StackTrace).GetMethod("ToString",
                BindingFlags.NonPublic | BindingFlags.Instance, null, [traceFormatType], null);

            object normalTraceFormat = Enum.GetValues(traceFormatType).GetValue(0);

            MethodCallExpression stackTraceString =
                Expression.Call(stack, toString, Expression.Constant(normalTraceFormat, traceFormatType));

            FieldInfo stackTraceStringField =
                typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);

            BinaryExpression assign =
                Expression.Assign(Expression.Field(target, stackTraceStringField), stackTraceString);

            return Expression
                .Lambda<Func<Exception, StackTrace, Exception>>(Expression.Block(assign, target), target, stack)
                .Compile();
        })();

    /// <summary>
    ///     Sets the stack trace of provided exception object
    /// </summary>
    /// <param name="target">The exception to change stack trace.</param>
    /// <param name="stack">The stack trace.</param>
    /// <returns></returns>
    public static Exception SetStackTrace(this Exception target, StackTrace stack) => SetStackTraceFunc(target, stack);

    public static void HandleException(this Exception exception)
    {
        FExExtensionsCommon.ExceptionHandler.Handle(exception);
    }

    public static void HandleException(this Exception exception, IExceptionHandlerOptions options)
    {
        FExExtensionsCommon.ExceptionHandler.Handle(exception, options);
    }

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

    /// <summary>
    ///     Gets a formatted string from the exception.
    /// </summary>
    /// <param name="ex">The exception to build.</param>
    /// <returns>A String.</returns>
    public static string BuildMessage(this Exception ex)
    {
        var message = new StringBuilder();
        BuildMessage(ex, ref message);

        return message.ToString();
    }

    /// <summary>
    ///     Appends exception details into a StringBuilder
    /// </summary>
    /// <param name="ex">The exception to build.</param>
    /// <param name="message">The StringBuilder to receive the exception detail messages.</param>
    private static void BuildMessage(this Exception ex, ref StringBuilder message)
    {
        message.Append(ex.GetType().Name).Append(": ").AppendLine(ex.Message);

        if (!string.IsNullOrWhiteSpace(ex.Source))
            message.Append("Source: ").AppendLine(ex.Source);

        //Stack trace is expensive to create. Do it only once.
        string stackTrace = ex.StackTrace;

        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            message.AppendLine("StackTrace:");
            message.AppendLine(stackTrace);
        }

        // Go into the inner exceptions recursively
        if (ex.InnerException is not null)
        {
            message.AppendLine("Inner Exception:");
            BuildMessage(ex.InnerException, ref message);
        }
    }
}