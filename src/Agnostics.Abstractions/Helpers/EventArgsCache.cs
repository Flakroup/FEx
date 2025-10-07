using System.Collections.Specialized;
using System.ComponentModel;

namespace FEx.Basics.Utilities.Collections;

public static class EventArgsCache
{
    public static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");

    public static readonly PropertyChangedEventArgs IsEmptyPropertyChanged = new("IsEmpty");

    public static readonly PropertyChangedEventArgs IndexerPropertyChanged = new("Item[]");

    public static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged =
        new(NotifyCollectionChangedAction.Reset);
}