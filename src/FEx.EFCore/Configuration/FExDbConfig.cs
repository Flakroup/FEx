using FEx.EFCore.Interfaces;
using System;
using System.Diagnostics;
using System.IO;

namespace FEx.EFCore.Configuration;

public record FExDbConfig : IFExDbConfig
{
    public bool RunMigrations { get; set; } = true;
    public bool GetMappings { get; set; } = true;
    public bool DropIfMigrationFailed { get; init; }
    public int DelayOnTimeout { get; init; } = 1000;
    public int PoolSize { get; init; } = 128;
    public string SqlDbName { get; init; }
    public FileInfo SqliteDbFile { get; init; }
    public bool UseSqlite { get; init; }

    public int? CommandTimeout { get; init; } = Debugger.IsAttached
        ? 5000
        : 30;

    public int MaxRetryCount { get; init; } = 10;
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(10);
}