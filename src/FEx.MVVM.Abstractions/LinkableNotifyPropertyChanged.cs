using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Logging;
using FEx.Agnostics.BaseObjects;
using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FEx.MVVM.Abstractions;

public abstract class LinkableNotifyPropertyChanged : NotifyPropertyChanged, ILinkableNotifyPropertyChanged
{
    protected ConcurrentDictionary<string, ConcurrentDictionary<Guid, ILink>> Links { get; }

    protected LinkableNotifyPropertyChanged()
    {
        Links = new();
    }

    public override void OnPropertySet<T>(T oldValue, T newValue, string propertyName)
    {
        base.OnPropertySet(oldValue, newValue, propertyName);

        if (Links.TryGetValue(propertyName, out ConcurrentDictionary<Guid, ILink> links))
            TriggerLinks(links.Values.ToList(), oldValue, newValue);
    }

    public void AddLink(ILink link)
    {
        ConcurrentDictionary<Guid, ILink> links = Links.GetOrAddValue(link.PropertyName, () => new());

        if (!links.TryAdd(link.Id, link))
            throw new InvalidOperationException($"This {nameof(link)} has already been added");

        link.Initialize();
    }

    public void Unlink(ILink link, bool resetProperty = false) =>
        Unlink(link.Id, link.PropertyName, link.PropertyType, resetProperty);

    public void Unlink(Guid linkId, string propertyName, Type propertyType, bool resetProperty = false)
    {
        if (!Links.TryGetValue(propertyName, out ConcurrentDictionary<Guid, ILink> links)
            || !links.TryRemove(linkId, out ILink link))
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
        foreach (ILink link in propertyLinks)
            link.OnPropertyChange(oldValue, newValue);
    }
}