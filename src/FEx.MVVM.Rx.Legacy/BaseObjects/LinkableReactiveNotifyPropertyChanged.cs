using FEx.MVVM.Abstractions;
using FEx.MVVM.Abstractions.Interfaces;
using System;

namespace FEx.MVVM.Rx.Legacy.BaseObjects;

public abstract class LinkableReactiveNotifyPropertyChanged : ReactiveNotifyPropertyChanged,
    ILinkableNotifyPropertyChanged
{
    private readonly LinkManager _linkManager;

    protected LinkableReactiveNotifyPropertyChanged()
    {
        _linkManager = new(GetType);
    }

    public override void OnPropertySet<T>(T oldValue, T newValue, string propertyName)
    {
        base.OnPropertySet(oldValue, newValue, propertyName);
        _linkManager.OnPropertySet(oldValue, newValue, propertyName);
    }

    public void AddLink(ILink link) => _linkManager.AddLink(link);

    public void Unlink(ILink link, bool resetProperty) => _linkManager.Unlink(link, resetProperty);

    public void Unlink(Guid linkId, string propertyName, Type propertyType, bool resetProperty) =>
        _linkManager.Unlink(linkId, propertyName, propertyType, resetProperty);
}