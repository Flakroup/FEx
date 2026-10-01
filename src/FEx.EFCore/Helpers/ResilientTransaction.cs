using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.EFCore.Helpers;

/// <summary>
/// Use of an EF Core resiliency strategy when using multiple DbContexts within an explicit BeginTransaction():
/// See: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
/// </summary>
public class ResilientTransaction
{
    private const int MaxBeginAttempts = 10;

    private readonly IFExLogger _logger;

    public ResilientTransaction(IFExLogger logger)
    {
        _logger = logger;
    }

    public Task<T> ExecuteAsync<T>(DbContext context, Func<Task<T>> action, string id) =>
        ExecuteAsync(context, action, id, IsolationLevel.Unspecified, null);

    public async Task<T> ExecuteAsync<T>(DbContext context,
                                         Func<Task<T>> action,
                                         string id,
                                         IsolationLevel isolationLevel,
                                         int? delayOnTimeout,
                                         CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(ct =>
            RunTransactionAsync(context, action, id, isolationLevel, delayOnTimeout, ct), cancellationToken);
    }

    public Task<T> ExecuteAsync<T>(DbContext context, Func<T> action, string id) =>
        ExecuteAsync(context, action, id, IsolationLevel.Unspecified, null);

    public async Task<T> ExecuteAsync<T>(DbContext context,
                                         Func<T> action,
                                         string id,
                                         IsolationLevel isolationLevel,
                                         int? delayOnTimeout,
                                         CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(ct =>
            RunTransactionAsync(context, action, id, isolationLevel, delayOnTimeout, ct), cancellationToken);
    }

    public T Execute<T>(DbContext context,
                        Func<T> action,
                        string id,
                        IsolationLevel isolationLevel = IsolationLevel.Unspecified,
                        int? delayOnTimeout = null)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return strategy.Execute(() => RunTransaction(context, action, id, isolationLevel, delayOnTimeout));
    }

    private async Task<T> RunTransactionAsync<T>(DbContext context,
                                                 Func<Task<T>> action,
                                                 string id,
                                                 IsolationLevel isolationLevel = IsolationLevel.Unspecified,
                                                 int? delayOnTimeout = null,
                                                 CancellationToken cancellationToken = default)
    {
        T res;

        await using var transaction =
            await GetTransactionAsync(context, id, isolationLevel, delayOnTimeout, cancellationToken);

        try
        {
            res = await action();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }

        return res;
    }

    private async Task<T> RunTransactionAsync<T>(DbContext context,
                                                 Func<T> action,
                                                 string id,
                                                 IsolationLevel isolationLevel = IsolationLevel.Unspecified,
                                                 int? delayOnTimeout = null,
                                                 CancellationToken cancellationToken = default)
    {
        T res;

        await using var transaction =
            await GetTransactionAsync(context, id, isolationLevel, delayOnTimeout, cancellationToken);

        try
        {
            res = action();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }

        return res;
    }

    private T RunTransaction<T>(DbContext context,
                                Func<T> action,
                                string id,
                                IsolationLevel isolationLevel = IsolationLevel.Unspecified,
                                int? delayOnTimeout = null)
    {
        T res;

        using var transaction = GetTransaction(context, id, isolationLevel, delayOnTimeout);

        try
        {
            res = action();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();

            throw;
        }

        return res;
    }

    private async Task<IDbContextTransaction> GetTransactionAsync(DbContext context,
                                                                  string id,
                                                                  IsolationLevel isolationLevel =
                                                                      IsolationLevel.Unspecified,
                                                                  int? delayOnTimeout = null,
                                                                  CancellationToken cancellationToken = default)
    {
        for (var attempt = 1;; attempt++)
        {
            try
            {
                return await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            }
            catch (InvalidOperationException ex) when (attempt < MaxBeginAttempts)
            {
                _logger.Error(ex, $"[{id}]\t{ex.Message}");
            }

            await Task.Delay(delayOnTimeout ?? 100, cancellationToken);
        }
    }

    private IDbContextTransaction GetTransaction(DbContext context,
                                                 string id,
                                                 IsolationLevel isolationLevel = IsolationLevel.Unspecified,
                                                 int? delayOnTimeout = null)
    {
        for (var attempt = 1;; attempt++)
        {
            try
            {
                return context.Database.BeginTransaction(isolationLevel);
            }
            catch (InvalidOperationException ex) when (attempt < MaxBeginAttempts)
            {
                _logger.Error(ex, $"[{id}]\t{ex.Message}");
            }

            JoinableAsyncHelper.DelayWithoutDeadlock(delayOnTimeout ?? 100);
        }
    }
}