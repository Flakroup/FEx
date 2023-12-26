using LiteDB;

namespace FEx.LiteDbx.Abstractions.Interfaces;

public interface ICacheableItem
{
    ObjectId LocalStorageId { get; }
}