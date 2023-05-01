using LiteDB;

namespace FEx.LiteDBx;

public interface ICacheableItem
{
    ObjectId LocalStorageId { get; }
}