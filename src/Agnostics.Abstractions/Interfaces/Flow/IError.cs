namespace FEx.Agnostics.Abstractions.Interfaces.Flow;

/// <summary>Describes a failure and optionally the failure that caused it.</summary>
public interface IError
{
    /// <summary>Gets the error message.</summary>
    string? Message { get; }
    /// <summary>Gets the deepest error in the inner error chain, or null when there is no inner error.</summary>
    IError? RootError { get; }
    /// <summary>Gets the error that caused this one.</summary>
    IError? InnerError { get; }

    /// <summary>Sets the error that caused this one.</summary>
    /// <param name="innerError">The inner error.</param>
    void SetInnerError(IError innerError);
}