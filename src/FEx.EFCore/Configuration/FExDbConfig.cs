using FEx.EFCore.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace FEx.EFCore.Configuration;

/// <remarks>
/// The password is kept out of every textual rendering of this record: <see cref="ToString" /> prints it
/// redacted, and both System.Text.Json and Newtonsoft.Json skip it when writing (it is still read, so a
/// config file or a JSON payload can supply it). Read it through <see cref="Password" /> only where the
/// connection is built.
/// </remarks>
public record FExDbConfig : IFExDbConfig //todo inherit SqlConnectionStringBuilder
{
    private const string Redacted = "***";

    public string? SqlInstance { get; set; }
    public string? SqlDbName { get; init; }
    public string? Username { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public string? Password { get; init; }
    public bool RunMigrations { get; set; } = true;
    public bool GetMappings { get; set; } = true;
    public bool DropIfMigrationFailed { get; init; }
    public int DelayOnTimeout { get; init; } = 1000;
    public int PoolSize { get; init; } = 100;
    public FileInfo? SqliteDbFile { get; init; }
    public bool UseSqlite { get; init; }
    public int MaxRetryCount { get; init; } = 10;
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(10);
    public bool TrustCertificate { get; init; } = true;
    public bool EnableSensitiveDataLogging { get; init; } = Debugger.IsAttached;

    public int? CommandTimeout { get; init; } = Debugger.IsAttached
        ? 5000
        : 30;

    /// <summary>Newtonsoft.Json convention: never write <see cref="Password" />; reading it is unaffected.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ShouldSerializePassword() => false;

    public static FExDbConfig ParseSqlConnectionString(string originalConnectionString)
    {
        var connectionString = new SqlConnectionStringBuilder(originalConnectionString);

        var config = new FExDbConfig
        {
            SqlInstance = connectionString.DataSource,
            SqlDbName = connectionString.InitialCatalog,
            Username = connectionString.UserID,
            Password = connectionString.Password,
            TrustCertificate = connectionString.TrustServerCertificate,
            PoolSize = connectionString.MaxPoolSize,
            DropIfMigrationFailed = Debugger.IsAttached
        };

        return config;
    }

    /// <summary>
    /// Replaces the compiler-generated member list behind the record's <c>ToString()</c>, which would print
    /// <see cref="Password" /> verbatim the moment anyone logs the config object. Derived records chain
    /// into this, so they inherit the redaction.
    /// </summary>
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append("SqlInstance = ").Append(SqlInstance)
               .Append(", SqlDbName = ").Append(SqlDbName)
               .Append(", Username = ").Append(Username)
               .Append(", Password = ").Append(Password is null ? null : Redacted)
               .Append(", RunMigrations = ").Append(RunMigrations)
               .Append(", GetMappings = ").Append(GetMappings)
               .Append(", DropIfMigrationFailed = ").Append(DropIfMigrationFailed)
               .Append(", DelayOnTimeout = ").Append(DelayOnTimeout)
               .Append(", PoolSize = ").Append(PoolSize)
               .Append(", SqliteDbFile = ").Append(SqliteDbFile)
               .Append(", UseSqlite = ").Append(UseSqlite)
               .Append(", MaxRetryCount = ").Append(MaxRetryCount)
               .Append(", MaxRetryDelay = ").Append(MaxRetryDelay)
               .Append(", TrustCertificate = ").Append(TrustCertificate)
               .Append(", EnableSensitiveDataLogging = ").Append(EnableSensitiveDataLogging)
               .Append(", CommandTimeout = ").Append(CommandTimeout);

        return true;
    }
}