using FEx.Agnostics.Utilities;
using Shouldly;
using Xunit;

namespace FEx.Core.Tests.Agnostics;

public sealed class FlakDynamicObjectTests
{
    [Fact]
    public void IntIndexerSetter_PadsMoreThanOneSlot()
    {
        var obj = new FlakDynamicObject();

        Should.NotThrow(() => obj[5] = "value");

        obj[5].ShouldBe("value");
        obj[0].ShouldBeNull();
        obj[4].ShouldBeNull();
    }

    [Fact]
    public void IntIndexerSetter_CanPadRepeatedly()
    {
        var obj = new FlakDynamicObject();

        obj[2] = "a";
        obj[6] = "b";

        obj[2].ShouldBe("a");
        obj[6].ShouldBe("b");
    }
}
