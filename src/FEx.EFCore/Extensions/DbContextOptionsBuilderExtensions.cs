using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FEx.EFCore.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static bool SetSqlInstanceConnection(this DbContextOptionsBuilder options,
                                                IFExDbConfig config,
                                                bool useSqlite = false)
    {
        var sqlInstanceFound = false;

        if (!useSqlite)
            sqlInstanceFound = options.UseSqlServer(config);
        else
            SqlLiteDbContextOptionsBuilderHelper.UseSqlite(options, config);

        if (Debugger.IsAttached)
            options.EnableSensitiveDataLogging();

        return sqlInstanceFound;
    }
}