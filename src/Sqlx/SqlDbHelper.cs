using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Asyncx.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Abstractions.Utilities;
using FEx.Sqlx.Abstractions;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Wmi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.Sqlx;

public class SqlDbHelper : AsyncInitializable, ISqlDbHelper
{
    public SQLInstanceInfo SQLInstanceInfo { get; private set; }

    public string SQLInstance => SQLInstanceInfo?.SQLInstance;

    public SqlDbHelper()
    {
        BeginInitialization();
    }

    public static async Task<IList<SQLInstanceInfo>> GetSqlInstancesAsync()
    {
        Task<IList<SQLInstanceInfo>> instances32Task =
            AsyncStatics.ExecuteTaskOnThreadPoolAsync(() => GetLocalSqlInstancesAsync());

        Task<IList<SQLInstanceInfo>> instances64Task = PlatformInfoProvider.Is64BitOperatingSystem
            ? AsyncStatics.ExecuteTaskOnThreadPoolAsync(() =>
                GetLocalSqlInstancesAsync(ProviderArchitecture.Use64bit))
            : Task.FromResult((IList<SQLInstanceInfo>)Enumerable.Empty<SQLInstanceInfo>().ToList());

        return (await Task.WhenAll(instances32Task, instances64Task)).SelectMany(x => x).ToList();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        _logger?.Information("DB Lookup");

        try
        {
            SQLInstanceInfo = await GetLatestSqlInstanceAsync();
        }
        finally
        {
            _logger?.Information("DB Lookup completed");
        }
    }

    private static async Task<SQLInstanceInfo> GetSQLInstanceInfoAsync(SQLInstanceInfo instance)
    {
        try
        {
            if (await instance.LoadInfoAsync())
                return instance;
        }
        catch
        {
            //ignored
        }

        return null;
    }

    private static bool HasValidLoginMode(SQLInstanceInfo sqlInstanceInfo) =>
        sqlInstanceInfo?.LoginMode is ServerLoginMode.Integrated or ServerLoginMode.Mixed;

    private static async Task<IList<SQLInstanceInfo>> GetLocalSqlInstancesAsync(
        ProviderArchitecture arch = ProviderArchitecture.Use32bit)
    {
        try
        {
            var comp = new ManagedComputer
            {
                ConnectionSettings =
                {
                    ProviderArchitecture = arch
                }
            };

            var serverInstances = comp.ServerInstances.OfType<ServerInstance>()
                .Select(serverInstance => new SQLInstanceInfo(serverInstance, comp))
                .ToList();

            return (await serverInstances.WithWhenAllTasksAsync(GetSQLInstanceInfoAsync)).Where(x => x is not null)
                .ToArray();
        }
        catch (Exception ex)
        {
            ex.HandleException(doNotReport: ex.Message.StartsWith("SQL Server WMI provider is not available on")
                                            && ex is SmoException);
        }

        return Enumerable.Empty<SQLInstanceInfo>().ToList();
    }

    private static async Task<SQLInstanceInfo> GetLatestSqlInstanceAsync()
    {
        IList<SQLInstanceInfo> sqlInstances = await GetSqlInstancesAsync();
        SQLInstanceInfo sqlInstance = null;

        if (sqlInstances?.Count > 0)
            sqlInstance =
                sqlInstances.OrderByDescending(x => x.ProductVersion)
                    .FirstOrDefault(x => !x.IsLocalDB && HasValidLoginMode(x))
                ?? sqlInstances.OrderByDescending(x => x.ProductVersion).FirstOrDefault(HasValidLoginMode);

        return sqlInstance;
    }
}
