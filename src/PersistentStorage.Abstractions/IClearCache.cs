using FEx.PersistentStorage.Abstractions.Enums;

namespace FEx.PersistentStorage.Abstractions;

public interface IClearCache
{
    public ClearCacheReason ClearCacheReason { get; }
#if NETSTANDARD2_0
    ClearCachePriority ClearCachePriority { get; }
#else
    ClearCachePriority ClearCachePriority => ClearCachePriority.Default;
#endif
    void ClearCache();
}