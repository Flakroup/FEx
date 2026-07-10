using DynamicData;

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

    public ChangeInfo(Change<TValue, TKey> change)
    {
        Change = change;
    }
}