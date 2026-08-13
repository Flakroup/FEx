using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Helpers;
using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Configuration;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using FEx.Sqlx.Abstractions;
using Microsoft.EntityFrameworkCore;
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

        try
        {
            await InitializeAsync();
        }
        catch
        {
            //ignored
        }
    }

    protected override async Task OnInitializeAsync()
    {
        if (!_dbHelper.IsInitialized)
            await JoinableAsyncHelper.AwaitWithoutDeadlockAsync(_dbHelper.InitializeAsync);

        _dbConfig.SqlInstance = _dbHelper.SQLInstance.Guard(nameof(_dbHelper.SQLInstance));

        await base.OnInitializeAsync();
    }
}