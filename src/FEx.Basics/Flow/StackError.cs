using FEx.Basics.Extensions;
using System.Diagnostics;
using System.Text;

namespace FEx.Basics.Flow;

public class StackError : Error, IStackError
{
    private string _stackTraceString;
    public StackTrace StackTrace { get; }
    public string StackTraceString => _stackTraceString ??= StackTrace.ToString();

    public string RootErrorStackTraceString => InnerError.TryGetError(out IStackError innerStackError)
        ? innerStackError.StackTraceString
        : StackTraceString;

    public StackError()
    {
        StackTrace = GetStackTrace();
    }

    public StackError(string message)
        : base(message)
    {
        StackTrace = GetStackTrace();
    }

    public StackError(IError innerError, string message = null)
        : base(innerError, message)
    {
        StackTrace = GetStackTrace();
    }

    public override string ToString() =>
        !string.IsNullOrEmpty(Message)
            ? new StringBuilder(Message).AppendLine(StackTrace.ToString()).ToString()
            : StackTrace.ToString();

    private static StackTrace GetStackTrace() => FExBasics.StackTraceProvider.GetStackTrace();
}

public class StackError<TErrorStatus> : Error<TErrorStatus>, IStackError
{
    private string _stackTraceString;
    public StackTrace StackTrace { get; }
    public string StackTraceString => _stackTraceString ??= StackTrace.ToString();

    public string RootErrorStackTraceString => InnerError.TryGetError(out IStackError innerStackError)
        ? innerStackError.StackTraceString
        : StackTraceString;

    public StackError(TErrorStatus status, string message = null)
        : base(status, message)
    {
        StackTrace = GetStackTrace();
    }

    public StackError(TErrorStatus status, IError innerError, string message = null)
        : base(status, innerError, message)
    {
        StackTrace = GetStackTrace();
    }

    public override string ToString() =>
        !string.IsNullOrEmpty(Message)
            ? new StringBuilder(Message).AppendLine(StackTrace.ToString()).ToString()
            : StackTrace.ToString();

    private static StackTrace GetStackTrace() => FExBasics.StackTraceProvider.GetStackTrace();
}