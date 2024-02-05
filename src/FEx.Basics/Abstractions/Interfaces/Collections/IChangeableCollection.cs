using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;

namespace FEx.Basics.Abstractions.Interfaces.Collections;

public interface IChangeableCollection : INotifyCollectionChanged, INotifyPropertyChanged
{
    Task WaitForCollectionEventsAsync();
}