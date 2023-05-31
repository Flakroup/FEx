namespace FEx.Basics.Flow;

public interface IError
{
    string Message { get; }
    IError RootError { get; }
    IError InnerError { get; }
}