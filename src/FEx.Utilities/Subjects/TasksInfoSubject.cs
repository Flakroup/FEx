using FEx.Abstractions;
using FEx.Rx;
using FEx.Rx.Extensions;
using FEx.Utilities.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FEx.Utilities.Subjects;

public class TasksInfoSubject : FExSubject<IList<Guid>>, ITasksInfoSubject
{
    private readonly ILogger _logger;
    private readonly IList<Guid> _tasks;
    private readonly IDisposable _subscription;

    public TasksInfoSubject(ILogger logger)
        : base(new BehaviorSubject<IList<Guid>>(null))
    {
        _logger = logger;
        _tasks = new ConcurrentList<Guid>();
        _subscription = Observable.Interval(TimeSpan.FromSeconds(1))
            .Where(_ => _tasks.Count > 0)
            .AsyncSubscribe(_ => HandleTasks());
        OnNext(_tasks);
    }

    public void AddTask(ITaskWrapper value)
    {
        _tasks.Add(value.Id);
        base.OnNext(_tasks);
    }

    public void RemoveTask(ITaskWrapper value)
    {
        _tasks.Remove(value.Id);
        base.OnNext(_tasks);
        if (_tasks.Count == 0)
            HandleTasks();
    }

    private void HandleTasks()
    {
        switch (_tasks.Count)
        {
            case 0:
                _logger.LogInformation("All tasks had finished");
                break;
            case 1:
                _logger.LogInformation($"{_tasks.Count} task running");
                break;
            case > 1:
                _logger.LogInformation($"{_tasks.Count} tasks running");
                break;
        }
    }
}