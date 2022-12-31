using FEx.Abstractions;
using FEx.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Fundamentals;

public class AsyncHelper
{
    private readonly IFExDispatcher _dispatcher;
    private readonly IExceptionHandler _exceptionHandler;

    public AsyncHelper(IFExDispatcher dispatcher, IExceptionHandler exceptionHandler)
    {
        _dispatcher = dispatcher;
        _exceptionHandler = exceptionHandler;
    }

    public TaskCompletionSource<object> FireAndForget(Action action, CancellationToken cancellationToken, AsyncMode asyncMode = AsyncMode.Default)
    {
        action.Guard(nameof(action));
        return FireTaskAndForget(() => Task.Run(action, cancellationToken), asyncMode);
    }

    public TaskCompletionSource<object> FireTaskAndForget(Func<Task> task, AsyncMode asyncMode = AsyncMode.Default)
    {
        task.Guard(nameof(task));
        var taskCompletionSource = new TaskCompletionSource<object>();
        _ = ExecuteAndCatchAsync(task, taskCompletionSource, asyncMode);
        return taskCompletionSource;
    }

    public IReadOnlyList<TaskCompletionSource<object>> FireTasksAndForget(IEnumerable<Func<Task>> tasks, AsyncMode asyncMode = AsyncMode.Default)
    {
        tasks.Guard(nameof(tasks));
        return tasks.Select(x => FireTaskAndForget(x, asyncMode))
            .ToList();
    }

    public async Task ExecuteOnThreadPoolAsync(Func<Task> task)
    {
        await ExecuteOnThreadPoolAsync<object>(async () =>
        {
            await task();
            return default;
        });
    }

    public async Task<T> ExecuteOnThreadPoolAsync<T>(Func<Task<T>> task)
    {
        return await await new TaskFactory(TaskScheduler.Default).StartNew(task);
    }

    private async Task ExecuteAndCatchAsync(Func<Task> task, TaskCompletionSource<object> taskCompletionSource, AsyncMode asyncMode = AsyncMode.Default)
    {
        try
        {
            switch (asyncMode)
            {
                case AsyncMode.MainThread:
                    await _dispatcher.InvokeOnMainThreadAsync(task);
                    break;
                case AsyncMode.ThreadPool:
                    await ExecuteOnThreadPoolAsync(task);
                    break;
                default:
                    await task();
                    break;
            }

            taskCompletionSource.SetResult(null);
        }
        catch (Exception ex)
        {
            if (_exceptionHandler.CanHandle(ex))
                _exceptionHandler.Handle(ex);

            taskCompletionSource.SetException(ex);
        }
    }
}