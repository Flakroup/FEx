using System;
using System.Linq;

namespace FEx.Legacy.Asyncx.Enums;

[Flags]
public enum JobSpecs
{
    None = 0,
    ShowTimeInfoAfterMain = 1,
    RunPreAndPostMain = 1 << 1,
    InformUserOnExceptionInMain = 1 << 2,
    Default = ShowTimeInfoAfterMain | RunPreAndPostMain | InformUserOnExceptionInMain
}

public static class JobSpecsExtensions
{
    public static bool HasFlagsFast(this JobSpecs? value, params JobSpecs[] flags) =>
        flags.All(flag => value.HasFlagFast(flag));

    public static bool HasFlagFast(this JobSpecs? value, JobSpecs flag)
    {
        value ??= JobSpecs.Default;

        return (value & flag) != 0;
    }
}