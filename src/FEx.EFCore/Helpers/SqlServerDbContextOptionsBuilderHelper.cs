using FEx.EFCore.Interfaces;
using FEx.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;

namespace FEx.EFCore.Helpers;

public static class SqlServerDbContextOptionsBuilderHelper
{
    public static bool UseSqlServer(DbContextOptionsBuilder options, string sqlInstance, IFExDbConfig config)
    {
        sqlInstance.Guard(nameof(sqlInstance));
        try
        {
            string testConnectionString = GetConnectionString(sqlInstance, "master");
            bool canConnect = SQLConnectionHelper.CheckDbConnection(testConnectionString);

            if (canConnect)
            {
                string connectionString = GetConnectionString(sqlInstance, config.SqlDbName, config.PoolSize);
                options.UseSqlServer(connectionString, serverDbContextOptionsBuilder =>
                {
                    serverDbContextOptionsBuilder.CommandTimeout(config.CommandTimeout);
                    serverDbContextOptionsBuilder.EnableRetryOnFailure(config.MaxRetryCount, config.MaxRetryDelay,
                        null);
                });

                return true;
            }
        }
        catch (Exception ex)
        {
            ex.HandleException();
        }

        return false;
    }

    private static string GetConnectionString(string sqlInstance,
                                              string sqlDbName,
                                              int poolSize = 0,
                                              bool trustCertificate = true)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append($"Data Source={sqlInstance};")
            .Append($"Initial Catalog={sqlDbName};")
            .Append("Integrated Security=True;")
            .Append("Trusted_Connection=True;");
        if (poolSize > 0)
            stringBuilder.Append("Pooling=true;").Append($"Max Pool Size={poolSize};");

        if (trustCertificate)
            stringBuilder.Append("TrustServerCertificate=True;");

        return stringBuilder.ToString();
    }
}