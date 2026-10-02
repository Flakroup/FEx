using FEx.Agnostics.Abstractions.Interfaces;
using FEx.EFCore.Helpers;
using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.EFCore.Tests;

public class ResilientTransactionTests
{
    private readonly IFExLogger _logger = Substitute.For<IFExLogger>();
    private readonly ResilientTransaction _sut;

    public ResilientTransactionTests() => _sut = new(_logger);

    private int LoggedErrors => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name.Contains("Error"));

    [Fact]
    public async Task ExecuteAsync_FailedCommit_Throws()
    {
        using var db = new SqliteMemory();
        await using var context = db.CreateContext();

        // The action completes the transaction itself, so the helper's own Commit is invalid.
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => _sut.ExecuteAsync(context,
            () =>
            {
                context.Database.CurrentTransaction!.Commit();

                return 1;
            },
            "t",
            System.Data.IsolationLevel.Unspecified,
            1));

        ShouldBeCommitFailure(ex);
    }

    [Fact]
    public async Task ExecuteAsync_AsyncDelegate_FailedCommit_Throws()
    {
        using var db = new SqliteMemory();
        await using var context = db.CreateContext();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => _sut.ExecuteAsync(context,
            async () =>
            {
                await context.Database.CurrentTransaction!.CommitAsync();

                return 1;
            },
            "t",
            System.Data.IsolationLevel.Unspecified,
            1));

        ShouldBeCommitFailure(ex);
    }

    [Fact]
    public void Execute_FailedCommit_Throws()
    {
        using var db = new SqliteMemory();
        using var context = db.CreateContext();

        var ex = Should.Throw<InvalidOperationException>(() => _sut.Execute(context,
            () =>
            {
                context.Database.CurrentTransaction!.Commit();

                return 1;
            },
            "t"));

        ShouldBeCommitFailure(ex);
    }

    [Fact]
    public async Task ExecuteAsync_BeginTransactionKeepsFailing_ThrowsAfterRetryCap()
    {
        using var db = new SqliteMemory();
        await using var context = db.CreateContext();
        await using var outer = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // A transaction is already active, so every BeginTransaction throws InvalidOperationException.
        var task = _sut.ExecuteAsync(context,
            () => Task.FromResult(1),
            "t",
            System.Data.IsolationLevel.Unspecified,
            1,
            TestContext.Current.CancellationToken);

        await Should.ThrowAsync<InvalidOperationException>(task).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        // Every failed attempt but the last is logged, so 10 attempts mean 9 logged errors.
        LoggedErrors.ShouldBe(9);
    }

    [Fact]
    public async Task Execute_BeginTransactionKeepsFailing_ThrowsAfterRetryCap()
    {
        using var db = new SqliteMemory();
        await using var context = db.CreateContext();
        await using var outer = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

        var task = Task.Run(() => _sut.Execute(context, () => 1, "t", delayOnTimeout: 1),
            TestContext.Current.CancellationToken);

        await Should.ThrowAsync<InvalidOperationException>(task).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        LoggedErrors.ShouldBe(9);
    }

    [Fact]
    public async Task ExecuteAsync_Cancelled_StopsRetrying()
    {
        using var db = new SqliteMemory();
        await using var context = db.CreateContext();
        await using var outer = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var task = _sut.ExecuteAsync(context,
            () => Task.FromResult(1),
            "t",
            System.Data.IsolationLevel.Unspecified,
            60_000,
            cts.Token);

        await Should.ThrowAsync<OperationCanceledException>(task).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_SyncDelegate_Cancelled_StopsRetrying()
    {
        using var db = new SqliteMemory();
        await using var context = db.CreateContext();
        await using var outer = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var task = _sut.ExecuteAsync(context,
            () => 1,
            "t",
            System.Data.IsolationLevel.Unspecified,
            60_000,
            cts.Token);

        await Should.ThrowAsync<OperationCanceledException>(task).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
    }

    // The rollback on a completed transaction throws too; the commit error must be the one surfaced.
    private static void ShouldBeCommitFailure(Exception ex)
    {
        ex.StackTrace.ShouldNotBeNull();
        ex.StackTrace.ShouldContain("Commit");
        ex.StackTrace.ShouldNotContain("Rollback");
    }
}
