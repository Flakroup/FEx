using System;
using System.IO;

namespace FEx.Imaging.Windows;

public interface IIndexEntryConfig
{
    DirectoryInfo FilesCacheDir { get; }
    string FilesCacheDirPath { get; }
    bool UseHttpClientService { get; }
    TimeSpan? CacheValidPeriod { get; }
}
