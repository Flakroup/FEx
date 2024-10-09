using FEx.Abstractions.Enums;
using FEx.Abstractions.Interfaces;
using FEx.Basics.Extensions;
using FEx.Extensions.Helpers;
using FEx.Legacy.Asyncx.Abstractions.Interfaces;
using FEx.Legacy.Asyncx.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Legacy.Asyncx;

public class TasksHandler : StaticAsyncHelper, ITasksHandler
{
    private readonly IAsyncHelper _asyncHelper;
    private readonly ITasksInfoSubject _tasksInfoSubject;

    public TasksHandler(IAsyncHelper asyncHelper, ITasksInfoSubject tasksInfoSubject)
    {
        _asyncHelper = asyncHelper;
        _tasksInfoSubject = tasksInfoSubject;
    }

    public async Task RunAsync(Action task,
                               JobSpecs? specs = null,
                               Action<JobSpecs?> pre = null,
                               Action<bool, JobSpecs?> post = null,
                               AsyncMode asyncMode = AsyncMode.ThreadPool,
                               CancellationToken cancellationToken = default) =>
        await RunFuncAsync(() => Wrap(task), specs, pre, post, asyncMode, cancellationToken);

    public async Task<T> RunTaskAsync<T>(Func<Task<T>> task,
                                         JobSpecs? specs = null,
                                         Action<JobSpecs?> pre = null,
                                         Action<bool, JobSpecs?> post = null,
                                         AsyncMode asyncMode = AsyncMode.ThreadPool)
    {
        var taskId = Guid.NewGuid();
        _tasksInfoSubject.AddTask(taskId);
        T result = default;
        var isSuccess = true;

        try
        {
            pre?.Invoke(specs);

            result = asyncMode switch
            {
                AsyncMode.MainThread => await _asyncHelper.ExecuteDeferredTaskOnMainThreadAsync(task),
                AsyncMode.ThreadPool => await ExecuteTaskOnThreadPoolAsync(task),
                _ => await task()
            };
        }
        catch (Exception ex)
        {
            isSuccess = false;
            ex.HandleException(specs.HasFlagFast(JobSpecs.InformUserOnExceptionInMain));
        }
        finally
        {
            post?.Invoke(isSuccess, specs);
            _tasksInfoSubject.RemoveTask(taskId);
        }

        return result;
    }

    public async Task<T> RunFuncAsync<T>(Func<T> task,
                                         JobSpecs? specs = null,
                                         Action<JobSpecs?> pre = null,
                                         Action<bool, JobSpecs?> post = null,
                                         AsyncMode asyncMode = AsyncMode.ThreadPool,
                                         CancellationToken cancellationToken = default)
    {
        var taskId = Guid.NewGuid();
        _tasksInfoSubject.AddTask(taskId);
        T result = default;
        var isSuccess = true;

        try
        {
            pre?.Invoke(specs);

            result = asyncMode switch
            {
                AsyncMode.MainThread => await _asyncHelper.ExecuteDeferredTaskOnMainThreadAsync(task),
                AsyncMode.ThreadPool => await ExecuteOnThreadPoolAsync(task, cancellationToken: cancellationToken),
                _ => await Task.Run(task, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            isSuccess = false;
            ex.HandleException(specs.HasFlagFast(JobSpecs.InformUserOnExceptionInMain));
        }
        finally
        {
            post?.Invoke(isSuccess, specs);
            _tasksInfoSubject.RemoveTask(taskId);
        }

        return result;
    }

    public async Task RunTaskAsync(Func<Task> task,
                                   JobSpecs? specs = null,
                                   Action<JobSpecs?> pre = null,
                                   Action<bool, JobSpecs?> post = null,
                                   AsyncMode asyncMode = AsyncMode.ThreadPool) =>
        await RunTaskAsync(() => WrapTaskAsync(task), specs, pre, post, asyncMode);
}