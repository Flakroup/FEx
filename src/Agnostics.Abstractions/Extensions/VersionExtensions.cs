using System;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class VersionExtensions
{
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

    public static bool FirstIsHigherThanSecond(this Version first, Version second)
    {
        if (first.Major > second.Major)
            return true;

        if (first.Minor > second.Minor)
            return true;

        return first.Build > second.Build || first.Revision > second.Revision;
    }
}