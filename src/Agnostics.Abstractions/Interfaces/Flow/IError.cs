namespace FEx.Abstractions.Interfaces;

public interface IError
{
    string Message { get; }
    IError RootError { get; }
    IError InnerError { get; }

    void SetInnerError(IError innerError);
}