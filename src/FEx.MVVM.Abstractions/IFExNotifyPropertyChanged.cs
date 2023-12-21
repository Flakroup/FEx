using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FEx.MVVM.Abstractions;

public interface IFExNotifyPropertyChanged : INotifyPropertyChanged
{
    void OnPropertyChanged([CallerMemberName] string propertyName = null);
    void OnPropertiesChanged(params string[] propertyNames);

    bool SetProperty<TRet>(ref TRet backingField,
                           TRet newValue,
                           Action<TRet> onPropertyChanged = null,
                           [CallerMemberName] string propertyName = null);
}