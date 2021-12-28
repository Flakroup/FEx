using System.ComponentModel;

namespace FEx.Extensions.Collections.Concurrent;

internal static class ObservableHashSetSingletons
{
    public static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");

    public static readonly PropertyChangingEventArgs CountPropertyChanging = new("Count");

    public static readonly object[] NoItems = Array.Empty<object>();
}