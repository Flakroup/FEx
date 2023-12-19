using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FEx.EFCore.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static bool SetSqlInstanceConnection(this DbContextOptionsBuilder options, IFExDbConfig config)
    {
        var sqlInstanceFound = true;

        if (config.UseSqlite)
            options.UseSqlite(config);
        else
            sqlInstanceFound = options.UseSqlServer(config);

        if (config.EnableSensitiveDataLogging)
            options.EnableSensitiveDataLogging();

        return sqlInstanceFound;
    }
}