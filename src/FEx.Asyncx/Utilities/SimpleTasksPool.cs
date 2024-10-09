using FEx.Abstractions.Enums;
using FEx.Abstractions.Interfaces;
using FEx.Basics.Collections.Concurrent;
using FEx.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.Asyncx.Utilities;

public class SimpleTasksPool
{
    private readonly IAsyncHelper _asyncHelper;

    protected ConcurrentList<Task> Tasks { get; }

    public SimpleTasksPool(IAsyncHelper asyncHelper)
    {
        _asyncHelper = asyncHelper;

        Tasks = [];
    }

    public async Task AddTaskAsync(Func<Task> task) =>
        await InternalAddTaskAsync(() => _asyncHelper.FireTaskAndForget(task, AsyncMode.ThreadPool).Task);

    public async Task AddActionTaskAsync(Action task) =>
        await InternalAddTaskAsync(() => _asyncHelper.FireAndForget(task, AsyncMode.ThreadPool).Task);

    public async Task<T> AddTaskAsync<T>(Func<Task<T>> task) =>
        await InternalAddTaskAsync(() => _asyncHelper.FireTaskAndForget(task, AsyncMode.ThreadPool).Task);

    public async Task<T> AddTaskAsync<T>(Func<T> task) =>
        await InternalAddTaskAsync(() => _asyncHelper.FireAndForget(task, AsyncMode.ThreadPool).Task);

    public void RemoveTask(Task task) => Tasks.Remove(task);

    public bool AnyTaskIsRunning() =>
        Tasks.Write(() => Tasks.Count > 0 && Tasks.Any(task => task.IsRunning()));

    public async Task WhenAllAsync() => await Tasks.Where(t => t.IsRunning()).WhenAllAsync();

    public async Task AddTaskAsync(Action task) =>
        await InternalAddTaskAsync(() => _asyncHelper.FireAndForget(task, AsyncMode.ThreadPool).Task);

    protected async Task<T> InternalAddTaskAsync<T>(Func<Task<T>> taskFunc)
    {
        Task<T> task = taskFunc();
        Tasks.Add(task);

        return await task;
    }

    protected async Task InternalAddTaskAsync(Func<Task> taskFunc)
    {
        Task task = taskFunc();
        Tasks.Add(task);
        await task;
    }
}