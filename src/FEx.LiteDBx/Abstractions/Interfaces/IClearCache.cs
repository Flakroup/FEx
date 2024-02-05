using FEx.LiteDBx.Enums;

namespace FEx.LiteDBx.Abstractions.Interfaces;

public interface IClearCache
{
    public ClearCacheReason ClearCacheReason { get; }
    void ClearCache();
}