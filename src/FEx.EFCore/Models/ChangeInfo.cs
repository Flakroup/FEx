using DynamicData;
using System.Collections.Generic;

namespace FEx.EFCore.Models;

public class ChangeInfo<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    public Change<TValue, TKey> Change { get; }
    public TKey Key => Change.Key;
    public TValue Value => Change.Current;
    public ChangeReason Reason => Change.Reason;
    public bool ToDelete => Reason == ChangeReason.Remove && ExistsInDb;
    public bool ExistsInDb { get; set; }

    /// <summary>
    /// The values of <see cref="Value" /> when it was first sent to the database; a write-back after a rejection
    /// compares against it to detect an edit that landed in the meantime.
    /// </summary>
    internal List<object?>? Snapshot { get; set; }

    public ChangeInfo(Change<TValue, TKey> change)
    {
        Change = change;
    }
}