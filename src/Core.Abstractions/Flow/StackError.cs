using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces.Flow;
using FEx.Core.Abstractions.Interfaces;
using System.Diagnostics;
using System.Text;

namespace FEx.Core.Abstractions.Flow;

public class StackError : Error, IStackError
{
    public string StackTrace { get; }

    public string? RootErrorStackTrace =>
        FindInnerStackError(InnerError) is { } innerStackError
            ? innerStackError.StackTrace
            : StackTrace;

    // Returns the nearest IStackError in the inner-error chain (mirrors IError.TryGetError search).
    internal static IStackError? FindInnerStackError(IError? error) =>
        error switch
        {
            null => null,
            IStackError stackError => stackError,
            _ => FindInnerStackError(error.InnerError)
        };

    public StackTrace OriginalStackTrace { get; }
#pragma warning disable CS0618 // Type or member is obsolete
    private static IStackTraceProvider StackTraceProvider => FExCoreStatics.StackTraceProvider;
#pragma warning restore CS0618 // Type or member is obsolete

    public StackError()
    {
        OriginalStackTrace = StackTraceProvider.GetStackTrace();
        StackTrace = OriginalStackTrace.ToString();
    }

    public StackError(string message)
        : base(message)
    {
        OriginalStackTrace = StackTraceProvider.GetStackTrace();
        StackTrace = OriginalStackTrace.ToString();
    }

    public StackError(IError innerError, string? message = null)
        : base(innerError, message)
    {
        OriginalStackTrace = StackTraceProvider.GetStackTrace();
        StackTrace = OriginalStackTrace.ToString();
    }

    public override string ToString() =>
        !string.IsNullOrEmpty(Message)
            ? new StringBuilder(Message).AppendLine(StackTrace).ToString()
            : StackTrace;
}

public class StackError<TErrorStatus> : Error<TErrorStatus>, IStackError
{
    public string StackTrace { get; }

    public string? RootErrorStackTrace =>
        StackError.FindInnerStackError(InnerError) is { } innerStackError
            ? innerStackError.StackTrace
            : StackTrace;

    public StackTrace OriginalStackTrace { get; }
#pragma warning disable CS0618 // Type or member is obsolete
    private static IStackTraceProvider StackTraceProvider => FExCoreStatics.StackTraceProvider;
#pragma warning restore CS0618 // Type or member is obsolete

    public StackError(TErrorStatus status, string? message = null)
        : base(status, message)
    {
        OriginalStackTrace = StackTraceProvider.GetStackTrace();
        StackTrace = OriginalStackTrace.ToString();
    }

    public StackError(TErrorStatus status, IError innerError, string? message = null)
        : base(status, innerError, message)
    {
        OriginalStackTrace = StackTraceProvider.GetStackTrace();
        StackTrace = OriginalStackTrace.ToString();
    }

    public override string ToString() =>
        !string.IsNullOrEmpty(Message)
            ? new StringBuilder(Message).AppendLine(StackTrace).ToString()
            : StackTrace;
}