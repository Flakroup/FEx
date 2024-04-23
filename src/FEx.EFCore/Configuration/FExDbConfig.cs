using FEx.EFCore.Interfaces;
using System;
using System.Diagnostics;
using System.IO;

namespace FEx.EFCore.Configuration;

public record FExDbConfig : IFExDbConfig //todo inherit SqlConnectionStringBuilder
{
    public string SqlInstance { get; set; }
    public string SqlDbName { get; init; }
    public string Username { get; init; }
    public string Password { get; init; }
    public bool RunMigrations { get; set; } = true;
    public bool GetMappings { get; set; } = true;
    public bool DropIfMigrationFailed { get; init; }
    public int DelayOnTimeout { get; init; } = 1000;
    public int PoolSize { get; init; } = 128;
    public FileInfo SqliteDbFile { get; init; }
    public bool UseSqlite { get; init; }
    public int MaxRetryCount { get; init; } = 10;
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(10);
    public bool TrustCertificate { get; init; } = true;
    public bool EnableSensitiveDataLogging { get; init; } = Debugger.IsAttached;

    public int? CommandTimeout { get; init; } = Debugger.IsAttached
        ? 5000
        : 30;
}