using FEx.Asyncx.Helpers;
using FEx.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using System;
using System.Threading.Tasks;

namespace FEx.Asyncx.Utilities;

public class AsyncJob<TK, T>
{
    public EventHandler<TaskStatus?> HasFinished;
    private readonly AsyncLazy<T> _coldTask;
    private readonly TaskCompletionSource<bool> _tcs;
    private readonly Action<Exception> _onException;
    private Task<T> _task;
    private ILogger _logger;

    public TK TaskId { get; }
    public bool IsStarted { get; protected set; }
    public bool IsFinished { get; protected set; }
    public int Index { get; }
    public bool IsRunning => _task is not null && !IsFinished;
    public TaskStatus? TaskStatus => _task?.Status;

    public AsyncJob(TK taskId, AsyncLazy<T> coldTask, int no, Action<Exception> onException, ILogger logger = null)
    {
        TaskId = taskId;
        Index = no;
        _onException = onException;
        _logger = logger;
        _tcs = new TaskCompletionSource<bool>();
        _coldTask = coldTask;
    }

    public async Task<T> GetResultAsync()
    {
        try
        {
            await _tcs.Task;
            Log($"Task {TaskId} has started");
            _task = _coldTask.GetValueAsync();

            return await _task;
        }
        catch (Exception ex)
        {
            _onException(ex);

            throw;
        }
        finally
        {
            Log($"Task {TaskId} has finished with {TaskStatus} state");
            IsFinished = _task?.IsFinished() ?? true;
            HasFinished?.Invoke(this, TaskStatus);
        }
    }

    public void StartTask()
    {
        if (!IsStarted)
        {
            IsStarted = true;
            _tcs.SetResult(true);
        }
    }

    public void SetLogger<TKey, TValue>(ILogger<AsyncQueue<TKey, TValue>> logger)
    {
        _logger = logger;
    }

    private void Log(string message)
    {
        _logger?.LogDebug(message);
    }
}