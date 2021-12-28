namespace FEx.Utilities.Flow;

public class Result<TError>
    where TError : IError, new()
{
    public static implicit operator Result<TError>(TError right)
    {
        return new Result<TError>(right);
    }

    public static implicit operator Result<TError>(string message)
    {
        var error = Activator.CreateInstance<TError>();
        error.Message = message;
        return new Result<TError>(error);
    }

    public static Result<TError> Success => new();
    public static Result<TError> Failure => new(new TError());

    public Result()
    {
        IsSuccessful = true;
    }

    public Result(TError error)
    {
        Error = error;
        IsSuccessful = false;
    }

    public TError Error { get; }
    public bool IsSuccessful { get; }
}

public class Result<TData, TError> : Result<TError>
    where TError : Error, new()
{
    public static implicit operator Result<TData, TError>(TData data)
    {
        return new Result<TData, TError>(data);
    }

    public static implicit operator Result<TData, TError>(TError error)
    {
        return new Result<TData, TError>(error);
    }

    public new static Result<TData, TError> Failure => new(new TError());

    public Result(TData data)
    {
        Data = data;
    }

    public Result(TError error) : base(error)
    {
    }

    public TData Data { get; }
}