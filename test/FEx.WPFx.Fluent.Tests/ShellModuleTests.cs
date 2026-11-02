using FEx.WPFx.Fluent.Shell;
using Shouldly;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

public sealed class ShellModuleTests
{
    [Fact]
    public void AnAvailableModule_ShowsItsOwnGlyph_AndItsLabelAsTheTooltip()
    {
        var module = new ShellModule("send", "Group", "Send", "\uE725");

        module.IsAvailable.ShouldBeTrue();
        module.DisplayGlyph.ShouldBe("\uE725");
        module.Tooltip.ShouldBe("Send");
    }

    [Fact]
    public void AModuleInPreparation_IsNotAvailable_ShowsTheClock_AndSaysWhy()
    {
        var module = new ShellModule("iwa", "Soon", "IWA", "\uE725", InPreparation: true);

        module.IsAvailable.ShouldBeFalse();
        module.DisplayGlyph.ShouldBe("\uE121");
        module.Tooltip.ShouldBe("IWA - moduł w przygotowaniu");
    }
}
