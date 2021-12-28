using System.Collections.Specialized;
using System.ComponentModel;

namespace FEx.Extensions.Collections.Concurrent.Notifiers;

public interface IChangeableCollection : INotifyCollectionChanged, INotifyPropertyChanged
{
    Task WaitForCollectionEventsAsync();
}