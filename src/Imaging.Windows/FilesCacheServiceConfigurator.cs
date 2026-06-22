using System;
using System.IO;

namespace FEx.Imaging.Windows;

public class FilesCacheServiceConfigurator : IFilesCacheServiceConfigurator
{
    public DirectoryInfo ImageCache { get; }
    public string SqlDbName { get; }
    public string SqliteDbFileName { get; }
    public bool UseSqlite { get; }
    public bool UseHttpClientService { get; }
    public TimeSpan? CacheValidPeriod { get; }
    public bool CacheAll { get; } = true;
}