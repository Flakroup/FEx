using System;

namespace FEx.Analyzers.Tests.Fakes;

/// <summary>A throw helper that lives in a library, referenced by analyzer test sources: neither the framework's nor in source.</summary>
public static class OutsideGuard
{
    public static void ThrowIfBad(int value)
    {
        if (value < 0)
            throw new InvalidOperationException();
    }
}
