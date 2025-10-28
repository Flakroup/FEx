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
    private readonly IFExLogger _logger;

    public ResilientTransaction(IFExLogger logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(DbContext context,
                                         Func<Task<T>> action,
                                         string id,
                                         IsolationLevel isolationLevel = IsolationLevel.Unspecified,
                                         int? delayOnTimeout = null)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(() =>
            RunTransactionAsync(context, action, id, isolationLevel, delayOnTimeout));
    }

    public async Task<T> ExecuteAsync<T>(DbContext context,
                                         Func<T> action,
                                         string id,
                                         IsolationLevel isolationLevel = IsolationLevel.Unspecified,
                                         int? delayOnTimeout = null)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(() =>
            RunTransactionAsync(context, action, id, isolationLevel, delayOnTimeout));
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
                                                 int? delayOnTimeout = null)
    {
        T res;

        await using var transaction = await GetTransactionAsync(context, id, isolationLevel, delayOnTimeout);

        try
        {
            res = await action();

            try
            {
                await transaction.CommitAsync();
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ex, $"[{id}]\t{ex.Message}");
                //ignored
            }
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
                                                 int? delayOnTimeout = null)
    {
        T res;

        await using var transaction = await GetTransactionAsync(context, id, isolationLevel, delayOnTimeout);

        try
        {
            res = action();

            try
            {
                await transaction.CommitAsync();
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ex, $"[{id}]\t{ex.Message}");
                //ignored
            }
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

            try
            {
                transaction.Commit();
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ex, $"[{id}]\t{ex.Message}");
                //ignored
            }
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
                                                                  int? delayOnTimeout = null)
    {
        do
        {
            try
            {
                return await context.Database.BeginTransactionAsync(isolationLevel, CancellationToken.None);
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ex, $"[{id}]\t{ex.Message}");
                //ignored
            }

            if (delayOnTimeout.HasValue)
                await Task.Delay(delayOnTimeout.Value);
        } while (true);
    }

    private IDbContextTransaction GetTransaction(DbContext context,
                                                 string id,
                                                 IsolationLevel isolationLevel = IsolationLevel.Unspecified,
                                                 int? delayOnTimeout = null)
    {
        do
        {
            try
            {
                return context.Database.BeginTransaction(isolationLevel);
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ex, $"[{id}]\t{ex.Message}");
                //ignored
            }

            if (delayOnTimeout.HasValue)
                JoinableAsyncHelper.DelayWithoutDeadlock(delayOnTimeout.Value);
        } while (true);
    }
}