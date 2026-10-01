using FEx.Agnostics.Abstractions.Interfaces;
using FEx.EFCore.Helpers;
using NSubstitute;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.EFCore.Tests;

public class ResilientTransactionTests
{
    private readonly ResilientTransaction _sut = new(Substitute.For<IFExLogger>());

    [Fact]
    public async Task ExecuteAsync_FailedCommit_Throws()
    {
        using var db = new SqliteMemory();
        await using var context = db.CreateContext();

        // The action completes the transaction itself, so the helper's own Commit is invalid.
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.ExecuteAsync(context,
            () =>
            {
                context.Database.CurrentTransaction!.Commit();

                return 1;
            },
            "t",
            System.Data.IsolationLevel.Unspecified,
            1));
    }

    [Fact]
    public void Execute_FailedCommit_Throws()
    {
        using var db = new SqliteMemory();
        using var context = db.CreateContext();

        Should.Throw<InvalidOperationException>(() => _sut.Execute(context,
            () =>
            {
                context.Database.CurrentTransaction!.Commit();

                return 1;
            },
            "t"));
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
}
