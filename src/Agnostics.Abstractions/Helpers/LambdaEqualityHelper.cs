using System;
using System.Linq;

namespace FEx.Agnostics.Abstractions.Helpers;

/// <summary>Implements value equality and hashing from a set of accessor delegates.</summary>
/// <typeparam name="T">The compared type.</typeparam>
public class LambdaEqualityHelper<T>
{
    private readonly Func<T, object>[] _equalityContributorAccessors;

    /// <summary>Initializes the helper.</summary>
    /// <param name="equalityContributorAccessors">Delegates that return the values that take part in equality.</param>
    public LambdaEqualityHelper(params Func<T, object>[] equalityContributorAccessors)
    {
        _equalityContributorAccessors = equalityContributorAccessors;
    }

    /// <summary>Determines whether two instances are equal.</summary>
    /// <param name="instance">The first instance.</param>
    /// <param name="other">The second instance.</param>
    /// <returns><c>true</c> if both are null, or both are of the same type and all contributing values are equal.</returns>
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

    /// <summary>Computes a hash code from the contributing values.</summary>
    /// <param name="instance">The instance to hash.</param>
    /// <returns>The combined hash code.</returns>
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