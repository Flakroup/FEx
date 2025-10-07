using FEx.LiteDBx.Abstractions.Interfaces;
using LiteDB;
using System;

namespace FEx.LiteDBx.Abstractions;

public abstract class CacheableItem : ICacheableItem
{
    [BsonId]
    public ObjectId LocalStorageId { get; set; }

    public DateTime TimeStamp { get; set; }

    protected CacheableItem()
    {
        TimeStamp = DateTime.Now;
    }
}