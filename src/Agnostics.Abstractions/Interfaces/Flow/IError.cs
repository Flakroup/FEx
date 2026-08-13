namespace FEx.Agnostics.Abstractions.Interfaces.Flow;

public interface IError
{
    string? Message { get; }
    IError? RootError { get; }
    IError? InnerError { get; }

    void SetInnerError(IError innerError);
}