using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FEx.Agnostics.BaseObjects;

public abstract class NotifyPropertyChanged : PropertyChangeAware, IFExNotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

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