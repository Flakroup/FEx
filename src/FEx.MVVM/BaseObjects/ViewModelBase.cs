using JetBrains.Annotations;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FEx.MVVM.BaseObjects;

public class ViewModelBase : ReactiveObject
{
    [NotifyPropertyChangedInvocator]
    protected virtual bool SetProperty<TRet>(ref TRet backingField,
                                             TRet newValue,
                                             Action<TRet> onPropertyChanged = null,
                                             [CallerMemberName] string propertyName = null)
    {
        if (!EqualityComparer<TRet>.Default.Equals(backingField, newValue))
        {
            backingField = newValue;
            ((IReactiveObject)this).RaisePropertyChanged(new(propertyName));
            onPropertyChanged?.Invoke(newValue);
            return true;
        }

        return false;
    }
}