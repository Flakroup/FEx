using FEx.Agnostics.Utilities;
using Shouldly;
using Xunit;

namespace FEx.Core.Tests.Agnostics;

public sealed class InterlockedBoolTests
{
    [Fact]
    public void EqualityOperator_ComparesValuesOfTwoInstances()
    {
        var a = new InterlockedBool(true);
        var b = new InterlockedBool(true);

        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_DetectsDifferentValues()
    {
        var a = new InterlockedBool(true);
        var b = new InterlockedBool(false);

        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_HandlesNull()
    {
        InterlockedBool? none = null;
        var a = new InterlockedBool(true);

        (none == null).ShouldBeTrue();
        (a == null).ShouldBeFalse();
        (a != null).ShouldBeTrue();
        (none == a).ShouldBeFalse();
    }
}
