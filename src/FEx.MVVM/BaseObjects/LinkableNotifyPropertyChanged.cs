using FEx.Basics;
using FEx.Extensions.Collections.Dictionaries;
using FEx.MVVM.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FEx.MVVM.BaseObjects;

public abstract class LinkableNotifyPropertyChanged : NotifyPropertyChanged, ILinkableNotifyPropertyChanged
{
    private static void TriggerLinks(IEnumerable<ILink> propertyLinks, object oldValue, object newValue)
    {
        foreach (ILink link in propertyLinks)
            link.OnPropertyChange(oldValue, newValue);
    }

    protected ConcurrentDictionary<string, ConcurrentDictionary<Guid, ILink>> Links { get; }

    protected LinkableNotifyPropertyChanged()
    {
        Links = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, ILink>>();
    }

    public void AddLink(ILink link)
    {
        ConcurrentDictionary<Guid, ILink> links =
            Links.GetOrAddValue(link.PropertyName, () => new ConcurrentDictionary<Guid, ILink>());

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
            FExBasics.Logger.LogError(
                $"There is no link from {propertyType.FullName} to {GetType().FullName} on {propertyName} property of id {linkId}");
            return;
        }

        link.UnlinkChildren(resetProperty);

        if (resetProperty)
            link.ResetProperty();
    }

    protected override void OnPropertySet<T>(T oldValue, T newValue, string propertyName)
    {
        base.OnPropertySet(oldValue, newValue, propertyName);

        if (Links.TryGetValue(propertyName, out ConcurrentDictionary<Guid, ILink> links))
            TriggerLinks(links.Values.ToList(), oldValue, newValue);
    }
}