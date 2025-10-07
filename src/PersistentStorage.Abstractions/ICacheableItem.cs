using LiteDB;

namespace FEx.LiteDBx.Abstractions.Interfaces;

public interface ICacheableItem
{
    ObjectId LocalStorageId { get; }
}