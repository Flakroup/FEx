using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;

namespace FEx.EFCore.Interfaces;

public interface IBulkDbServiceBase<TDbContext> : IDbServiceBase<TDbContext> where TDbContext : DbContext
{
    Func<BulkConfig> BulkConfig { get; }
    SemaphoreSlim BulkOperationsSemaphore { get; }

    void ProgressAction(decimal progress, string operationId, OperationType type, string tableName);
}