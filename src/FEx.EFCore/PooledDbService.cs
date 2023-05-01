using FEx.Abstractions;
using FEx.Extensions;
using FEx.Extensions.Collections.Enumerables;
using FEx.Utilities.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.EFCore;

public abstract class PooledDbService<TDbContext> : IPooledDbService<TDbContext> where TDbContext : DbContext
{
    protected readonly ILogger _logger;
    private readonly IScopeProvider _scopeProvider;
    private readonly ResilientTransaction _transaction;

    //public int DelayOnTimeout { get; }

    public Map<string, string> Mappings { get; private set; }

    protected PooledDbService(IScopeProvider scopeProvider, ILogger logger, ResilientTransaction transaction)
    {
        _scopeProvider = scopeProvider;
        _logger = logger;
        _transaction = transaction;

        //DelayOnTimeout = dbConfig.DelayOnTimeout;
    }

    public async Task InitializeAsync(Func<TDbContext, Task> afterAppliedMigration = null, bool dropIfMigrationFailed = false)
    {
        await RunMigrationsAsync(afterAppliedMigration, dropIfMigrationFailed);

        EnsureMappingSnapshot();
    }

    public async Task RunMigrationsAsync(Func<TDbContext, Task> afterAppliedMigration = null, bool dropIfMigrationFailed = false)
    {
        try
        {
            await MigrateAsync(afterAppliedMigration);
        }
        catch when (dropIfMigrationFailed && Drop())
        {
            await MigrateAsync(afterAppliedMigration);
        }
    }

    public async Task MigrateAsync(Func<TDbContext, Task> afterAppliedMigration = null)
    {
        bool hasNoPendingMigrations = HasNoPendingMigrations();

        if (!hasNoPendingMigrations)
        {
            RunActionInDbContext(dbContext => dbContext.Database.Migrate(), null, false, false);
            await RunTaskInDbContextAsync(afterAppliedMigration);
        }

        hasNoPendingMigrations = HasNoPendingMigrations();

        if (!hasNoPendingMigrations)
            throw new($"Applying migrations for {typeof(TDbContext).FullName} failed.");
    }

    public void RunActionInDbContext(Action<TDbContext> func, string errorMessage = null, bool saveChanges = true, bool useTransaction = true)
    {
        RunFuncInDbContext(dbContext =>
        {
            func(dbContext);
            return (object)null;
        }, errorMessage, saveChanges, useTransaction);
    }

    public T RunFuncInDbContext<T>(Func<TDbContext, T> func, string errorMessage = null, bool saveChanges = true, bool useTransaction = true)
    {
        return RunWithinTransaction(func, errorMessage, saveChanges, useTransaction);
    }

    public async Task RunTaskInDbContextAsync(Func<TDbContext, Task> func, string errorMessage = null, bool saveChanges = true, bool useTransaction = true)
    {
        await RunTaskInDbContextAsync(func.WrapTask, errorMessage, saveChanges, useTransaction);
    }

    public async Task<T> RunTaskInDbContextAsync<T>(Func<TDbContext, Task<T>> func, string errorMessage = null, bool saveChanges = true, bool useTransaction = true)
    {
        return await RunWithinTransactionAsync(func, errorMessage, saveChanges, useTransaction);
    }

    public async Task<T> RunTaskInDbContextAsync<T>(Func<TDbContext, Func<Task<T>>> func, string errorMessage = null, bool saveChanges = true, bool useTransaction = true)
    {
        return await RunWithinTransactionAsync(dbContext => func(dbContext)(), errorMessage, saveChanges, useTransaction);
    }

    protected virtual void OnValidationSuccess(string id, IReadOnlyCollection<EntityEntry> entities)
    {
    }

    protected virtual void OnValidationFail(string id, IReadOnlyCollection<EntityValidationFail> failedValidations)
    {
    }

    protected virtual void OnFaultyEntity(string id, EntityValidationFail failedValidation)
    {
    }

    protected virtual void OnValidationStart(string id, IReadOnlyCollection<EntityEntry> entities)
    {
    }

    protected void EnsureMappingSnapshot()
    {
        if (Mappings is null)
            Mappings = new();
        else
            Mappings.Clear();

        RunActionInDbContext(dbContext =>
        {
            foreach (IEntityType type in dbContext.Model.GetEntityTypes()
                         .ToList())
            {
                Mappings.Add(type.ClrType.Name, dbContext.Model.FindEntityType(type.Name)
                    .GetTableName());
            }
        });
    }

    protected T RunWithinTransaction<T>(Func<TDbContext, T> func, string errorMessage, bool saveChanges, bool useTransaction = true, IsolationLevel isolationLevel = IsolationLevel.Unspecified, int? delayOnTimeout = null)
    {
        using IServiceScope scope = _scopeProvider.CreateScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        try
        {
            if (useTransaction && dbContext.Database.CurrentTransaction is null)
                return _transaction.Execute(dbContext, () => Execute(dbContext, func, saveChanges), isolationLevel, delayOnTimeout);

            return Execute(dbContext, func, saveChanges);
        }
        catch (Exception e)
        {
            _logger.LogError($"{errorMessage ?? ""} {e.Message}", e);
            throw;
        }
    }

