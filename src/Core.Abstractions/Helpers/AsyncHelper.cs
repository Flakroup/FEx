using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions.Interfaces;
using FEx.Core.Abstractions.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static FEx.Agnostics.Abstractions.AsyncStatics;

namespace FEx.Core.Abstractions.Helpers;

public class AsyncHelper : IAsyncHelper
{
    //private readonly ITasksInfoSubject _tasksInfoSubject;
    private readonly IFExDispatcher _dispatcher;
    private readonly IExceptionHandler _exceptionHandler;

    public AsyncHelper(IFExDispatcher dispatcher,
                       //ITasksInfoSubject tasksInfoSubject,
                       IExceptionHandler exceptionHandler)
    {
        _dispatcher = dispatcher;
        //_tasksInfoSubject = tasksInfoSubject;
        _exceptionHandler = exceptionHandler;
    }

    public async Task ExecuteDeferredTaskOnMainThreadAsync(Action func,
                                                           AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await ExecuteTaskOnThreadPoolAsync(() => _dispatcher.InvokeOnMainThreadAsync(func), options);

    public async Task<T> ExecuteDeferredTaskOnMainThreadAsync<T>(Func<T> func,
                                                                 AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await ExecuteTaskOnThreadPoolAsync(() => _dispatcher.InvokeOnMainThreadAsync(func), options);

    public async Task ExecuteDeferredTaskOnMainThreadAsync(Func<Task> func,
                                                           AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await ExecuteTaskOnThreadPoolAsync(() => _dispatcher.InvokeOnMainThreadAsync(func), options);

    public async Task<T> ExecuteDeferredTaskOnMainThreadAsync<T>(Func<Task<T>> func,
                                                                 AsyncOptions options = AsyncOptions.ImmediateStart) =>
        await ExecuteTaskOnThreadPoolAsync(() => _dispatcher.InvokeOnMainThreadAsync(func), options);

    public ITaskWrapper FireAndForget(Action action,
                                      AsyncMode asyncMode = AsyncMode.Default,
                                      IExceptionHandlerOptions options = null,
                                      CancellationToken cancellationToken = default)
    {
        action.Guard(nameof(action));
        var taskWrapper = new TaskWrapper();

        taskWrapper.SetTask(() =>
            ExecuteAndCatchAsync(action.Wrap, taskWrapper, asyncMode, options, cancellationToken));

        return taskWrapper;
    }

    public ITaskWrapper<T> FireAndForget<T>(Func<T> func,
                                            AsyncMode asyncMode = AsyncMode.Default,
                                            IExceptionHandlerOptions options = null,
                                            CancellationToken cancellationToken = default)
    {
        func.Guard(nameof(func));
        var taskWrapper = new TaskWrapper<T>();
        taskWrapper.SetTask(() => ExecuteAndCatchAsync(func, taskWrapper, asyncMode, options, cancellationToken));

        return taskWrapper;
    }

    public ITaskWrapper FireTaskAndForget(Func<Task> task,
                                          AsyncMode asyncMode = AsyncMode.Default,
                                          IExceptionHandlerOptions options = null)
    {
        task.Guard(nameof(task));
        var taskWrapper = new TaskWrapper();
        taskWrapper.SetTask(() => ExecuteTaskAndCatchAsync(task.WrapTaskAsync, taskWrapper, asyncMode, options));

        return taskWrapper;
    }

    public ITaskWrapper<T> FireTaskAndForget<T>(Func<Task<T>> task,
                                                AsyncMode asyncMode = AsyncMode.Default,
                                                IExceptionHandlerOptions options = null)
    {
        task.Guard(nameof(task));
        var taskWrapper = new TaskWrapper<T>();
        taskWrapper.SetTask(() => ExecuteTaskAndCatchAsync(task, taskWrapper, asyncMode, options));

        return taskWrapper;
    }

    public IReadOnlyList<ITaskWrapper> FireTasksAndForget(IEnumerable<Func<Task>> tasks,
                                                          AsyncMode asyncMode = AsyncMode.Default,
                                                          IExceptionHandlerOptions options = null)
    {
        var deferredList = (tasks?.ToList()).Guard(nameof(tasks));

        return deferredList.Select(x => FireTaskAndForget(x, asyncMode, options)).ToList().AsReadOnly();
    }

    public IReadOnlyList<ITaskWrapper<T>> FireTasksAndForget<T>(IEnumerable<Func<Task<T>>> tasks,
                                                                AsyncMode asyncMode = AsyncMode.Default,
                                                                IExceptionHandlerOptions options = null)
    {
        var deferredList = (tasks?.ToList()).Guard(nameof(tasks));

        return deferredList.Select(x => FireTaskAndForget(x, asyncMode, options)).ToList().AsReadOnly();
    }

    private static void SetResult<T>(T result, ITaskWrapperBase taskWrapper)
    {
        switch (taskWrapper)
        {
            case TaskWrapper<T> typedWrapper:
                typedWrapper.SetResult(result);

                break;
            case TaskWrapper wrapper:
                wrapper.SetResult();

                break;
            default:
                throw new InvalidOperationException($"{taskWrapper.GetType().FullName} is not handled");
        }
    }

    private async Task<T> ExecuteAndCatchAsync<T>(Func<T> func,
                                                  ITaskWrapperBase taskWrapper,
                                                  AsyncMode asyncMode = AsyncMode.Default,
                                                  IExceptionHandlerOptions options = null,
                                                  CancellationToken cancellationToken = default)
    {
        //_tasksInfoSubject.AddTask(taskWrapper);
        T result = default;

        try
        {
            result = asyncMode switch
            {
                AsyncMode.MainThread => await ExecuteDeferredTaskOnMainThreadAsync(func),
                AsyncMode.ThreadPool => await ExecuteOnThreadPoolAsync(func, cancellationToken: cancellationToken),
                _ => await Task.Run(func, cancellationToken)
            };

            SetResult(result, taskWrapper);
        }
        catch (Exception ex)
        {
            _exceptionHandler.Handle(ex, options);
            taskWrapper.SetException(ex);
        }

        //_tasksInfoSubject.RemoveTask(taskWrapper);

        return result;
    }

    private async Task<T> ExecuteTaskAndCatchAsync<T>(Func<Task<T>> task,
                                                      ITaskWrapperBase taskWrapper,
                                                      AsyncMode asyncMode = AsyncMode.Default,
                                                      IExceptionHandlerOptions options = null)
    {
        //_tasksInfoSubject.AddTask(taskWrapper);
        T result = default;

        try
        {
            result = asyncMode switch
            {
                AsyncMode.MainThread => await ExecuteDeferredTaskOnMainThreadAsync(task),
                AsyncMode.ThreadPool => await ExecuteTaskOnThreadPoolAsync(task),
                _ => await task()
            };

            SetResult(result, taskWrapper);
        }
        catch (Exception ex)
        {
            _exceptionHandler.Handle(ex, options);
            taskWrapper.SetException(ex);
        }

        //_tasksInfoSubject.RemoveTask(taskWrapper);

        return result;
    }
}