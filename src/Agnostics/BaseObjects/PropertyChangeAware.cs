using FEx.Agnostics.Abstractions.Utilities;
using System;
using System.Runtime.CompilerServices;

namespace FEx.Agnostics.BaseObjects;

public class PropertyChangeAware
{
    public virtual void OnPropertiesChanged(params string[] propertyNames)
    {
        if (!(propertyNames?.Length > 0))
            return;

        foreach (var propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }

    public virtual bool SetProperty<TRet>(ref TRet backingField,
                                          TRet newValue,
                                          Action<TRet> onPropertyChanged = null,
                                          [CallerMemberName] string propertyName = null)
    {
        if (EqualityHelper.IsEqual(ref backingField, newValue))
            return false;

        var oldValue = backingField;
        backingField = newValue;
        OnPropertySet(oldValue, newValue, propertyName);
        OnPropertyChanged(propertyName);
        onPropertyChanged?.Invoke(newValue);

        return true;
    }

    public virtual void OnPropertySet<T>(T oldValue, T newValue, string propertyName)
    {
    }

    public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
    }
}