using FEx.Extensions.Base;
using FEx.Extensions.Base.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

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
                BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { traceFormatType }, null);
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

    public static void HandleException(this Exception exception, object options)
    {
        FExExtensionsCommon.ExceptionHandler.Handle(exception, options);
    }

    public static void HandleException(this Exception ex,
                                       bool? informUser = null,
                                       bool wait = false,
                                       bool doNotReport = false,
                                       params (string, object)[] custom)
    {
        if (ex is TaskCanceledException)
            doNotReport = true;

        ExceptionHandlerOptions options = informUser != null || wait || doNotReport || custom?.Any() == true
            ? new ExceptionHandlerOptions
            {
                InformUser = informUser,
                Wait = wait,
                DoNotReport = doNotReport,
                Custom = custom
            }
            : null;
        ex.HandleException(options);
    }
}