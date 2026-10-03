using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>An <see cref="System.ComponentModel.INotifyPropertyChanged" /> object that exposes helpers to raise change notifications and set backing fields.</summary>
public interface IFExNotifyPropertyChanged : INotifyPropertyChanged
{
    /// <summary>Raises the <c>PropertyChanged</c> event for one property.</summary>
    /// <param name="propertyName">The property name; the caller member name by default.</param>
    void OnPropertyChanged([CallerMemberName] string? propertyName = null);
    /// <summary>Raises the <c>PropertyChanged</c> event for several properties.</summary>
    /// <param name="propertyNames">The names of the changed properties.</param>
    void OnPropertiesChanged(params string[] propertyNames);

    /// <summary>Sets a backing field and raises change notifications when the value changed.</summary>
    /// <typeparam name="TRet">The property type.</typeparam>
    /// <param name="backingField">The backing field to update.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="onPropertyChanged">Called with the new value after a change.</param>
    /// <param name="propertyName">The property name; the caller member name by default.</param>
    /// <returns><c>true</c> if the value changed.</returns>
    bool SetProperty<TRet>(ref TRet backingField,
                           TRet newValue,
                           Action<TRet>? onPropertyChanged = null,
                           [CallerMemberName] string? propertyName = null);

    /// <summary>Called after a property value was set.</summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="oldValue">The previous value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="propertyName">The name of the property that was set.</param>
    void OnPropertySet<T>(T oldValue, T newValue, string propertyName);
}