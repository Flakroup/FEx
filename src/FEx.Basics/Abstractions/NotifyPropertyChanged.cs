using FEx.Basics.Abstractions.Interfaces;
using FEx.Extensions;
using FEx.Extensions.Base.Helpers;
using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FEx.Basics.Abstractions;

public abstract class NotifyPropertyChanged : IFExNotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertiesChanged(params string[] propertyNames)
    {
        if (!(propertyNames?.Length > 0))
            return;

        foreach (string propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }

    public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        FExBasics.EventDeliverer.DeliverEvent(EventDelegate, this);

        return;

        void EventDelegate() => NotifyChanged(propertyName);
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