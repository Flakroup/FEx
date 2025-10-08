using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Collections.Concurrent;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Abstractions.Interfaces;
using FEx.Core.Abstractions.Subjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace FEx.Core.Subjects;

public sealed class TasksInfoSubject : FExBehaviorSubject<IList<Guid>>, ITasksInfoSubject
{
    private readonly ILogger _logger;
    private readonly ConcurrentList<Guid> _tasks;
    private readonly IDisposable _subscription;

    private bool _isDisposed;

    public TasksInfoSubject(ILogger<TasksInfoSubject> logger)
    {
        _logger = logger;
        _tasks = [];

        _subscription = Observable.Interval(TimeSpan.FromSeconds(1))
            .Where(_ => _tasks.Count > 0)
            .AsyncSubscribe(_ => HandleTasks());

        OnNext(_tasks);
    }

    public void AddTask(ITaskWrapperBase value) => AddTask(value.Id);

    /// <inheritdoc />
    public void AddTask(Guid taskId)
    {
        _tasks.Add(taskId);
        OnNext(_tasks);
    }

    public void RemoveTask(ITaskWrapperBase value) => RemoveTask(value.Id);

    /// <inheritdoc />
    public void RemoveTask(Guid taskId)
    {
        _tasks.Remove(taskId);
        OnNext(_tasks);

        if (_tasks.Count == 0)
            HandleTasks();
    }

    private void HandleTasks()
    {
        switch (_tasks.Count)
        {
            case 0:
                _logger.LogInformation("All tasks have finished");

                break;
            case 1:
                _logger.LogInformation($"{_tasks.Count} task running");

                break;
            case > 1:
                _logger.LogInformation($"{_tasks.Count} tasks running");

                break;
        }
    }

    #region IDisposable
    protected override void Dispose(bool isDisposing)
    {
        if (_isDisposed)
            return;

#pragma warning disable IDISP023
        _subscription.Dispose();
#pragma warning restore IDISP023

        _isDisposed = true;
        base.Dispose(isDisposing);
    }
    #endregion
}