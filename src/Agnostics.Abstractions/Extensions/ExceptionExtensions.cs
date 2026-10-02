using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class ExceptionExtensions
{
    // Private runtime field; its name is not guaranteed on every runtime. When it is missing the stack trace
    // cannot be replaced, so SetStackTrace degrades to leaving the exception untouched instead of throwing from
    // the type initializer (which would break every member of this class).
    private static readonly FieldInfo? _stackTraceStringField =
        typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Sets the stack trace of provided exception object
    /// </summary>
    /// <param name="target">The exception to change stack trace.</param>
    /// <param name="stack">The stack trace.</param>
    /// <returns></returns>
    public static Exception SetStackTrace(this Exception target, StackTrace stack)
    {
        stack.Guard(nameof(stack));

        // The public ToString() renders the same text as the runtime's own "normal" trace format.
        _stackTraceStringField?.SetValue(target, stack.ToString());

        return target;
    }

    /// <summary>
    /// Gets a formatted string from the exception.
    /// </summary>
    /// <param name="ex">The exception to build.</param>
    /// <returns>A String.</returns>
    public static string BuildMessage(this Exception ex)
    {
        var message = new StringBuilder();
        ex.BuildMessage(ref message);

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
            inner = inner.InnerException!;
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
            ex.InnerException.BuildMessage(ref message);
        }
    }
}