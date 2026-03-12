using FEx.EFCore.Configuration;

namespace FEx.Imaging.Windows;

public interface IFilesCacheServiceConfig : IIndexEntryConfig
{
    IDbServiceConfig DbServiceConfig { get; }
    bool CacheAll { get; }
}
