using FEx.Abstractions.Extensions;
using FEx.Abstractions.Interfaces;
using System.Diagnostics;
using System.Text;

namespace FEx.Abstractions.Flow.Errors;

public class StackError : Error, IStackError
{
    public string StackTrace { get; }

    public string RootErrorStackTrace =>
        InnerError.TryGetError(out IStackError innerStackError)
            ? innerStackError.StackTrace
            : StackTrace;

    public StackTrace OriginalStackTrace { get; }

    public StackError()
    {
        OriginalStackTrace = FExFoundation.StackTraceProvider.GetStackTrace();
        StackTrace = OriginalStackTrace.ToString();
    }

    public StackError(string message)
        : base(message)
    {
        OriginalStackTrace = FExFoundation.StackTraceProvider.GetStackTrace();
        StackTrace = OriginalStackTrace.ToString();
    }

    public StackError(IError innerError, string message = null)
        : base(innerError, message)
    {
        OriginalStackTrace = FExFoundation.StackTraceProvider.GetStackTrace();
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

    public string RootErrorStackTrace =>
        InnerError.TryGetError(out IStackError innerStackError)
            ? innerStackError.StackTrace
            : StackTrace;

    public StackTrace OriginalStackTrace { get; }

    public StackError(TErrorStatus status, string message = null)
        : base(status, message)
    {
        OriginalStackTrace = FExFoundation.StackTraceProvider.GetStackTrace();
        StackTrace = OriginalStackTrace.ToString();
    }

    public StackError(TErrorStatus status, IError innerError, string message = null)
        : base(status, innerError, message)
    {
        OriginalStackTrace = FExFoundation.StackTraceProvider.GetStackTrace();
        StackTrace = OriginalStackTrace.ToString();
    }

    public override string ToString() =>
        !string.IsNullOrEmpty(Message)
            ? new StringBuilder(Message).AppendLine(StackTrace).ToString()
            : StackTrace;
}