using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Helpers;
using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Configuration;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using FEx.Sqlx.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace FEx.EFCore.Services;

/// <summary>
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
/// <remarks>Requires <c>Initialize();</c> call in .ctor</remarks>
public abstract class EFCoreDatabaseBackedService<TDbContext> : BulkDbServiceBase<TDbContext>,
    IEFCoreDatabaseBackedService<TDbContext> where TDbContext : DbContext
{
    private readonly ISqlDbHelper _dbHelper;
    public string? DbKey { get; protected set; }

    protected EFCoreDatabaseBackedService(IScopeProvider scopeProvider,
                                          IDbServiceConfig config,
                                          ResilientTransaction resilientTransaction,
                                          ISqlDbHelper dbHelper,
                                          IAsyncInitializable[] dependencies)
        : base(scopeProvider, config.BulkDbConfig, resilientTransaction, config.DbConfig, dependencies)
    {
        _dbHelper = dbHelper;
    }

    protected virtual async Task EnsureIsInitializedAsync()
    {
        if (IsInitialized)
            return;

        // InitializeAsync rethrows a failure kept from an earlier attempt (e.g. a background BeginInitialization);
        // start over so the current outcome reaches the caller.
        if (_initializationTask is { IsFaulted: true } or { IsCanceled: true })
            await ResetAsync();

        await InitializeAsync();
    }

    // The SQL instance is resolved before the dependencies are initialized, as it was before the template method;
    // sealed so a further subclass cannot skip it.
    protected sealed override async Task OnBeforeDependenciesInitializationAsync()
    {
        if (!_dbHelper.IsInitialized)
            await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(_dbHelper.InitializeAsync);

        _dbConfig.SqlInstance = _dbHelper.SQLInstance.Guard(nameof(_dbHelper.SQLInstance));
    }
}