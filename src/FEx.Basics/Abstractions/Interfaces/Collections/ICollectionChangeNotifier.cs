using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;

namespace FEx.Basics.Abstractions.Interfaces.Collections;

public interface ICollectionChangeNotifier : INotifyCollectionChanged, INotifyPropertyChanged
{
    TaskCompletionSource<bool> CollectionChangedTcs { get; }
    bool NotifyOnCreationContext { get; }
    TaskCompletionSource<bool> PropertyChangedTcs { get; }

    void OnCollectionChanged(object sender,
                             string[] propertyChangedArgs,
                             NotifyCollectionChangedAction changeAction,
                             object changedItem,
                             object oldItem,
                             int? index,
                             int? oldIndex);

    void SetNotifyOnCreationContext(bool notifyOnCreationContext);
    Task WaitForCollectionEventsAsync();
    void SetUseDispatcherContext(bool useDispatcherContext);
}