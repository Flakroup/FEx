using FEx.Core.Subjects;
using Microsoft.Extensions.Logging;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Core.Tests.Subjects;

/// <summary>
/// Pins the structured log templates of <see cref="TasksInfoSubject" />: constant template, count passed as argument.
/// </summary>
public sealed class TasksInfoSubjectTests
{
    private sealed class CapturingLogger : ILogger<TasksInfoSubject>
    {
        public ConcurrentQueue<(string Message, string? Template)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var template = (state as IEnumerable<KeyValuePair<string, object?>>)?
                .FirstOrDefault(x => x.Key == "{OriginalFormat}").Value as string;

            Entries.Enqueue((formatter(state, exception), template));
        }
    }

    [Fact]
    public void RemoveTask_LogsThatAllTasksFinished_WhenTheLastTaskIsRemoved()
    {
        var logger = new CapturingLogger();
        using var subject = new TasksInfoSubject(logger);
        var id = Guid.NewGuid();

        subject.AddTask(id);
        subject.RemoveTask(id);

        logger.Entries.ShouldContain(("All tasks have finished", "All tasks have finished"));
    }

    [Fact]
    public async Task HandleTasks_RendersTheTaskCountThroughTheTemplate_WhileTasksRun()
    {
        var logger = new CapturingLogger();
        using var subject = new TasksInfoSubject(logger);

        subject.AddTask(Guid.NewGuid());
        subject.AddTask(Guid.NewGuid());

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (logger.Entries.IsEmpty && DateTime.UtcNow < deadline)
            await Task.Delay(100, TestContext.Current.CancellationToken);

        logger.Entries.ShouldContain(("2 tasks running", "{TaskCount} tasks running"));
    }
}
