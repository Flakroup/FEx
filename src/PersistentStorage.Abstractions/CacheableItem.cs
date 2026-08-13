using LiteDB;
using System;

namespace FEx.PersistentStorage.Abstractions;

public abstract class CacheableItem : ICacheableItem
{
    [BsonId]
    public ObjectId LocalStorageId { get; set; } = ObjectId.NewObjectId();

    public DateTime TimeStamp { get; set; }

    protected CacheableItem()
    {
        TimeStamp = DateTime.Now;
    }
}