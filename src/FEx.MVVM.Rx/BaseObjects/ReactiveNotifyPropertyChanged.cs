using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.Agnostics.Abstractions.Interfaces;
using JetBrains.Annotations;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FEx.MVVM.Rx.BaseObjects;

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

            foreach (var propertyName in propertyNames)
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

    public bool SetProperty<TRet>(ref TRet backingField,
                                  TRet newValue,
                                  [CallerMemberName] string propertyName = null) =>
        SetProperty(ref backingField, newValue, null, propertyName);

    [NotifyPropertyChangedInvocator]
    public virtual bool SetProperty<TRet>(ref TRet backingField,
                                          TRet newValue,
                                          Action<TRet> onPropertyChanged,
                                          [CallerMemberName] string propertyName = null)
    {
        propertyName.Guard(nameof(propertyName));

        if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
            return false;

        OnPropertyChangingInternal(propertyName);
        var oldValue = backingField;
        backingField = newValue;
        OnPropertySet(oldValue, newValue, propertyName);
        OnPropertyChangedInternal(propertyName);

        onPropertyChanged?.Invoke(newValue);

        return true;
    }

    public virtual void OnPropertySet<T>(T oldValue, T newValue, string propertyName)
    {
    }

    private void OnPropertyChangingInternal(string propertyName)
    {
        ReactiveObject sender = this;

        EventDelegate();

        return;

        void EventDelegate() => sender.RaisePropertyChanging(propertyName);
    }

    private void OnPropertyChangedInternal(string propertyName)
    {
        ReactiveObject sender = this;

        EventDelegate();

        return;

        void EventDelegate() => sender.RaisePropertyChanged(propertyName);
    }
}