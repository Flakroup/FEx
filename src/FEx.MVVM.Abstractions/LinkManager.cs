using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Logging;
using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FEx.MVVM.Abstractions;

public class LinkManager
{
    private readonly Func<Type> _ownerTypeProvider;

    public ConcurrentDictionary<string, ConcurrentDictionary<Guid, ILink>> Links { get; }

    public LinkManager(Func<Type> ownerTypeProvider)
    {
        _ownerTypeProvider = ownerTypeProvider;
        Links = new();
    }

    public void OnPropertySet<T>(T oldValue, T newValue, string propertyName)
    {
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

    public void Unlink(ILink link, bool resetProperty) =>
        Unlink(link.Id, link.PropertyName, link.PropertyType, resetProperty);

    public void Unlink(Guid linkId, string propertyName, Type propertyType, bool resetProperty)
    {
        if (!Links.TryGetValue(propertyName, out var links)
            || !links.TryRemove(linkId, out var link))
        {
            FExStaticLogger.Error(
                $"There is no link from {propertyType.FullName} to {_ownerTypeProvider().FullName} on {propertyName} property of id {linkId}");

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