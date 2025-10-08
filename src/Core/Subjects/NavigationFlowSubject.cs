using FEx.Agnostics.Collections.Concurrent;
using FEx.Core.Abstractions.Enums;
using FEx.Core.Abstractions.Interfaces;
using FEx.Core.Abstractions.Models;
using FEx.Core.Abstractions.Subjects;
using System;
using System.Collections.Generic;

namespace FEx.Core.Subjects;

public class NavigationFlowSubject : FExBehaviorSubject<IReadOnlyList<NavigationFlowChange>>, INavigationFlowSubject
{
    private readonly ConcurrentList<NavigationFlowChange> _data;

    public NavigationFlowSubject()
        : base(new List<NavigationFlowChange>().AsReadOnly())
    {
        _data = [];
    }

    public void OnNext(NavigationFlowChange value)
    {
        _data.Add(value);
        OnNext(_data.AsReadOnly());
    }

    public void Pop(Type pageType) => OnNext(new(pageType, NavigationFlow.Pop));

    public void Push(Type pageType) => OnNext(new(pageType, NavigationFlow.Push));
}