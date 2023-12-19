using FEx.Basics;
using FEx.Extensions;
using FEx.Extensions.Collections.Lists;
using FEx.MVVM.Abstractions;
using JetBrains.Annotations;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FEx.Rx.BaseObjects;

public class ReactiveNotifyPropertyChanged : ReactiveObject, IFExNotifyPropertyChanged
{
    /// <summary>
    /// Use this method in your ReactiveObject classes when creating custom
    /// properties where raiseAndSetIfChanged doesn't suffice.
    /// </summary>
    /// <param name="propertyNames">The property names.</param>
    public void OnPropertiesChanged(params string[] propertyNames)
    {
        if (propertyNames.IsNotNullOrEmptyList())
        {
            ReactiveObject sender = this;

            foreach (string propertyName in propertyNames)
                sender.RaisePropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// Use this method in your ReactiveObject classes when creating custom
    /// properties where raiseAndSetIfChanged doesn't suffice.
    /// </summary>
    /// <param name="propertyName">The property names.</param>
    [NotifyPropertyChangedInvocator]
    public void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is not null)
            OnPropertyChangedInternal(propertyName);
    }

    [NotifyPropertyChangedInvocator]
    public virtual bool SetProperty<TRet>(ref TRet backingField,
                                          TRet newValue,
                                          Action<TRet> onPropertyChanged = null,
                                          [CallerMemberName] string propertyName = null)
    {
        propertyName.Guard(nameof(propertyName));

        if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
            return false;

        OnPropertyChangingInternal(propertyName);
        backingField = newValue;
        OnPropertyChangedInternal(propertyName);

        onPropertyChanged?.Invoke(newValue);

        return true;
    }

    private void OnPropertyChangingInternal(string propertyName)
    {
        ReactiveObject sender = this;

        void EventDelegate() => sender.RaisePropertyChanging(propertyName);
        FExBasics.EventDeliverer.DeliverEvent(EventDelegate, this);
    }

    private void OnPropertyChangedInternal(string propertyName)
    {
        ReactiveObject sender = this;

        void EventDelegate() => sender.RaisePropertyChanged(propertyName);
        FExBasics.EventDeliverer.DeliverEvent(EventDelegate, this);
    }
}