using FEx.MVVM.Abstractions.Events;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IProgressNotifyPropertyChanged
{
    event ProgressPropertyChangedEventHandler ProgressPropertyChanged;
}