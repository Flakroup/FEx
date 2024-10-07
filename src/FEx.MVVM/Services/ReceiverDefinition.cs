using FEx.Extensions.Collections.Lists;
using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Collections.Generic;

namespace FEx.MVVM.Services;

public class ReceiverDefinition : IEquatable<ReceiverDefinition>
{
    public IProgressAggregator Container { get; }

    public IList<string> PropertyNames { get; }

    public ReceiverDefinition(IProgressAggregator container, params string[] propertyNames)
    {
        Container = container;

        PropertyNames = propertyNames.IsNotNullOrEmptyList()
            ? propertyNames
            : null;
    }

    public bool Equals(ReceiverDefinition other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other) || Equals(Container, other.Container);
    }

    public static bool operator ==(ReceiverDefinition left, ReceiverDefinition right) => Equals(left, right);

    public static bool operator !=(ReceiverDefinition left, ReceiverDefinition right) => !Equals(left, right);

    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == GetType() && Equals((ReceiverDefinition)obj);
    }

    public override int GetHashCode() =>
        Container is not null
            ? Container.GetHashCode()
            : 0;

    public bool IsReceivingThisProperty(string propertyName) => PropertyNames?.Contains(propertyName) != false;
}