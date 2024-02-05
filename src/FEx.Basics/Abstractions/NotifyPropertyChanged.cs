using FEx.Basics.Abstractions.Interfaces;
using FEx.Extensions;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FEx.Basics.Abstractions;

public abstract class NotifyPropertyChanged : PropertyChangeAware, IFExNotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertiesChanged(params string[] propertyNames)
    {
        if (!(propertyNames?.Length > 0))
            return;

        foreach (string propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }

    public override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName is null
            || PropertyChanged is null)
            return;

        FExBasics.EventDeliverer.DeliverEvent(EventDelegate, this);

        return;

        void EventDelegate() => NotifyChanged(propertyName);
    }

    [NotifyPropertyChangedInvocator]
    private void NotifyChanged([CallerMemberName] string propertyName = null)
    {
        if (propertyName is null
            || PropertyChanged is null)
            return;

        PropertyChanged.HandlePropertyChanged(this, propertyName);
    }
}