using FEx.EFCore.Configuration;
using FEx.EFCore.Interfaces;
using FEx.Extensions;
using FEx.Fundamentals.Utilities.OS;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

namespace FEx.EFCore.Helpers;

public static class SQLConnectionHelper
{
    public static async Task<bool> CheckDbConnectionAsync(string connectionString)
    {
        try
        {
#if NETSTANDARD
            using var connection = new SqlConnection(connectionString);
#else
            await using var connection = new SqlConnection(connectionString);
#endif
            await connection.OpenAsync();

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

    public static async Task<bool> CheckMasterDbConnectionAsync(IFExDbConfig config)
    {
        FExDbConfig testConfig = GetMasterDbConfig(config);
        string testConnectionString = GetConnectionString(testConfig);

        return await CheckDbConnectionAsync(testConnectionString);
    }

    public static bool CheckMasterDbConnection(IFExDbConfig config)
    {
        FExDbConfig testConfig = GetMasterDbConfig(config);
        string testConnectionString = GetConnectionString(testConfig);

        return CheckDbConnection(testConnectionString);
    }

    private static FExDbConfig GetMasterDbConfig(IFExDbConfig config)
    {
        config.Guard(nameof(config));

        return new FExDbConfig
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

    public static string GetConnectionString(IFExDbConfig config)
    {
        var sB = new SqlConnectionStringBuilder
        {
            DataSource = config.SqlInstance,
            InitialCatalog = config.SqlDbName,
            UserID = config.Username,
            Password = config.Password
        };

        if (config.SqlInstance == "localhost"
            && OSVersionInfo.IsWin)
        {
            sB.IntegratedSecurity = true;
            sB.Add("Trusted_Connection", "True");
        }

        if (config.PoolSize > 0)
        {
            sB.Pooling = true;
            sB.MaxPoolSize = config.PoolSize;
        }

        sB.TrustServerCertificate = config.TrustCertificate;

        return sB.ToString();
    }
}