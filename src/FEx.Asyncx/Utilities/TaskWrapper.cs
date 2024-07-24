using FEx.Abstractions.Flow;
using FEx.Abstractions.Flow.Errors;
using FEx.Abstractions.Interfaces;
using System;
using System.Threading.Tasks;

namespace FEx.Asyncx.Utilities;

public class TaskWrapper : TaskWrapperBase<Task, Result<ExceptionError>>, ITaskWrapper
{
    public TaskWrapper(bool setStackTrace = false)
        : base(setStackTrace)
    {
    }

    public void SetResult() => Result = Result<ExceptionError>.Success;

    protected override Result<ExceptionError> ConvertExceptionToError(Exception exception) =>
        new ExceptionError(exception);

    protected override async Task ExecuteTaskAsync(Func<Task> task)
    {
        if (!IsFinished)
            await task();

        if (Result.IsFailure)
            throw Result.Error.Exception;
    }
}

public class TaskWrapper<T> : TaskWrapperBase<Task<T>, Result<T, ExceptionError>>, ITaskWrapper<T>
{
    public TaskWrapper(bool setStackTrace = false)
        : base(setStackTrace)
    {
    }

    public void SetResult(T result) => Result = result;

    protected override Result<T, ExceptionError> ConvertExceptionToError(Exception exception) =>
        new ExceptionError(exception);

    protected override async Task<T> ExecuteTaskAsync(Func<Task<T>> task)
    {
        if (!IsFinished)
            await task();

        if (Result.IsFailure)
            throw Result.Error.Exception;

        return Result.Data;
    }
}