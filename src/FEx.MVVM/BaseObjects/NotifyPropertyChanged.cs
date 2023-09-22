using FEx.Basics;
using FEx.Extensions;
using FEx.Extensions.Base.Helpers;
using FEx.MVVM.Abstractions;
using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FEx.MVVM.BaseObjects;

public class NotifyPropertyChanged : IFExNotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertiesChanged(params string[] propertyNames)
    {
        if (propertyNames?.Any() != true)
            return;

        foreach (string propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }

    public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        void EventDelegate() => NotifyChanged(propertyName);
        FExBasics.EventDeliverer.DeliverEvent(EventDelegate, this);
    }

    public virtual bool SetProperty<TRet>(ref TRet backingField,
                                          TRet newValue,
                                          Action<TRet> onPropertyChanged = null,
                                          [CallerMemberName] string propertyName = null)
    {
        if (EqualityHelper.IsEqual(ref backingField, newValue))
            return false;
        TRet oldValue = backingField;
        backingField = newValue;
        OnPropertySet(oldValue, newValue, propertyName);
        OnPropertyChanged(propertyName);
        onPropertyChanged?.Invoke(newValue);

        return true;
    }

    protected virtual void OnPropertySet<T>(T oldValue, T newValue, string propertyName)
    {
    }

    [NotifyPropertyChangedInvocator]
    private void NotifyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        PropertyChanged.HandlePropertyChanged(this, propertyName);
    }
}