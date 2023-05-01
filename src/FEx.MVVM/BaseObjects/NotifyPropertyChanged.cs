using FEx.Abstractions;
using FEx.Fundamentals;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FEx.MVVM.BaseObjects;

public class NotifyPropertyChanged : INotifyPropertyChanged
{
    private readonly IFExDispatcher _dispatcher;

    public NotifyPropertyChanged()
    {
        _dispatcher = Foundation.Dispatcher;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static bool SetPropertyStatic<T>(ref T backingField, T newValue, Action<T> onPropertyChanged = null)
    {
        if (!EqualityComparer<T>.Default.Equals(backingField, newValue))
        {
            backingField = newValue;
            onPropertyChanged?.Invoke(newValue);
            return true;
        }

        return false;
    }

    [NotifyPropertyChangedInvocator]
    public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is not null
            && PropertyChanged is not null)
            _dispatcher.SendInThisOrMainThreadContext(() => PropertyChanged(this, new(propertyName)));
    }

    public void OnPropertiesChanged(params string[] propertyNames)
    {
        if (propertyNames?.Any() != true)
            return;

        foreach (string propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }

    [NotifyPropertyChangedInvocator]
    protected virtual bool SetProperty<TRet>(ref TRet backingField, TRet newValue, Action<TRet> onPropertyChanged = null, [CallerMemberName] string propertyName = null)
    {
        return SetPropertyStatic(ref backingField, newValue, x => OnPropertySet(x, propertyName, onPropertyChanged));
    }

    private void OnPropertySet<TRet>(TRet newValue, string propertyName, Action<TRet> onPropertyChanged)
    {
        OnPropertyChanged(propertyName);
        onPropertyChanged?.Invoke(newValue);
    }
}