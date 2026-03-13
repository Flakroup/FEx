using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Logging;
using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FEx.MVVM.Rx.BaseObjects;

public abstract class LinkableReactiveNotifyPropertyChanged : ReactiveNotifyPropertyChanged,
    ILinkableNotifyPropertyChanged
{
    protected ConcurrentDictionary<string, ConcurrentDictionary<Guid, ILink>> Links { get; }

    protected LinkableReactiveNotifyPropertyChanged()
    {
        Links = new();
    }

    public override void OnPropertySet<T>(T oldValue, T newValue, string propertyName)
    {
        base.OnPropertySet(oldValue, newValue, propertyName);

        if (Links.TryGetValue(propertyName, out var links))
            TriggerLinks(links.Values.ToList(), oldValue, newValue);
    }

    public void AddLink(ILink link)
    {
        var links = Links.GetOrAddValue(link.PropertyName, () => new());

        if (!links.TryAdd(link.Id, link))
            throw new InvalidOperationException($"This {nameof(link)} has already been added");

        link.Initialize();
    }

    public void Unlink(ILink link, bool resetProperty = false) =>
        Unlink(link.Id, link.PropertyName, link.PropertyType, resetProperty);

    public void Unlink(Guid linkId, string propertyName, Type propertyType, bool resetProperty = false)
    {
        if (!Links.TryGetValue(propertyName, out var links)
            || !links.TryRemove(linkId, out var link))
        {
            FExStaticLogger.Error(
                $"There is no link from {propertyType.FullName} to {GetType().FullName} on {propertyName} property of id {linkId}");

            return;
        }

        link.UnlinkChildren(resetProperty);

        if (resetProperty)
            link.ResetProperty();
    }

    private static void TriggerLinks(IEnumerable<ILink> propertyLinks, object oldValue, object newValue)
    {
        foreach (var link in propertyLinks)
            link.OnPropertyChange(oldValue, newValue);
    }
}