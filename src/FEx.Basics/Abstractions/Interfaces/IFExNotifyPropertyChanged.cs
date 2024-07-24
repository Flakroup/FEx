using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FEx.Basics.Abstractions.Interfaces;

public interface IFExNotifyPropertyChanged : INotifyPropertyChanged
{
    void OnPropertyChanged([CallerMemberName] string propertyName = null);
    void OnPropertiesChanged(params string[] propertyNames);

    bool SetProperty<TRet>(ref TRet backingField,
                           TRet newValue,
                           Action<TRet> onPropertyChanged = null,
                           [CallerMemberName] string propertyName = null);

    void OnPropertySet<T>(T oldValue, T newValue, string propertyName);
}