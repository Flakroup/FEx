using FEx.Core.Abstractions.Models;
using System;
using System.Collections.Generic;

namespace FEx.Core.Abstractions.Interfaces;

public interface INavigationFlowSubject : IFExBehaviorSubject<IReadOnlyList<NavigationFlowChange>>
{
    void OnNext(NavigationFlowChange value);
    void Pop(Type pageType);
    void Push(Type pageType);
}