using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.Threading.Tasks;

namespace FEx.Core.Abstractions.Utilities;

public class TaskWrapper : TaskWrapperBase<Task, Result<ExceptionError>>, ITaskWrapper
{
    public TaskWrapper(Func<Task>? task = null, bool setStackTrace = true)
        : base(task, setStackTrace)
    {
    }

    public void SetResult() => Result = Result<ExceptionError>.Success;

    protected override Result<ExceptionError> ConvertExceptionToError(Exception exception) =>
        new ExceptionError(exception);

    protected override async Task ExecuteTaskAsync(Func<Task> task)
    {
        if (!IsFinished)
            await task();

        // IsFailure guarantees a non-null Error; its Exception is set by ConvertExceptionToError.
        if (Result.IsFailure)
            throw Result.Error!.Exception!;
    }
}

public class TaskWrapper<T> : TaskWrapperBase<Task<T>, Result<T, ExceptionError>>, ITaskWrapper<T>
{
    public TaskWrapper(Func<Task<T>>? task = null, bool setStackTrace = true)
        : base(task, setStackTrace)
    {
    }

    public void SetResult(T result) => Result = result;

    protected override Result<T, ExceptionError> ConvertExceptionToError(Exception exception) =>
        new ExceptionError(exception);

    protected override async Task<T> ExecuteTaskAsync(Func<Task<T>> task)
    {
        if (!IsFinished)
            await task();

        // IsFailure guarantees a non-null Error; its Exception is set by ConvertExceptionToError.
        if (Result.IsFailure)
            throw Result.Error!.Exception!;

        return Result.Data;
    }
}