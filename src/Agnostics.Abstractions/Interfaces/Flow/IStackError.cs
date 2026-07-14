namespace FEx.Agnostics.Abstractions.Interfaces.Flow;

public interface IStackError : IError
{
    string? StackTrace { get; }
    string? RootErrorStackTrace { get; }
}