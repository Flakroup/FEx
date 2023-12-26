using FEx.LiteDbx.Enums;

namespace FEx.LiteDbx.Abstractions.Interfaces;

public interface IClearCache
{
    public ClearCacheReason ClearCacheReason { get; }
    void ClearCache();
}