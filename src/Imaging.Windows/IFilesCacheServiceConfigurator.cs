using System;
using System.IO;

namespace FEx.Imaging.Windows;

public interface IFilesCacheServiceConfigurator
{
    DirectoryInfo ImageCache { get; }
    string SqlDbName { get; }
    string SqliteDbFileName { get; }
    bool UseSqlite { get; }
    bool UseHttpClientService { get; }
    TimeSpan? CacheValidPeriod { get; }
    bool CacheAll { get; }
}