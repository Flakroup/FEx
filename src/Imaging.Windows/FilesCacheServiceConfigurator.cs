using System;
using System.IO;

namespace FEx.Imaging.Windows;

public class FilesCacheServiceConfigurator : IFilesCacheServiceConfigurator
{
    // Non-nullable per IFilesCacheServiceConfigurator; these get-only members are populated externally and have no in-type initializer
    public DirectoryInfo ImageCache { get; } = null!;
    public string SqlDbName { get; } = null!;
    public string SqliteDbFileName { get; } = null!;
    public bool UseSqlite { get; }
    public bool UseHttpClientService { get; }
    public TimeSpan? CacheValidPeriod { get; }
    public bool CacheAll { get; } = true;
}