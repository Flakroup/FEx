using FEx.Abstractions.Flow;
using FEx.Abstractions.Flow.Errors;
using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Abstractions;
using FEx.Common.Collections;
using FEx.Common.Extensions;
using FEx.EFCore.Extensions;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Models;
using FEx.Extensions;
using FEx.Extensions.Collections.Enumerables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.EFCore.Services;

/// <summary>
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
/// <remarks>Requires <c>BeginInitialization();</c> call in .ctor</remarks>
public abstract class PooledDbService<TDbContext> : AsyncInitializable, IPooledDbService<TDbContext>
    where TDbContext : DbContext
{
    protected readonly IFExDbConfig _dbConfig;
    private readonly IScopeProvider _scopeProvider;
    private readonly ResilientTransaction _transaction;

    public int? DelayOnTimeout => _dbConfig.DelayOnTimeout;

    public IReadOnlyDictionary<string, Mapping> Mappings { get; private set; }
    public Map<string, string> TableMappings { get; private set; }

    protected PooledDbService(IScopeProvider scopeProvider,
                              ResilientTransaction transaction,
                              IFExDbConfig dbConfig,
                              params IAsyncInitializable[] dependencies)
        : base(dependencies)
    {
        _scopeProvider = scopeProvider;
        _transaction = transaction;
        _dbConfig = dbConfig;
    }

    public async Task RunTaskInDbContextAsync(Func<TDbContext, Task> func,
                                              string errorMessage = null,
                                              bool saveChanges = true,
                                              bool useTransaction = true) =>
        await RunTaskInDbContextAsync(func.WrapTask, errorMessage, saveChanges, useTransaction);

    public async Task<T> RunTaskInDbContextAsync<T>(Func<TDbContext, Task<T>> func,
                                                    string errorMessage = null,
                                                    bool saveChanges = true,
                                                    bool useTransaction = true) =>
        await RunWithinTransactionAsync(func, errorMessage, saveChanges, useTransaction);

    public async Task<T> RunTaskInDbContextAsync<T>(Func<TDbContext, Func<Task<T>>> func,
                                                    string errorMessage = null,
                                                    bool saveChanges = true,
                                                    bool useTransaction = true) =>
        await RunWithinTransactionAsync(dbContext => func(dbContext)(), errorMessage, saveChanges, useTransaction);

    public async Task<bool> RunMigrationsAsync()
    {
        try
        {
            await MigrateAsync();

            return true;
        }
        catch when (_dbConfig.DropIfMigrationFailed)
        {
            if (await DropAsync())
            {
                await MigrateAsync();

                return true;
            }

            throw;
        }
    }

    public async Task MigrateAsync()
    {
        bool hasNoPendingMigrations = await HasNoPendingMigrationsAsync();

        if (!hasNoPendingMigrations)
        {
            await RunActionInDbContextAsync(dbContext => dbContext.Database.Migrate(), null, false, false);
            await AfterAppliedMigrationAsync();
            hasNoPendingMigrations = await HasNoPendingMigrationsAsync();
        }

        if (!hasNoPendingMigrations)
            throw new($"Applying migrations for {typeof(TDbContext).FullName} failed.");
    }

    public async Task RunActionInDbContextAsync(Action<TDbContext> func,
                                                string errorMessage = null,
                                                bool saveChanges = true,
                                                bool useTransaction = true) =>
        await RunFuncInDbContextAsync(dbContext =>
            {
                func(dbContext);

                return (object)null;
            },
            errorMessage,
            saveChanges,
            useTransaction);

    public async Task<T> RunFuncInDbContextAsync<T>(Func<TDbContext, T> func,
                                                    string errorMessage = null,
                                                    bool saveChanges = true,
                                                    bool useTransaction = true) =>
        await RunWithinTransactionAsync(func, errorMessage, saveChanges, useTransaction);

    public T RunWithinTransaction<T>(Func<TDbContext, T> func,
                                     string errorMessage = null,
                                     bool saveChanges = true,
                                     bool useTransaction = true,
                                     IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    {
        using IServiceScope scope = _scopeProvider.CreateScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var id = Guid.NewGuid().ToString();

        try
        {
            return useTransaction && dbContext.Database.CurrentTransaction is null
                ? _transaction.Execute(dbContext,
                    () => Execute(dbContext, func, saveChanges, id),
                    id,
                    isolationLevel,
                    DelayOnTimeout)
                : Execute(dbContext, func, saveChanges, id);
        }
        catch (Exception e)
        {
            _logger.LogError($"[{id}]\t{errorMessage ?? ""} {e.Message}", e);

            throw;
        }
    }

    public async Task<T> RunWithinTransactionAsync<T>(Func<TDbContext, T> func,
                                                      string errorMessage = null,
                                                      bool saveChanges = true,
                                                      bool useTransaction = true,
                                                      IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    {
        using IServiceScope scope = _scopeProvider.CreateScope();
#if NETSTANDARD
        using TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
#else
        await using TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
#endif
        var id = Guid.NewGuid().ToString();

        try
        {
            return useTransaction && dbContext.Database.CurrentTransaction is null
                ? await _transaction.ExecuteAsync(dbContext,
                    () => Execute(dbContext, func, saveChanges, id),
                    id,
                    isolationLevel,
                    DelayOnTimeout)
                : Execute(dbContext, func, saveChanges, id);
        }
        catch (Exception e)
        {
            _logger.LogError($"[{id}]\t{errorMessage ?? ""} {e.Message}", e);

            throw;
        }
    }

    public async Task<T> RunWithinTransactionAsync<T>(Func<TDbContext, Task<T>> func,
                                                      string errorMessage = null,
                                                      bool saveChanges = true,
                                                      bool useTransaction = true,
                                                      IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    {
        using IServiceScope scope = _scopeProvider.CreateScope();
#if NETSTANDARD
        using TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
#else
        await using TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
#endif
        var id = Guid.NewGuid().ToString();

        try
        {
            return useTransaction && dbContext.Database.CurrentTransaction is null
                ? await _transaction.ExecuteAsync(dbContext,
                    () => ExecuteAsync(dbContext, func, saveChanges, id),
                    id,
                    isolationLevel,
                    DelayOnTimeout)
                : await ExecuteAsync(dbContext, func, saveChanges, id);
        }
        catch (Exception e)
        {
            _logger.LogError($"[{id}]\t{errorMessage ?? ""} {e.Message}", e);

            throw;
        }
    }

    protected virtual async Task AfterAppliedMigrationAsync() => await Task.CompletedTask;

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

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        bool result = await SQLConnectionHelper.CheckMasterDbConnectionAsync(_dbConfig);

        if (result && _dbConfig.RunMigrations)
            result = await RunMigrationsAsync();

        if (result && _dbConfig.GetMappings)
            await EnsureMappingSnapshotAsync();
    }

    protected async Task EnsureMappingSnapshotAsync() =>
        await RunActionInDbContextAsync(dbContext =>
        {
            var mappings = dbContext.Model.GetEntityTypes()
                .Select(t => new Mapping
                {
                    ClrTypeName = t.ClrType.FullName.Guard("ClrTypeName"),
                    TableName = t.GetTableName(),
                    Properties = t.GetMappedProperties()
                })
                .ToDictionary(mapping => mapping.ClrTypeName);

            Mappings = new ReadOnlyDictionary<string, Mapping>(mappings);
            TableMappings = new(Mappings.ToDictionary(x => x.Key, x => x.Value.TableName));
        });

    protected Result<Error> ValidateAndSaveChanges(TDbContext dbContext,
                                                   string id,
                                                   bool validateAllProperties = true,
                                                   bool acceptAllChangesOnSuccess = true)
    {
        Result<Error> result = dbContext.ValidateChangedEntities(null,
            validateAllProperties,
            OnValidationStart,
            OnFaultyEntity,
            OnValidationFail,
            OnValidationSuccess);

        if (result.IsFailure)
            return result;

        _logger.LogInformation($"[{id}]\tSaving changes to database");

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
                        entry.OriginalValues.SetValues(databaseValues);
                    else
                        switch (entry.State)
                        {
                            case EntityState.Deleted:
                                entry.State = EntityState.Detached;

                                break;
                            case EntityState.Modified:
                                entry.State = EntityState.Added;

                                break;
                            default:
                                throw;
                        }
                }

                retries--;
            }
        }

        _logger.LogInformation($"[{id}]\t{res} rows affected");

        return result;
    }

    protected async Task<Result<Error>> ValidateAndSaveChangesAsync(TDbContext dbContext,
                                                                    string id,
                                                                    bool validateAllProperties = true,
                                                                    bool acceptAllChangesOnSuccess = true)
    {
        Result<Error> result = dbContext.ValidateChangedEntities(id,
            validateAllProperties,
            OnValidationStart,
            OnFaultyEntity,
            OnValidationFail,
            OnValidationSuccess);

        if (result.IsFailure)
            return result;

        _logger.LogInformation($"[{id}]\tSaving changes to database");

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
                        entry.OriginalValues.SetValues(databaseValues);
                    else
                        switch (entry.State)
                        {
                            case EntityState.Deleted:
                                entry.State = EntityState.Detached;

                                break;
                            case EntityState.Modified:
                                entry.State = EntityState.Added;

                                break;
                            default:
                                throw;
                        }
                }

                retries--;
            }
        }

        _logger.LogInformation($"[{id}]\t{res} rows affected");

        return result;
    }

    protected async Task ShrinkDbAsync() => await RunTaskInDbContextAsync(ShrinkDbAsync, null, false, false);

    private static async Task ShrinkDbAsync(TDbContext context)
    {
        if (context.IsSqlite())
            await context.Database.ExecuteSqlRawAsync("VACUUM;");
    }

    private async Task<bool> DropAsync() =>
        await RunFuncInDbContextAsync(dbContext => dbContext.Database.EnsureDeleted(), null, false, false);

    private async Task<bool> HasNoPendingMigrationsAsync() =>
        await RunFuncInDbContextAsync(dbContext =>
            {
                var migs = dbContext.Database.GetMigrations().ToList();
                var aMigs = dbContext.Database.GetAppliedMigrations().ToList();
                var pMigs = dbContext.Database.GetPendingMigrations().ToList();

                return migs.UnorderedSequenceEqual(aMigs) && pMigs.Count == 0;
            },
            null,
            false,
            false);

    private T Execute<T>(TDbContext dbContext, Func<TDbContext, T> func, bool saveChanges, string id)
    {
        T res = func(dbContext);

        if (saveChanges)
            ValidateAndSaveChanges(dbContext, id);

        return res;
    }

    private async Task<T> ExecuteAsync<T>(TDbContext dbContext,
                                          Func<TDbContext, Task<T>> func,
                                          bool saveChanges,
                                          string id)
    {
        T res = await func(dbContext);

        if (saveChanges)
            await ValidateAndSaveChangesAsync(dbContext, id);

        return res;
    }
}