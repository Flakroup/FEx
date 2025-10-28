using EFCore.BulkExtensions;
using FEx.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#if NETSTANDARD
using Microsoft.Data.SqlClient;
#endif

namespace FEx.EFCore.Extensions;

public static class BulkOperationsExtensions
{
    public static Func<BulkConfig> DefaultBulkConfig { get; } = () => new()
    {
        BatchSize = 2000,
        WithHoldlock = true,
        BulkCopyTimeout = 0,
        SqlBulkCopyOptions = SqlBulkCopyOptions.TableLock
    };

    public static async Task BulkInsertAsync<TDbContext, T>(this IBulkDbServiceBase<TDbContext> service,
                                                            IList<T> entities,
                                                            string errorMessage = null,
                                                            CancellationToken cancellationToken = default)
        where TDbContext : DbContext where T : class =>
        await DoBulkDbContextTaskAsync(entities,
            service,
            ctx => ctx.BulkInsertAsync,
            service.BulkConfig,
            errorMessage,
            OperationType.Insert,
            cancellationToken);

    public static async Task BulkDeleteAsync<TDbContext, T>(this IBulkDbServiceBase<TDbContext> service,
                                                            IList<T> entities,
                                                            string errorMessage = null,
                                                            CancellationToken cancellationToken = default)
        where TDbContext : DbContext where T : class =>
        await DoBulkDbContextTaskAsync(entities,
            service,
            ctx => ctx.BulkDeleteAsync,
            service.BulkConfig,
            errorMessage,
            OperationType.Delete,
            cancellationToken);

    public static async Task BulkInsertOrUpdateAsync<TDbContext, T>(this IBulkDbServiceBase<TDbContext> service,
                                                                    IList<T> entities,
                                                                    string errorMessage = null,
                                                                    CancellationToken cancellationToken = default)
        where TDbContext : DbContext where T : class =>
        await DoBulkDbContextTaskAsync(entities,
            service,
            ctx => ctx.BulkInsertOrUpdateAsync,
            service.BulkConfig,
            errorMessage,
            OperationType.InsertOrUpdate,
            cancellationToken);

    public static async Task BulkInsertOrUpdateOrDeleteAsync<TDbContext, T>(
        this IBulkDbServiceBase<TDbContext> service,
        IList<T> entities,
        string errorMessage = null,
        CancellationToken cancellationToken = default) where TDbContext : DbContext where T : class
    {
        const OperationType operationType =
#if NETSTANDARD
            OperationType.InsertOrUpdateDelete;
#else
            OperationType.InsertOrUpdateOrDelete;
#endif
        await DoBulkDbContextTaskAsync(entities,
            service,
            ctx => ctx.BulkInsertOrUpdateOrDeleteAsync,
            service.BulkConfig,
            errorMessage,
            operationType,
            cancellationToken);
    }

    public static async Task BulkReadAsync<TDbContext, T>(this IBulkDbServiceBase<TDbContext> service,
                                                          IList<T> entities,
                                                          string errorMessage = null,
                                                          CancellationToken cancellationToken = default)
        where TDbContext : DbContext where T : class =>
        await DoBulkDbContextTaskAsync(entities,
            service,
            ctx => ctx.BulkReadAsync,
            service.BulkConfig,
            errorMessage,
            OperationType.Read,
            cancellationToken);

    public static async Task BulkUpdateAsync<TDbContext, T>(this IBulkDbServiceBase<TDbContext> service,
                                                            IList<T> entities,
                                                            string errorMessage = null,
                                                            CancellationToken cancellationToken = default)
        where TDbContext : DbContext where T : class =>
        await DoBulkDbContextTaskAsync(entities,
            service,
            ctx => ctx.BulkUpdateAsync,
            service.BulkConfig,
            errorMessage,
            OperationType.Update,
            cancellationToken);

    private static BulkConfig EnsureConfig<T>(ICollection<T> entities, Func<BulkConfig> config) where T : class
    {
        BulkConfig cfg = config?.Invoke() ?? DefaultBulkConfig();

        cfg.NotifyAfter = cfg.NotifyAfter
                          ?? Math.Max((int)Math.Ceiling(entities.Count / 5D), (int)Math.Ceiling(cfg.BatchSize / 5D));

        return cfg;
    }

    private static async Task DoBulkDbContextTaskAsync<TDbContext, T>(IList<T> entities,
                                                                      IBulkDbServiceBase<TDbContext> service,
                                                                      Func<TDbContext, Func<IList<T>, BulkConfig,
                                                                          Action<decimal>, Type, CancellationToken,
                                                                          Task>> func,
                                                                      Func<BulkConfig> configFunc,
                                                                      string errorMessage,
                                                                      OperationType type,
                                                                      CancellationToken cancellationToken)
        where TDbContext : DbContext where T : class
    {
        BulkConfig config = EnsureConfig(entities, configFunc);

        if (service.BulkOperationsSemaphore is not null)
            await service.BulkOperationsSemaphore.WaitAsync(cancellationToken);

        try
        {
            var id = Guid.NewGuid().ToString();
            var hasCompleted = false;
            string tableName = service.TableMappings.ForwardIndex[typeof(T).FullName];

            await service.RunTaskInDbContextAsync(ctx => func(ctx)(entities,
                    config,
                    config.NotifyAfter is not null
                        ? p =>
                        {
                            hasCompleted = p == 1;
                            service.ProgressAction(p, id, type, tableName);
                        }
                        : null,
                    null,
                    cancellationToken),
                errorMessage,
                false,
                false);

            if (!hasCompleted)
                service.ProgressAction(1, id, type, tableName);
        }
        finally
        {
            service.BulkOperationsSemaphore?.Release();
        }
    }
}