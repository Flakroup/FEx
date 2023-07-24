namespace FEx.Basics.Flow;

public interface IStackError : IError
{
    string StackTraceString { get; }
    string RootErrorStackTraceString { get; }
}