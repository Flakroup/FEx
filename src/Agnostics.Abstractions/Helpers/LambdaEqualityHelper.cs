using System;
using System.Linq;

namespace FEx.Agnostics.Abstractions.Helpers;

public class LambdaEqualityHelper<T>
{
    private readonly Func<T, object>[] _equalityContributorAccessors;

    public LambdaEqualityHelper(params Func<T, object>[] equalityContributorAccessors)
    {
        _equalityContributorAccessors = equalityContributorAccessors;
    }

    public bool Equals(T? instance, T? other)
    {
        if (instance is null ^ other is null)
            return false;

        if (ReferenceEquals(instance, other))
            return true;

        // Both are non-null here: the checks above cover the differing-null and both-null cases.
        return instance!.GetType() == other!.GetType()
               && _equalityContributorAccessors.All(accessor => Equals(accessor(instance), accessor(other)));
    }

    public int GetHashCode(T instance)
    {
        var hashCode = GetType().GetHashCode();

        unchecked
        {
            hashCode = _equalityContributorAccessors.Select(accessor => accessor(instance))
                .Aggregate(hashCode,
                    (current, item) => current * 397
                                       ^ (item != null
                                           ? item.GetHashCode()
                                           : 0));
        }

        return hashCode;
    }
}