    protected async Task<T> RunWithinTransactionAsync<T>(Func<TDbContext, Task<T>> func, string errorMessage, bool saveChanges, bool useTransaction = true, IsolationLevel isolationLevel = IsolationLevel.Unspecified, int? delayOnTimeout = null)
    {
        using IServiceScope scope = _scopeProvider.CreateScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        try
        {
            if (useTransaction && dbContext.Database.CurrentTransaction is null)
                return await _transaction.ExecuteAsync(dbContext, () => ExecuteAsync(dbContext, func, saveChanges), isolationLevel, delayOnTimeout);

            return await ExecuteAsync(dbContext, func, saveChanges);
        }
        catch (Exception e)
        {
            _logger.LogError($"{errorMessage ?? ""} {e.Message}", e);
            throw;
        }
    }

    protected bool? ValidateAndSaveChanges(TDbContext dbContext, bool validateAllProperties = true, bool acceptAllChangesOnSuccess = true)
    {
        bool? isSuccess = dbContext.ValidateChangedEntities(null, validateAllProperties, OnValidationStart, OnFaultyEntity, OnValidationFail, OnValidationSuccess);

        if (isSuccess != true)
            return isSuccess;

        _logger.LogInformation("Saving changes to database");

        var res = 0;
        var saved = false;
        var retries = 1;

        while (!saved)
        {
            try
            {
                res = dbContext.SaveChanges(acceptAllChangesOnSuccess);
                saved = true;
            }
            catch (DbUpdateConcurrencyException ex) when (retries > 0)
            {
                foreach (EntityEntry entry in ex.Entries)
                {
                    PropertyValues databaseValues = entry.GetDatabaseValues();

                    if (databaseValues is not null)
                    {
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                    else
                    {
                        if (entry.State == EntityState.Deleted)
                            entry.State = EntityState.Detached;
                        else if (entry.State == EntityState.Modified)
                            entry.State = EntityState.Added;
                        else
                            throw;
                    }
                }

                retries--;
            }
        }

        _logger.LogInformation($"{res} rows affected");
        return true;
    }

    protected async Task<bool?> ValidateAndSaveChangesAsync(TDbContext dbContext, bool validateAllProperties = true, bool acceptAllChangesOnSuccess = true)
    {
        bool? isSuccess = dbContext.ValidateChangedEntities(null, validateAllProperties, OnValidationStart, OnFaultyEntity, OnValidationFail, OnValidationSuccess);

        if (isSuccess != true)
            return isSuccess;
        _logger.LogInformation("Saving changes to database");

        var res = 0;
        var saved = false;
        var retries = 1;

        while (!saved)
        {
            try
            {
                res = await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess);
                saved = true;
            }
            catch (DbUpdateConcurrencyException ex) when (retries > 0)
            {
                foreach (EntityEntry entry in ex.Entries)
                {
                    PropertyValues databaseValues = await entry.GetDatabaseValuesAsync();

                    if (databaseValues is not null)
                    {
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                    else
                    {
                        if (entry.State == EntityState.Deleted)
                            entry.State = EntityState.Detached;
                        else if (entry.State == EntityState.Modified)
                            entry.State = EntityState.Added;
                        else
                            throw;
                    }
                }

                retries--;
            }
        }

        _logger.LogInformation($"{res} rows affected");
        return true;
    }

    private bool Drop()
    {
        return RunFuncInDbContext(dbContext => dbContext.Database.EnsureDeleted(), null, false, false);
    }

    private bool HasNoPendingMigrations()
    {
        return RunFuncInDbContext(dbContext =>
        {
            string[] migs = dbContext.Database.GetMigrations()
                .ToArray();
            string[] aMigs = dbContext.Database.GetAppliedMigrations()
                .ToArray();
            string[] pMigs = dbContext.Database.GetPendingMigrations()
                .ToArray();

            return migs.UnorderedSequenceEqual(aMigs) && pMigs.Length == 0;
        }, null, false, false);
    }

    private T Execute<T>(TDbContext dbContext, Func<TDbContext, T> func, bool saveChanges)
    {
        T res = func(dbContext);

        if (saveChanges)
            ValidateAndSaveChanges(dbContext);

        return res;
    }

    private async Task<T> ExecuteAsync<T>(TDbContext dbContext, Func<TDbContext, Task<T>> func, bool saveChanges)
    {
        T res = await func(dbContext);

        if (saveChanges)
            await ValidateAndSaveChangesAsync(dbContext);

        return res;
    }
}