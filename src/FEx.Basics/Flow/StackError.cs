using System;

namespace FEx.Basics.Flow
{
    public class StackError : Error, IStackError
    {
        public string StackTrace { get; }

        public string RootErrorStackTrace => (RootError as IStackError)?.StackTrace;

        public StackError()
        {
            StackTrace = FExBasics.StackTraceProvider.GetStackTrace().ToString();
        }

        public StackError(string message)
            : base(message)
        {
            StackTrace = FExBasics.StackTraceProvider.GetStackTrace().ToString();
        }

        public StackError(IError innerError, string message = null)
            : base(innerError, message)
        {
            StackTrace = FExBasics.StackTraceProvider.GetStackTrace().ToString();
        }

        public override string ToString() =>
            !string.IsNullOrEmpty(Message)
                ? Message + Environment.NewLine + StackTrace
                : StackTrace;
    }

    public class StackError<TErrorStatus> : Error<TErrorStatus>
    {
        public string StackTrace { get; }

        public string RootErrorStackTrace => (RootError as IStackError)?.StackTrace;

        public StackError(TErrorStatus status, string message = null)
            : base(status, message)
        {
            StackTrace = FExBasics.StackTraceProvider.GetStackTrace().ToString();
        }

        public StackError(TErrorStatus status, IError innerError, string message = null)
            : base(status, innerError, message)
        {
            StackTrace = FExBasics.StackTraceProvider.GetStackTrace().ToString();
        }

        public override string ToString() =>
            !string.IsNullOrEmpty(Message)
                ? Message + Environment.NewLine + StackTrace
                : StackTrace;
    }
}