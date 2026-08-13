using System;

namespace FEx.MVVM.Abstractions.Events;

public class ProgressPropertyChangedEventArgs : EventArgs
{
    public virtual string ContainerId { get; }

    /// <summary>
    /// Indicates the name of the property that changed.
    /// </summary>
    public virtual string PropertyName { get; }

    /// <summary>
    /// Contains the value of the property that changed.
    /// </summary>
    public virtual object Value { get; }

    public ProgressPropertyChangedEventArgs(string containerId, string propertyName, object value)
    {
        ContainerId = containerId;
        PropertyName = propertyName;
        Value = value;
    }
}