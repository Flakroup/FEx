using FEx.Agnostics.Abstractions.Utilities;
using System;
using System.Runtime.CompilerServices;

namespace FEx.Agnostics.BaseObjects;

/// <summary>Base class providing a backing-field setter that detects value changes and exposes hooks for reacting to them.</summary>
public class PropertyChangeAware
{
    /// <summary>Calls <see cref="OnPropertyChanged"/> for each of the given property names.</summary>
    /// <param name="propertyNames">The names of the properties that changed; a null or empty array is ignored.</param>
    public virtual void OnPropertiesChanged(params string[] propertyNames)
    {
        if (!(propertyNames?.Length > 0))
            return;

        foreach (var propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }

    /// <summary>Assigns a new value to a backing field and, when it differs from the current one, runs the change hooks and callback.</summary>
    /// <typeparam name="TRet">The type of the property.</typeparam>
    /// <param name="backingField">The field that stores the property value.</param>
    /// <param name="newValue">The value to assign.</param>
    /// <param name="onPropertyChanged">An optional callback invoked with the new value after the change notifications.</param>
    /// <param name="propertyName">The name of the property; filled in automatically from the caller.</param>
    /// <returns><see langword="true"/> if the value changed and was assigned; <see langword="false"/> if it was already equal.</returns>
    public virtual bool SetProperty<TRet>(ref TRet backingField,
                                          TRet newValue,
                                          Action<TRet>? onPropertyChanged = null,
                                          [CallerMemberName] string? propertyName = null)
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

    /// <summary>Hook called after a property value was assigned and before change notification, with both the old and new values; does nothing by default.</summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="oldValue">The value before the assignment.</param>
    /// <param name="newValue">The value after the assignment.</param>
    /// <param name="propertyName">The name of the property that was set.</param>
    public virtual void OnPropertySet<T>(T oldValue, T newValue, string? propertyName)
    {
    }

    /// <summary>Hook called after a property has changed; does nothing by default.</summary>
    /// <param name="propertyName">The name of the changed property; filled in automatically from the caller.</param>
    public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
    }
}