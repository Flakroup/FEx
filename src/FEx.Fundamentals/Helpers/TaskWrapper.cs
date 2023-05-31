using FEx.Basics.Flow;
using System;
using System.Threading.Tasks;

namespace FEx.Fundamentals.Helpers;

public class TaskWrapper : ITaskWrapper
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

    public bool IsFinished { get; private set; }

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

    public void SetResult(object result)
    {
        Result = Result<ExceptionError>.Success;
    }

    public void SetException(Exception exception)
    {
        Result = new ExceptionError(exception);
    }

    public void SetTask(Func<Task> task)
    {
        Task = ExecuteTaskAsync(task);
    }

    private async Task ExecuteTaskAsync(Func<Task> task)
    {
        await task();

        if (Result.IsFailure)
            throw Result.Error.Exception;
    }
}

public class TaskWrapper<T> : ITaskWrapper
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

    public bool IsFinished { get; private set; }

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

    public void SetResult(object result)
    {
        Result = new((T)result);
    }

    public void SetException(Exception exception)
    {
        Result = new ExceptionError(exception);
    }

    public void SetTask(Func<Task<T>> task)
    {
        Task = ExecuteTaskAsync(task);
    }

    private async Task<T> ExecuteTaskAsync(Func<Task<T>> task)
    {
        await task();

        if (Result.IsFailure)
            throw Result.Error.Exception;

        return Result.Data;
    }
}

public interface ITaskWrapper
{
    void SetResult(object result);
    void SetException(Exception exception);
}