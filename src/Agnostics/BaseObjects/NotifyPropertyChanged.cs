using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FEx.Agnostics.BaseObjects;

/// <summary>Base class that raises <see cref="PropertyChanged"/> whenever a property set through <see cref="PropertyChangeAware.SetProperty{TRet}"/> changes.</summary>
public abstract class NotifyPropertyChanged : PropertyChangeAware, IFExNotifyPropertyChanged
{
    /// <summary>Occurs when a property value has changed.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Runs the base change handling and then raises <see cref="PropertyChanged"/> for the property when there are subscribers.</summary>
    /// <param name="propertyName">The name of the changed property; nothing is raised when it is <see langword="null"/>.</param>
    public override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName is null
            || PropertyChanged is null)
            return;

        EventDelegate();

        return;

        void EventDelegate() => NotifyChanged(propertyName);
    }

    [NotifyPropertyChangedInvocator]
    private void NotifyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        PropertyChanged.HandlePropertyChanged(this, propertyName);
    }
}