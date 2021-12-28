using System.Diagnostics;

namespace FEx.Utilities.Flow;

public interface IError
{
    Error InnerError { get; }
    string Message { get; set; }
    string RootErrorStackTrace { get; }
    string StackTrace { get; }
}

public abstract class Error : IError
{
    protected Error()
    {
        StackTrace = new StackTrace(true).ToString();
    }

    protected Error(Error innerError)
        : this()
    {
        InnerError = innerError ?? throw new ArgumentNullException(nameof(innerError));
        RootErrorStackTrace = InnerError.RootErrorStackTrace ?? InnerError.StackTrace;
    }

    public Error InnerError { get; }
    public string Message { get; set; }
    public string StackTrace { get; }
    public string RootErrorStackTrace { get; }
}

public abstract class Error<TErrorStatus> : Error
    where TErrorStatus : Enum
{
    protected Error(TErrorStatus status)
    {
        Status = status;
    }

    protected Error(TErrorStatus status, Error innerError) : base(innerError)
    {
        Status = status;
    }

    public TErrorStatus Status { get; }
}