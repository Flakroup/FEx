using FEx.Agnostics.Abstractions.Extensions;
using FEx.Core.Abstractions.Extensions;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace FEx.EFCore.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static bool ConfigureDbContext(this DbContextOptionsBuilder options,
                                          IFExDbConfig config,
                                          ISqlDbHelper sqlDbHelper)
    {
        if (!sqlDbHelper.IsInitialized)
            throw new InvalidOperationException();

        config.SqlInstance = sqlDbHelper.SQLInstance;

        var sqlInstanceFound = true;

        if (config.UseSqlite)
            options.UseSqlite(config);
        else
            sqlInstanceFound = options.UseSqlServer(config);

        if (config.EnableSensitiveDataLogging)
            options.EnableSensitiveDataLogging();

        return sqlInstanceFound;
    }

    public static void UseSqlite(this DbContextOptionsBuilder options, IFExDbConfig config) =>
        options.UseSqlite($"data source={config.SqliteDbFile.FullName}",
            sqliteDbContextOptionsBuilder => sqliteDbContextOptionsBuilder.CommandTimeout(config.CommandTimeout));

    public static bool UseSqlServer(this DbContextOptionsBuilder options,
                                    IFExDbConfig config,
                                    Action<SqlServerDbContextOptionsBuilder> configure = null)
    {
        config.SqlInstance.Guard(nameof(IFExDbConfig.SqlInstance));

        try
        {
            var canConnect = SQLConnectionHelper.CheckMasterDbConnection(config);

            if (canConnect)
            {
                var connectionString = SQLConnectionHelper.GetConnectionString(config);

                options.UseSqlServer(connectionString,
                    serverDbContextOptionsBuilder =>
                    {
                        serverDbContextOptionsBuilder.CommandTimeout(config.CommandTimeout);

                        serverDbContextOptionsBuilder.EnableRetryOnFailure(config.MaxRetryCount,
                            config.MaxRetryDelay,
                            null);

                        configure?.Invoke(serverDbContextOptionsBuilder);
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
}