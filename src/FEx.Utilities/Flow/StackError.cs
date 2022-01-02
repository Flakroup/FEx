using FEx.Extensions;

namespace FEx.Utilities.Flow;

public class StackError : Error
{
    public static implicit operator StackError(string message)
    {
        return new StackError { Message = message };
    }

    public StackError()
    {
    }

    public StackError(string message = null)
    {
        Message = message;
    }

    public StackError(Error innerError, string message = null)
        : base(innerError)
    {
        Message = message;
    }

    public override string ToString()
    {
        if (Message.IsNotNullOrEmptyString())
        {
            return Message + Environment.NewLine + StackTrace;
        }

        return StackTrace;
    }
}