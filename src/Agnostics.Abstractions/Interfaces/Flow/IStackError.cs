namespace FEx.Abstractions.Interfaces;

public interface IStackError : IError
{
    string StackTrace { get; }
    string RootErrorStackTrace { get; }
}