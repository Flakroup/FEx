using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Asyncx.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.Sqlx.Abstractions;
using Microsoft.Data.Sql;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
#if NETFRAMEWORK
using FEx.Agnostics.Abstractions.Utilities;
using Microsoft.SqlServer.Management.Smo.Wmi;
#endif
#if NET
using System.Management;
using System.Runtime.Versioning;
#endif

namespace FEx.Sqlx;

public class SqlDbHelper : AsyncInitializable, ISqlDbHelper
{
    public SQLInstanceInfo? SQLInstanceInfo { get; private set; }

    public string? SQLInstance => SQLInstanceInfo?.SQLInstance;

    public SqlDbHelper()
    {
        BeginInitialization();
    }

    public static async Task<IList<SQLInstanceInfo>> GetSqlInstancesAsync()
    {
#if NETFRAMEWORK
        var instances32Task = AsyncStatics.ExecuteTaskOnThreadPoolAsync(() => GetLocalSqlInstancesFromWmiAsync());

        var instances64Task = PlatformInfoProvider.Is64BitOperatingSystem
            ? AsyncStatics.ExecuteTaskOnThreadPoolAsync(() =>
                GetLocalSqlInstancesFromWmiAsync(ProviderArchitecture.Use64bit))
            : Task.FromResult((IList<SQLInstanceInfo>)Enumerable.Empty<SQLInstanceInfo>().ToList());

        return [.. (await Task.WhenAll(instances32Task, instances64Task)).SelectMany(x => x)];
#else
        return await AsyncStatics.ExecuteTaskOnThreadPoolAsync(GetLocalSqlInstancesAsync);
#endif
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

    private static async Task<SQLInstanceInfo?> GetSQLInstanceInfoAsync(SQLInstanceInfo instance)
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

    // Used by GetSqlInstancesAsync in the non-NETFRAMEWORK build (#else branch). R# analyzes the net48
    // TFM, where that single call site is preprocessed out, so it incorrectly reports this as unused.
    // ReSharper disable once UnusedMember.Local
    private static async Task<IList<SQLInstanceInfo>> GetLocalSqlInstancesAsync()
    {
        try
        {
            var serverInstances = GetLocalSqlInstanceNames()
                .Where(x => x.IsNotNullOrEmptyString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(x => new SQLInstanceInfo(x))
                .ToList();

            return [.. (await serverInstances.WithWhenAllTasksAsync(GetSQLInstanceInfoAsync)).OfType<SQLInstanceInfo>()];
        }
        catch (Exception ex)
        {
            ex.HandleException(false);
        }

        return Enumerable.Empty<SQLInstanceInfo>().ToList();
    }

    private static IEnumerable<string> GetLocalSqlInstanceNames()
    {
#if NET
        if (OperatingSystem.IsWindows())
        {
            var wmiInstanceNames = GetLocalSqlInstanceNamesFromWmi();
            if (wmiInstanceNames.Count > 0)
                return wmiInstanceNames;
        }
#endif
        using var dataSources = SqlDataSourceEnumerator.Instance.GetDataSources();
        return [.. dataSources.Rows.OfType<DataRow>().Select(GetSqlInstanceName)];
    }

#if NET
    // Faithful net10 replacement for SMO ManagedComputer (whose WMI assemblies are absent from the
    // SqlManagementObjects net10.0 asset): query the same SQL Server WMI provider directly.
    [SupportedOSPlatform("windows")]
    private static IReadOnlyCollection<string> GetLocalSqlInstanceNamesFromWmi()
    {
        var instanceNames = new List<string>();
        var machineName = Environment.MachineName;

        foreach (var managementNamespace in GetSqlComputerManagementNamespaces())
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(managementNamespace,
                    "SELECT ServiceName FROM SqlService WHERE SQLServiceType = 1");
                using var results = searcher.Get();

                foreach (var service in results.OfType<ManagementObject>())
                {
                    using (service)
                    {
                        var instanceName =
                            ServiceNameToInstanceName(machineName, Convert.ToString(service["ServiceName"]));
                        if (instanceName.IsNotNullOrEmptyString())
                            instanceNames.Add(instanceName);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.HandleException(false);
            }
        }

        return instanceNames;
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<string> GetSqlComputerManagementNamespaces()
    {
        const string sqlServerRoot = @"root\Microsoft\SqlServer";
        var namespaces = new List<string>();

        try
        {
            using var searcher = new ManagementObjectSearcher(sqlServerRoot, "SELECT Name FROM __NAMESPACE");
            using var results = searcher.Get();

            foreach (var managementNamespace in results.OfType<ManagementObject>())
            {
                using (managementNamespace)
                {
                    var name = Convert.ToString(managementNamespace["Name"]);
                    if (name?.StartsWith("ComputerManagement", StringComparison.OrdinalIgnoreCase) == true)
                        namespaces.Add($@"{sqlServerRoot}\{name}");
                }
            }
        }
        catch (Exception ex)
        {
            ex.HandleException(false);
        }

        return namespaces;
    }

    private static string? ServiceNameToInstanceName(string machineName, string? serviceName)
    {
        if (!serviceName.IsNotNullOrEmptyString())
            return null;

        if (serviceName.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
            return machineName;

        const string namedInstancePrefix = "MSSQL$";
        return serviceName.StartsWith(namedInstancePrefix, StringComparison.OrdinalIgnoreCase)
            ? $@"{machineName}\{serviceName.Substring(namedInstancePrefix.Length)}"
            : null;
    }
#endif

#if NETFRAMEWORK
    private static async Task<IList<SQLInstanceInfo>> GetLocalSqlInstancesFromWmiAsync(
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

            return [.. (await serverInstances.WithWhenAllTasksAsync(GetSQLInstanceInfoAsync)).OfType<SQLInstanceInfo>()];
        }
        catch (Exception ex)
        {
            ex.HandleException(doNotReport: ex.Message.StartsWith("SQL Server WMI provider is not available on")
                                            && ex is SmoException);
        }

        return Enumerable.Empty<SQLInstanceInfo>().ToList();
    }
#endif

    private static string GetSqlInstanceName(DataRow row)
    {
        // ServerName is a non-nullable column of the SqlDataSourceEnumerator schema; Guard throws if absent.
        var serverName = Convert.ToString(row["ServerName"]).Guard("ServerName");
        var instanceName = Convert.ToString(row["InstanceName"]);

        return instanceName.IsNotNullOrEmptyString()
            ? $"{serverName}\\{instanceName}"
            : serverName;
    }

    private static async Task<SQLInstanceInfo?> GetLatestSqlInstanceAsync()
    {
        var sqlInstances = await GetSqlInstancesAsync();
        SQLInstanceInfo? sqlInstance = null;

        if (sqlInstances?.Count > 0)
            sqlInstance =
                sqlInstances.OrderByDescending(x => x.ProductVersion)
                    .FirstOrDefault(x => !x.IsLocalDB && HasValidLoginMode(x))
                ?? sqlInstances.OrderByDescending(x => x.ProductVersion).FirstOrDefault(HasValidLoginMode);

        return sqlInstance;
    }
}