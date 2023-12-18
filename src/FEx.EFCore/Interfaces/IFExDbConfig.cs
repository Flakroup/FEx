using System;
using System.IO;

namespace FEx.EFCore.Interfaces;

public interface IFExDbConfig
{
    string SqlInstance { get; }
    string SqlDbName { get; }
    string Username { get; }
    string Password { get; }
    FileInfo SqliteDbFile { get; }
    int? CommandTimeout { get; }
    int MaxRetryCount { get; }
    TimeSpan MaxRetryDelay { get; }
    int PoolSize { get; }
    int DelayOnTimeout { get; }
    bool RunMigrations { get; set; }
    bool GetMappings { get; set; }
    bool DropIfMigrationFailed { get; }
    bool UseSqlite { get; }
    bool TrustCertificate { get; }
}