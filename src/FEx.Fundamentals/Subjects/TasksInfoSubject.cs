using FEx.Abstractions.Interfaces;
using FEx.Basics.Collections.Concurrent;
using FEx.Rx.Extensions;
using FEx.Rx.Subjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace FEx.Fundamentals.Subjects;

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

    public void AddTask(ITaskWrapperBase value)
    {
        _tasks.Add(value.Id);
        OnNext(_tasks);
    }

    public void RemoveTask(ITaskWrapperBase value)
    {
        _tasks.Remove(value.Id);
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