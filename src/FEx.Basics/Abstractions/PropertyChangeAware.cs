using FEx.Extensions.Base.Helpers;
using System;
using System.Runtime.CompilerServices;

namespace FEx.Basics.Abstractions;

public class PropertyChangeAware
{
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

    public virtual void OnPropertySet<T>(T oldValue, T newValue, string propertyName)
    {
    }

    public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
    }
}