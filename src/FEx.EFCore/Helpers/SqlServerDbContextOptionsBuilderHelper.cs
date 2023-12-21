using FEx.EFCore.Interfaces;
using FEx.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace FEx.EFCore.Helpers;

public static class SqlServerDbContextOptionsBuilderHelper
{
    public static bool UseSqlServer(this DbContextOptionsBuilder options,
                                    IFExDbConfig config,
                                    Action<SqlServerDbContextOptionsBuilder> configure = null)
    {
        config.SqlInstance.Guard(nameof(IFExDbConfig.SqlInstance));

        try
        {
            bool canConnect = SQLConnectionHelper.CheckMasterDbConnection(config);

            if (canConnect)
            {
                string connectionString = SQLConnectionHelper.GetConnectionString(config);

                options.UseSqlServer(connectionString, serverDbContextOptionsBuilder =>
                {
                    serverDbContextOptionsBuilder.CommandTimeout(config.CommandTimeout);

                    serverDbContextOptionsBuilder.EnableRetryOnFailure(config.MaxRetryCount, config.MaxRetryDelay,
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