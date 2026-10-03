using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions.Interfaces;
using FEx.Legacy.Asyncx;
using NSubstitute;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Legacy.Tests;

/// <summary>A failed task must reach the awaiter: returning default(T) is indistinguishable from a real result.</summary>
public sealed class TasksHandlerTests
{
    private readonly ITasksInfoSubject _tasksInfo = Substitute.For<ITasksInfoSubject>();
    private readonly TasksHandler _handler;

    public TasksHandlerTests() => _handler = new(Substitute.For<IAsyncHelper>(), _tasksInfo);

    [Fact]
    public async Task RunTaskAsync_FailingTask_RethrowsToTheAwaiter_AfterPostAndCleanup()
    {
        bool? success = null;

        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _handler.RunTaskAsync<int>(() => throw new InvalidOperationException("boom"),
                null,
                null,
                (ok, _) => success = ok,
                AsyncMode.ThreadPool));

        ex.Message.ShouldBe("boom");
        success.ShouldBe(false);
        _tasksInfo.Received(1).RemoveTask(Arg.Any<Guid>());
    }

    [Fact]
    public async Task RunFuncAsync_FailingFunc_RethrowsToTheAwaiter_AfterPostAndCleanup()
    {
        bool? success = null;

        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _handler.RunFuncAsync<int>(() => throw new InvalidOperationException("boom"),
                null,
                null,
                (ok, _) => success = ok,
                AsyncMode.ThreadPool,
                CancellationToken.None));

        ex.Message.ShouldBe("boom");
        success.ShouldBe(false);
        _tasksInfo.Received(1).RemoveTask(Arg.Any<Guid>());
    }

    [Fact]
    public async Task RunFuncAsync_SucceedingFunc_ReturnsTheResult()
    {
        var result = await _handler.RunFuncAsync(() => 42, null, null, null, AsyncMode.ThreadPool, CancellationToken.None);

        result.ShouldBe(42);
    }
}
