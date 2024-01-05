using FEx.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace FEx.Basics.Abstractions;

public class PropertyChangeAware
{
    protected virtual bool SetProperty<TRet>(ref TRet backingField,
                                             TRet newValue,
                                             Action<TRet> onPropertyChanged = null,
                                             [CallerMemberName] string propertyName = null) =>
        propertyName is not null
        && this.SetObjectProperty(ref backingField, newValue, onPropertyChanged is not null
            ? (_, _, v) => onPropertyChanged(v)
            : null, propertyName);
}