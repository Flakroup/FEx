using FEx.EFCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FEx.EFCore.Helpers;

public static class SqlLiteDbContextOptionsBuilderHelper
{
    public static void UseSqlite(DbContextOptionsBuilder options, IFExDbConfig config)
    {
        options.UseSqlite($"data source={config.SqliteDbFile.FullName}",
            sqliteDbContextOptionsBuilder => sqliteDbContextOptionsBuilder.CommandTimeout(config.CommandTimeout));
    }
}