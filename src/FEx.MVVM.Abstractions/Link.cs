using FEx.Agnostics.Abstractions.Extensions;
using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace FEx.MVVM.Abstractions;

public class Link : ILink
{
    private readonly Action<ILink, object, object> _onPropertyChange;
    private readonly ILinkableNotifyPropertyChanged _sender;
    private readonly Func<ILinkableNotifyPropertyChanged, object> _getPropertyValue;
    private readonly ILink _parentLink;
    private ConcurrentDictionary<Guid, ILink> _childLinks;
    private object _lastValue;
    private bool _isUnlinked;

    public Type PropertyType { get; }
    public string PropertyName { get; }
    public Guid Id { get; }

    public object DefaultValue { get; }

    public Link(Type propertyType,
                ILinkableNotifyPropertyChanged sender,
                string propertyName,
                Func<ILinkableNotifyPropertyChanged, object> getPropertyValue,
                Action<ILink, object, object> onPropertyChange,
                object defaultValue)
        : this(propertyType, sender, propertyName, getPropertyValue, onPropertyChange, defaultValue, null)
    {
    }

    public Link(Type propertyType,
                ILinkableNotifyPropertyChanged sender,
                string propertyName,
                Func<ILinkableNotifyPropertyChanged, object> getPropertyValue,
                Action<ILink, object, object> onPropertyChange,
                object defaultValue,
                ILink parentLink)
    {
        _sender = sender.Guard(nameof(sender));
        PropertyType = propertyType.Guard(nameof(propertyType));
        PropertyName = propertyName.Guard(nameof(sender));
        _getPropertyValue = getPropertyValue.Guard(nameof(getPropertyValue));
        _onPropertyChange = onPropertyChange.Guard(nameof(onPropertyChange));
        DefaultValue = defaultValue;
        Id = Guid.NewGuid();
        _parentLink = parentLink;
    }

    public void UnlinkChildren(bool resetPropertyValue)
    {
        if (_childLinks.IsNullOrEmptyCollection())
            return;

        foreach (var key in _childLinks.Keys.ToList())
        {
            if (_childLinks.TryRemove(key, out var link))
                link.Unlink(resetPropertyValue);
        }
    }

    public void Unlink(bool resetPropertyValue)
    {
        try
        {
            _sender.Unlink(this, resetPropertyValue);
        }
        finally
        {
            _isUnlinked = true;
        }
    }

    public void ResetProperty() => OnPropertyChange(_lastValue, DefaultValue);

    public object GetPropertyValue() => _getPropertyValue(_sender);

    public void OnPropertyChange(object oldValue, object newValue)
    {
        if (_isUnlinked)
            throw new InvalidOperationException("You cannot call unlinked link");

        _onPropertyChange(this, oldValue, _lastValue = newValue);
    }

    public void Initialize()
    {
        _parentLink?.AddChild(this);
        OnPropertyChange(DefaultValue, GetPropertyValue());
    }

    public void AddChild(ILink link)
    {
        _childLinks ??= new();

        if (!_childLinks.TryAdd(link.Id, link))
            throw new InvalidOperationException($"This link already has a child of {link.Id} id");
    }

    public void RelinkChildren<T>(T newSender, Action onNewSender) where T : ILinkableNotifyPropertyChanged
    {
        var isNull = newSender is null;
        UnlinkChildren(isNull);

        if (!isNull)
            onNewSender();
    }
}