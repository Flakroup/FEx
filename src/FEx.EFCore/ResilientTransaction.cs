using FEx.Async;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.EFCore;

/// <summary>
///     Use of an EF Core resiliency strategy when using multiple DbContexts within an explicit BeginTransaction():
///     See: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
/// </summary>
public class ResilientTransaction
{
    private readonly ILogger _logger;

    public ResilientTransaction(ILogger<ResilientTransaction> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(DbContext context, Func<Task<T>> action, IsolationLevel isolationLevel = IsolationLevel.Unspecified, int? delayOnTimeout = null)
    {
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => RunTransactionAsync(context, action, isolationLevel, delayOnTimeout));
    }

    public T Execute<T>(DbContext context, Func<T> action, IsolationLevel isolationLevel = IsolationLevel.Unspecified, int? delayOnTimeout = null)
    {
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return strategy.Execute(() => RunTransaction(context, action, isolationLevel, delayOnTimeout));
    }

    private async Task<T> RunTransactionAsync<T>(DbContext context, Func<Task<T>> action, IsolationLevel isolationLevel = IsolationLevel.Unspecified, int? delayOnTimeout = null)
    {
        T res;

        await using IDbContextTransaction transaction = await GetTransactionAsync(context, isolationLevel, delayOnTimeout);
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

    private T RunTransaction<T>(DbContext context, Func<T> action, IsolationLevel isolationLevel = IsolationLevel.Unspecified, int? delayOnTimeout = null)
    {
        T res;

        using IDbContextTransaction transaction = GetTransaction(context, isolationLevel, delayOnTimeout);
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

    private async Task<IDbContextTransaction> GetTransactionAsync(DbContext context, IsolationLevel isolationLevel = IsolationLevel.Unspecified, int? delayOnTimeout = null)
    {
        do
        {
            try
            {
                return await context.Database.BeginTransactionAsync(isolationLevel, CancellationToken.None);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex.Message, ex);
                //ignored
            }

            if (delayOnTimeout.HasValue)
                await Task.Delay(delayOnTimeout.Value);
        } while (true);
    }

    private IDbContextTransaction GetTransaction(DbContext context, IsolationLevel isolationLevel = IsolationLevel.Unspecified, int? delayOnTimeout = null)
    {
        do
        {
            try
            {
                return context.Database.BeginTransaction(isolationLevel);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex.Message, ex);
                //ignored
            }

            if (delayOnTimeout.HasValue)
                JoinableAsyncHelper.DelayWithoutDeadlock(delayOnTimeout.Value);
        } while (true);
    }
}