namespace FEx.Agnostics.Abstractions.Interfaces.Flow;

/// <summary>An error that records the stack trace of where it occurred.</summary>
public interface IStackError : IError
{
    /// <summary>Gets the stack trace of this error.</summary>
    string? StackTrace { get; }
    /// <summary>Gets the stack trace of the root error in the inner error chain, or of this error when there is none.</summary>
    string? RootErrorStackTrace { get; }
}