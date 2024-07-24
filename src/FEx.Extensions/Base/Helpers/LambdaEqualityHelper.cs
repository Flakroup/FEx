using System;
using System.Linq;

namespace FEx.Extensions.Base.Helpers;

public class LambdaEqualityHelper<T>
{
    private readonly Func<T, object>[] _equalityContributorAccessors;

    public LambdaEqualityHelper(params Func<T, object>[] equalityContributorAccessors)
    {
        _equalityContributorAccessors = equalityContributorAccessors;
    }

    public bool Equals(T instance, T other)
    {
        if (instance is null ^ other is null)
            return false;

        if (ReferenceEquals(instance, other))
            return true;

        return instance.GetType() == other.GetType()
               && _equalityContributorAccessors.All(accessor => Equals(accessor(instance), accessor(other)));
    }

    public int GetHashCode(T instance)
    {
        int hashCode = GetType().GetHashCode();

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