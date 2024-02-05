using FEx.Asyncx.Utilities;
using FEx.Basics.Abstractions;
using FEx.Extensions;
using FEx.Extensions.Collections.Dictionaries;
using FEx.Extensions.Collections.Lists;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Helpers;

public class AsyncQueue<TKey, TValue> : PropertyChangeAware
{
    protected readonly EventHandler<int> _limitChanged;
    private readonly ConcurrentDictionary<TKey, AsyncJob<TKey, TValue>> _dictionary;
    private readonly ConcurrentDictionary<TKey, Task<TValue>> _jobTasks;
    private readonly SemaphoreSlim _semaphore;
    private readonly SemaphoreSlim _locker;
    private readonly Action<Exception> _onException;
    private ILogger<AsyncQueue<TKey, TValue>> _logger;
    private int _limit;
    private int _counter;

    public bool AutoRemoveFinishedTasks { get; }

    public int Limit
    {
        get => _limit;
        set
        {
            if (value < 1)
                throw new Exception("Limit cannot be less than 1");

            SetProperty(ref _limit, value, l => _limitChanged?.Invoke(this, l));
        }
    }

    public int Count => _dictionary.Count;

    public AsyncQueue(Action<Exception> onException, int limit, bool autoRemoveFinishedTasks = true)
    {
        _counter = 0;
        Limit = limit;
        _onException = onException;
        _dictionary = new ConcurrentDictionary<TKey, AsyncJob<TKey, TValue>>();
        _jobTasks = new ConcurrentDictionary<TKey, Task<TValue>>();
        _semaphore = new SemaphoreSlim(1, 1);
        _locker = new SemaphoreSlim(1, 1);
        AutoRemoveFinishedTasks = autoRemoveFinishedTasks;
        _limitChanged += OnLimitChanged;
    }

    public void SetLogger(ILogger<AsyncQueue<TKey, TValue>> logger)
    {
        _logger = logger;

        foreach (TKey key in _dictionary.Keys.ToList())
            _dictionary.TryGetKeyValue(key)?.SetLogger(_logger);
    }

    public async Task<TValue> GetOrAddAsync(TKey key, Func<Task<TValue>> valueFactory)
    {
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
        return await GetOrAddAsync(key, new AsyncLazy<TValue>(valueFactory));
#pragma warning restore VSTHRD012 // Provide JoinableTaskFactory where allowed
    }

    public async Task<TValue> GetOrAddAsync(TKey key, AsyncLazy<TValue> coldTask)
    {
        AsyncJob<TKey, TValue> task = await GetOrAddTaskAsync(key, coldTask);

        return await _jobTasks.AddOrUpdateValue(task.TaskId, task.GetResultAsync);
    }

    public async Task RemoveFinishedTasksAsync()
    {
        await _locker.WaitAsync();

        try
        {
            while (_dictionary.Any(x => x.Value.IsFinished))
                RemoveTasksById(_dictionary.Where(x => x.Value.IsFinished).Select(x => x.Key).ToArray());
        }
        finally
        {
            _locker.Release();
        }
    }

    public AsyncJob<TKey, TValue> GetTaskByKey(TKey key) => _dictionary.TryGetKeyValue(key);

    public async Task WaitForAllRunningJobsAsync()
    {
        if (_dictionary.Any(x => !x.Value.IsFinished))
            while (_dictionary.Any(x => !x.Value.IsFinished))
            {
                Task<TValue>[] jobs = _dictionary.Values.Where(x => x.IsRunning)
                    .OrderBy(x => x.Index)
                    .Select(x => _jobTasks.TryGetKeyValue(x.TaskId))
                    .Where(x => x is not null)
                    .ToArray();

                if (jobs.IsNotNullOrEmptyList())
                {
                    await Task.WhenAll(jobs);
                }
                else
                {
                    await TryStartNextTaskAsync();
                    await Task.Delay(250);
                }
            }
    }

    protected async Task<bool> TryStartNextTaskAsync()
    {
        var hasStartedAny = false;

        if (_dictionary.Values.Count(x => x.IsStarted) < Limit
            && await _semaphore.WaitAsync(TimeSpan.Zero))
        {
            await _locker.WaitAsync();

            try
            {
                while (_dictionary.Values.Count(x => x.IsStarted) < Limit)
                {
                    AsyncJob<TKey, TValue> task = GetNextTask();

                    if (task is not null)
                    {
                        task.StartTask();
                        hasStartedAny = true;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                _locker.Release();
                _semaphore.Release();
            }
        }

        return hasStartedAny;
    }

    private async Task<AsyncJob<TKey, TValue>> GetOrAddTaskAsync(TKey key, AsyncLazy<TValue> coldTask)
    {
        if (!_dictionary.TryGetValue(key, out AsyncJob<TKey, TValue> task)
            || task.IsFinished)
        {
            task = _dictionary.AddOrUpdateValue(key,
                () => new AsyncJob<TKey, TValue>(key, coldTask, GetTaskNr(), _onException, _logger));

            task.HasFinished += OnJobHasFinished;
        }

        await TryStartNextTaskAsync();

        return task;
    }

    private AsyncJob<TKey, TValue> GetNextTask()
    {
        return _dictionary.Values.Count(x => x.IsStarted) >= Limit
            ? null
            : _dictionary.Values.Where(x => !x.IsStarted).OrderBy(x => x.Index).FirstOrDefault();
    }

    [SuppressMessage("Usage",
        "VSTHRD100:Avoid async void methods",
        Justification = "Method is an event callback with try/catch inside")]
    private async void OnJobHasFinished(object sender, TaskStatus? e)
    {
        await TryStartNextTaskEventCallbackAsync((AsyncJob<TKey, TValue>)sender);
    }

    private async Task TryStartNextTaskEventCallbackAsync(AsyncJob<TKey, TValue> task)
    {
        try
        {
            if (AutoRemoveFinishedTasks)
                TryRemoveFinishedTask(task);

            await TryStartNextTaskAsync();
        }
        catch (Exception ex)
        {
            ex.HandleException();
        }
    }

    private void TryRemoveFinishedTask(AsyncJob<TKey, TValue> task)
    {
        if (task?.IsFinished == true)
            RemoveTasksById(task.TaskId);
    }

    private void RemoveTasksById(params TKey[] keys)
    {
        foreach (TKey key in keys)
        {
            (bool _, AsyncJob<TKey, TValue> removedValue) = _dictionary.RemoveValue(key);

            if (removedValue is not null)
            {
                Log($"Task {removedValue.TaskId} has been removed");
                removedValue.HasFinished -= OnJobHasFinished;
                _jobTasks.RemoveValue(key);
            }
        }
    }

    private int GetTaskNr() => Interlocked.Increment(ref _counter);

    [SuppressMessage("Usage",
        "VSTHRD100:Avoid async void methods",
        Justification = "Method is an event callback with try/catch inside")]
    private async void OnLimitChanged(object sender, int e)
    {
        await TryStartNextTaskEventCallbackAsync(null);
    }

    private void Log(string message)
    {
        _logger?.LogDebug(message);
    }
}