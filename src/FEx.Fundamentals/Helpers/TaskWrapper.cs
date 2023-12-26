using FEx.Basics.Flow;
using System;
using System.Threading.Tasks;

namespace FEx.Fundamentals.Helpers;

public class TaskWrapper : TaskWrapperBase
{
    private Task _task;
    private Result<ExceptionError> _result;

    public Result<ExceptionError> Result
    {
        get => _result;
        private set
        {
            _result = value;
            IsFinished = true;
        }
    }

    public Task Task
    {
        get => _task;
        private set
        {
            if (_task is not null)
                throw new InvalidOperationException($"{nameof(Task)} is already set");

            _task = value;
        }
    }

    public override void SetException(Exception exception) => Result = new ExceptionError(exception);

    public void SetResult() => Result = Result<ExceptionError>.Success;

    public void SetTask(Func<Task> task) => Task = ExecuteTaskAsync(task);

    private async Task ExecuteTaskAsync(Func<Task> task)
    {
        await task();

        if (Result.IsFailure)
            throw Result.Error.Exception;
    }
}

public class TaskWrapper<T> : TaskWrapperBase
{
    private Task<T> _task;
    private Result<T, ExceptionError> _result;

    public Result<T, ExceptionError> Result
    {
        get => _result;
        private set
        {
            _result = value;
            IsFinished = true;
        }
    }

    public Task<T> Task
    {
        get => _task;
        private set
        {
            if (_task is not null)
                throw new InvalidOperationException($"{nameof(Task)} is already set");

            _task = value;
        }
    }

    public override void SetException(Exception exception) => Result = new ExceptionError(exception);

    public void SetResult(T result) => Result = new Result<T, ExceptionError>(result);

    public void SetTask(Func<Task<T>> task) => Task = ExecuteTaskAsync(task);

    private async Task<T> ExecuteTaskAsync(Func<Task<T>> task)
    {
        await task();

        if (Result.IsFailure)
            throw Result.Error.Exception;

        return Result.Data;
    }
}