using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.Basics.Abstractions.Interfaces;
using FEx.Extensions;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FEx.Basics.Abstractions;

public abstract class NotifyPropertyChanged : PropertyChangeAware, IFExNotifyPropertyChanged
{
    private IFExDispatcher _dispatcher;

    public event PropertyChangedEventHandler PropertyChanged;

    protected IFExDispatcher Dispatcher =>
        FExFoundation.HasBeenInitialized
#pragma warning disable CS0618 // Type or member is obsolete
            ? _dispatcher ??= FExFoundation.Dispatcher
            : null;
#pragma warning restore CS0618 // Type or member is obsolete

    public override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName is null
            || PropertyChanged is null)
            return;

        if (FExFoundation.HasBeenInitialized)
            Dispatcher.InvokeOnMainThread(EventDelegate, this);
        else
            EventDelegate();

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