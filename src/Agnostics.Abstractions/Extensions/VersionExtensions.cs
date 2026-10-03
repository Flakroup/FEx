using System;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for comparing versions.</summary>
public static class VersionExtensions
{
    /// <summary>Determines whether two versions differ in any component that is defined in the first version.</summary>
    /// <param name="first">The version whose defined components are compared.</param>
    /// <param name="second">The version to compare with.</param>
    /// <returns><c>true</c> if a defined component of <paramref name="first" /> differs from <paramref name="second" />.</returns>
    public static bool FirstIsOtherThanSecond(this Version first, Version second)
    {
        if (first.Major > -1
            && first.Major != second.Major)
            return true;

        if (first.Minor > -1
            && first.Minor != second.Minor)
            return true;

        return first.Build > -1 && first.Build != second.Build
               || first.Revision > -1 && first.Revision != second.Revision;
    }

    /// <summary>Determines whether the first version is higher in any component, compared component by component without regard to more significant ones.</summary>
    /// <param name="first">The first version.</param>
    /// <param name="second">The second version.</param>
    /// <returns><c>true</c> if the major, minor, build or revision of <paramref name="first" /> exceeds that of <paramref name="second" />.</returns>
    public static bool FirstIsHigherThanSecond(this Version first, Version second)
    {
        if (first.Major > second.Major)
            return true;

        if (first.Minor > second.Minor)
            return true;

        return first.Build > second.Build || first.Revision > second.Revision;
    }
}