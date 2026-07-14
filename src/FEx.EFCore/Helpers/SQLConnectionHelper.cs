using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Core.Abstractions.Extensions;
using FEx.EFCore.Configuration;
using FEx.EFCore.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.EFCore.Helpers;

public static class SQLConnectionHelper
{
    public static Task<bool> CheckDbConnectionAsync(string connectionString) =>
        CheckDbConnectionAsync(connectionString, CancellationToken.None);

    public static async Task<bool> CheckDbConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
#if NETSTANDARD2_0
            using var connection = new SqlConnection(connectionString);
#else
            await using var connection = new SqlConnection(connectionString);
#endif
            await connection.OpenAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            ex.HandleException();

            return false;
        }
    }

    public static bool CheckDbConnection(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            return true;
        }
        catch (Exception ex)
        {
            ex.HandleException();

            return false;
        }
    }

    public static async Task<bool> CheckMasterDbConnectionAsync(IFExDbConfig config,
                                                                CancellationToken cancellationToken = default)
    {
        var testConfig = GetMasterDbConfig(config);
        var testConnectionString = GetConnectionString(testConfig);

        return await CheckDbConnectionAsync(testConnectionString, cancellationToken);
    }

    public static bool CheckMasterDbConnection(IFExDbConfig config)
    {
        var testConfig = GetMasterDbConfig(config);
        var testConnectionString = GetConnectionString(testConfig);

        return CheckDbConnection(testConnectionString);
    }

    public static string GetConnectionString(IFExDbConfig config)
    {
        var sB = new SqlConnectionStringBuilder
        {
            DataSource = config.SqlInstance,
            InitialCatalog = config.SqlDbName
        };

        if (config.Username is not null)
            sB.UserID = config.Username;

        if (config.Password is not null)
            sB.Password = config.Password;

        var host = config.SqlInstance.Guard(nameof(IFExDbConfig.SqlInstance)).Split('\\')[0];

        if ((host.CompareOrdinalIgnoreCase("localhost") || host.CompareOrdinalIgnoreCase(Environment.MachineName))
            && PlatformInfoProvider.IsWindows)
        {
            sB.IntegratedSecurity = true;
            sB.Add("Trusted_Connection", "True");
        }

        if (config.PoolSize > 1)
        {
            sB.Pooling = true;
            sB.MaxPoolSize = config.PoolSize;
        }

        sB.TrustServerCertificate = config.TrustCertificate;

        return sB.ToString();
    }

    private static FExDbConfig GetMasterDbConfig(IFExDbConfig config)
    {
        config.Guard(nameof(config));

        return new()
        {
            SqlInstance = config.SqlInstance.Guard(nameof(IFExDbConfig.SqlInstance)),
            SqlDbName = "master",
            Username = config.Username,
            Password = config.Password,
            CommandTimeout = config.CommandTimeout,
            MaxRetryCount = config.MaxRetryCount,
            MaxRetryDelay = config.MaxRetryDelay,
            PoolSize = 0,
            DelayOnTimeout = config.DelayOnTimeout,
            RunMigrations = false,
            GetMappings = false,
            DropIfMigrationFailed = false,
            TrustCertificate = config.TrustCertificate
        };
    }
}