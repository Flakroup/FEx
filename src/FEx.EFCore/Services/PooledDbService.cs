using FEx.Agnostics.Abstractions.Collections;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Flow;
using FEx.Asyncx.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Extensions;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
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

    // Both are populated in EnsureMappingSnapshotAsync during async initialization, before any consumer access.
    public IReadOnlyDictionary<string, Mapping> Mappings { get; private set; } = null!;
    public Map<string, string> TableMappings { get; private set; } = null!;

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
                                              string? errorMessage = null,
                                              bool saveChanges = true,
                                              bool useTransaction = true) =>
        await RunTaskInDbContextAsync(func.WrapTask, errorMessage, saveChanges, useTransaction);

    public async Task<T> RunTaskInDbContextAsync<T>(Func<TDbContext, Task<T>> func,
                                                    string? errorMessage = null,
                                                    bool saveChanges = true,
                                                    bool useTransaction = true) =>
        await RunWithinTransactionAsync(func, errorMessage, saveChanges, useTransaction);

    public async Task<T> RunTaskInDbContextAsync<T>(Func<TDbContext, Func<Task<T>>> func,
                                                    string? errorMessage = null,
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

    /// <summary>
    /// Migrate does the same job that EnsureCreated, but also adds table with migrations history
    /// </summary>
    /// <returns></returns>
    public async Task MigrateAsync()
    {
        var hasNoPendingMigrations = await HasNoPendingMigrationsAsync();

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
                                                string? errorMessage,
                                                bool saveChanges,
                                                bool useTransaction) =>
        await RunFuncInDbContextAsync(dbContext =>
            {
                func(dbContext);

                return (object?)null;
            },
            errorMessage,
            saveChanges,
            useTransaction);

    public async Task<T> RunFuncInDbContextAsync<T>(Func<TDbContext, T> func,
                                                    string? errorMessage = null,
                                                    bool saveChanges = true,
                                                    bool useTransaction = true) =>
        await RunWithinTransactionAsync(func, errorMessage, saveChanges, useTransaction);

    public T RunWithinTransaction<T>(Func<TDbContext, T> func,
                                     string? errorMessage,
                                     bool saveChanges,
                                     bool useTransaction,
                                     IsolationLevel isolationLevel)
    {
        using var scope = _scopeProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
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
            _logger.Error(e, $"[{id}]\t{errorMessage ?? ""} {e.Message}");

            throw;
        }
    }

    public async Task<T> RunWithinTransactionAsync<T>(Func<TDbContext, T> func,
                                                      string? errorMessage = null,
                                                      bool saveChanges = true,
                                                      bool useTransaction = true,
                                                      IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    {
        using var scope = _scopeProvider.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
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
            _logger.Error(e, $"[{id}]\t{errorMessage ?? ""} {e.Message}");

            throw;
        }
    }

    public async Task<T> RunWithinTransactionAsync<T>(Func<TDbContext, Task<T>> func,
                                                      string? errorMessage = null,
                                                      bool saveChanges = true,
                                                      bool useTransaction = true,
                                                      IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    {
        using var scope = _scopeProvider.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
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
            _logger.Error(e, $"[{id}]\t{errorMessage ?? ""} {e.Message}");

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

        var result = await SQLConnectionHelper.CheckMasterDbConnectionAsync(_dbConfig);

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
                        TableName = t.GetTableName().Guard(nameof(Mapping.TableName)),
                        Properties = t.GetMappedProperties()
                    })
                    .ToDictionary(mapping => mapping.ClrTypeName);

                Mappings = new ReadOnlyDictionary<string, Mapping>(mappings);
                TableMappings = new(Mappings.ToDictionary(x => x.Key, x => x.Value.TableName));
            },
            null,
            true,
            true);

    /// <inheritdoc cref="ValidateAndSaveChanges(TDbContext, string, bool, bool)" />
    protected Result<Error> ValidateAndSaveChanges(TDbContext dbContext, string id) =>
        ValidateAndSaveChanges(dbContext, id, true, true);

    /// <summary>
    /// Validates the changed entities and, if valid, saves them once.
    /// </summary>
    /// <remarks>
    /// An optimistic-concurrency conflict is never resolved here: if another writer changed or deleted a row
    /// this context modifies, <see cref="DbUpdateConcurrencyException" /> propagates to the caller, exactly like
    /// <see cref="DbContextExtensions.ValidateAndSaveChangesAsync{TDbContext}(TDbContext)" />.
    /// </remarks>
    /// <exception cref="DbUpdateConcurrencyException">Another writer changed or deleted an affected row.</exception>
    protected Result<Error> ValidateAndSaveChanges(TDbContext dbContext,
                                                   string id,
                                                   bool validateAllProperties,
                                                   bool acceptAllChangesOnSuccess)
    {
        var result = Validate(dbContext, id, validateAllProperties);

        if (result.IsFailure)
            return result;

        _logger.Information($"[{id}]\tSaving changes to database");
        var res = dbContext.SaveChanges(acceptAllChangesOnSuccess);
        _logger.Information($"[{id}]\t{res} rows affected");

        return result;
    }

    /// <inheritdoc cref="ValidateAndSaveChanges(TDbContext, string, bool, bool)" />
    protected async Task<Result<Error>> ValidateAndSaveChangesAsync(TDbContext dbContext,
                                                                    string id,
                                                                    bool validateAllProperties = true,
                                                                    bool acceptAllChangesOnSuccess = true)
    {
        var result = Validate(dbContext, id, validateAllProperties);

        if (result.IsFailure)
            return result;

        _logger.Information($"[{id}]\tSaving changes to database");
        var res = await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess);
        _logger.Information($"[{id}]\t{res} rows affected");

        return result;
    }

    private Result<Error> Validate(TDbContext dbContext, string id, bool validateAllProperties) =>
        dbContext.ValidateChangedEntities(id,
            validateAllProperties,
            OnValidationStart,
            OnFaultyEntity,
            OnValidationFail,
            OnValidationSuccess);

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
        var res = func(dbContext);

        if (saveChanges)
            ValidateAndSaveChanges(dbContext, id);

        return res;
    }

    private async Task<T> ExecuteAsync<T>(TDbContext dbContext,
                                          Func<TDbContext, Task<T>> func,
                                          bool saveChanges,
                                          string id)
    {
        var res = await func(dbContext);

        if (saveChanges)
            await ValidateAndSaveChangesAsync(dbContext, id);

        return res;
    }
}