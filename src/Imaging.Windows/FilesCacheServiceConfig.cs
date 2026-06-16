using FEx.EFCore.Configuration;
using System;
using System.IO;

namespace FEx.Imaging.Windows;

public class FilesCacheServiceConfig : IFilesCacheServiceConfig
{
    public IDbServiceConfig DbServiceConfig { get; }
    public DirectoryInfo FilesCacheDir { get; }
    public string FilesCacheDirPath => FilesCacheDir.FullName;
    public bool UseHttpClientService { get; }
    public bool CacheAll { get; }
    public TimeSpan? CacheValidPeriod { get; }

    public FilesCacheServiceConfig(IDbServiceConfig dbServiceConfig,
                                   DirectoryInfo filesCacheDir,
                                   bool useHttpClientService = false,
                                   TimeSpan? cacheValidPeriod = null,
                                   bool cacheAll = true)
    {
        DbServiceConfig = dbServiceConfig;
        FilesCacheDir = filesCacheDir;
        FilesCacheDir.Create();
        UseHttpClientService = useHttpClientService;
        CacheValidPeriod = cacheValidPeriod;
        CacheAll = cacheAll;
    }
}