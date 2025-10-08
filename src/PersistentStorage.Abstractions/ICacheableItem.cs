using LiteDB;

namespace FEx.PersistentStorage.Abstractions;

public interface ICacheableItem
{
    ObjectId LocalStorageId { get; }
}