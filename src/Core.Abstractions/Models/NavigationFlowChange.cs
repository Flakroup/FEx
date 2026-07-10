using FEx.Core.Abstractions.Enums;
using System;

namespace FEx.Core.Abstractions.Models;

public record NavigationFlowChange
{
    public string? SenderType { get; }
    public string SenderName { get; }
    public NavigationFlow Flow { get; }

    public NavigationFlowChange(Type senderType, NavigationFlow flow)
    {
        SenderType = senderType.FullName;
        SenderName = senderType.Name;
        Flow = flow;
    }
}