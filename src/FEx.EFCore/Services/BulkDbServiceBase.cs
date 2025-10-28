using EFCore.BulkExtensions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Configuration;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;

namespace FEx.EFCore.Services;

/// <summary>
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
/// <remarks>Requires <c>Initialize();</c> call in .ctor</remarks>
public abstract class BulkDbServiceBase<TDbContext> : DbServiceBase<TDbContext>, IBulkDbServiceBase<TDbContext>
    where TDbContext : DbContext
{
    private bool _isDisposed;

    public Func<BulkConfig> BulkConfig { get; }
    public SemaphoreSlim BulkOperationsSemaphore { get; }

    protected BulkDbServiceBase(IScopeProvider scopeProvider,
                                IBulkDbConfig bulkDbConfig,
                                ResilientTransaction resilientTransaction,
                                IFExDbConfig dbConfig,
                                IAsyncInitializable[] dependencies)
        : base(scopeProvider, resilientTransaction, dbConfig, dependencies)
    {
        BulkConfig = bulkDbConfig?.Config;

        if (bulkDbConfig?.ParallelBulkOperations > 0)
            BulkOperationsSemaphore = new(bulkDbConfig.ParallelBulkOperations, bulkDbConfig.ParallelBulkOperations);
    }

    public void ProgressAction(decimal progress, string operationId, OperationType type, string tableName)
    {
        var opIdStr = operationId.IsNotNullOrEmptyString()
            ? $"{operationId} "
            : string.Empty;

        var prgStr = progress == 1
            ? "Completed"
            : $"{Math.Floor(progress * 100)}%";

        _logger.Debug($"[BulkOperation] [{tableName}] [{type}] {opIdStr}{prgStr}");
    }

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            BulkOperationsSemaphore?.Dispose();

        _isDisposed = true;

        base.Dispose(disposing);
    }
    #endregion
}