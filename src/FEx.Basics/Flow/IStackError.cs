namespace FEx.Basics.Flow;

public interface IStackError
{
    string StackTrace { get; }
    string RootErrorStackTrace { get; }
}