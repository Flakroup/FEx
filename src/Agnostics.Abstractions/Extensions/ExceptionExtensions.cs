using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class ExceptionExtensions
{
    private static readonly Func<Exception, StackTrace, Exception> _setStackTraceFunc =
        new Func<Func<Exception, StackTrace, Exception>>(static () =>
        {
            var target = Expression.Parameter(typeof(Exception));
            var stack = Expression.Parameter(typeof(StackTrace));
            var traceFormatType = typeof(StackTrace).GetNestedType("TraceFormat", BindingFlags.NonPublic);

            var toString = typeof(StackTrace).GetMethod("ToString",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [traceFormatType],
                null);

            var normalTraceFormat =
#if NET9_0_OR_GREATER
                Enum.GetValuesAsUnderlyingType(traceFormatType!).GetValue(0);
#else
                Enum.GetValues(traceFormatType!).GetValue(0);
#endif
            var stackTraceString =
                Expression.Call(stack, toString!, Expression.Constant(normalTraceFormat, traceFormatType));

            var stackTraceStringField =
                typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);

            var assign = Expression.Assign(Expression.Field(target, stackTraceStringField!), stackTraceString);

            return Expression
                .Lambda<Func<Exception, StackTrace, Exception>>(Expression.Block(assign, target), target, stack)
                .Compile();
        })();

    /// <summary>
    /// Sets the stack trace of provided exception object
    /// </summary>
    /// <param name="target">The exception to change stack trace.</param>
    /// <param name="stack">The stack trace.</param>
    /// <returns></returns>
    public static Exception SetStackTrace(this Exception target, StackTrace stack)
    {
        stack.Guard(nameof(stack));

        return _setStackTraceFunc(target, stack);
    }

    /// <summary>
    /// Gets a formatted string from the exception.
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
    /// Stacks the specified self.
    /// </summary>
    /// <typeparam name="T">Type of exception</typeparam>
    /// <param name="self">Current instance.</param>
    /// <returns>A string.</returns>
    public static string Stack<T>(this T self) where T : Exception
    {
        Exception inner = self;
        var stackTrace = new StringBuilder();

        while (inner is not null)
        {
            stackTrace.AppendLine(inner.StackTrace);

            break;
        }

        return stackTrace.ToString();
    }

    /// <summary>
    /// Gets the innerexceptions from a exception.
    /// </summary>
    /// <typeparam name="T">Type of exception</typeparam>
    /// <param name="self">Current instance.</param>
    /// <returns>A list of exceptions.</returns>
    public static IEnumerable<Exception> History<T>(this T self) where T : Exception
    {
        var exceptions = new List<Exception>();
        Exception inner = self;

        while (inner is not null)
        {
            exceptions.Add(inner);

            if (inner.InnerException is not null)
                inner = inner.InnerException;
            else
                break;
        }

        return exceptions;
    }

    /// <summary>
    /// Appends exception details into a StringBuilder
    /// </summary>
    /// <param name="ex">The exception to build.</param>
    /// <param name="message">The StringBuilder to receive the exception detail messages.</param>
    private static void BuildMessage(this Exception ex, ref StringBuilder message)
    {
        message.Append(ex.GetType().Name).Append(": ").AppendLine(ex.Message);

        if (!string.IsNullOrWhiteSpace(ex.Source))
            message.Append("Source: ").AppendLine(ex.Source);

        //Stack trace is expensive to create. Do it only once.
        var stackTrace = ex.StackTrace;

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