using FEx.WPFx.Fluent.Shell;
using Shouldly;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

public sealed class MenuEntryTests
{
    private static readonly ShellModule Send = new("send", "Group", "Send", "\uE725");
    private static readonly ShellModule Preparing = new("soon", "Soon", "Soon", "\uE725", InPreparation: true);

    [Fact]
    public void IsActive_RaisesPropertyChangedOnTheChangeOnly()
    {
        var entry = new MenuEntry(Send);
        var raised = 0;
        entry.PropertyChanged += (_, e) =>
        {
            e.PropertyName.ShouldBe(nameof(MenuEntry.IsActive));
            raised++;
        };

        entry.IsActive = true;
        entry.IsActive = true;
        entry.IsActive = false;

        raised.ShouldBe(2);
    }

    [Fact]
    public void Entry_ExposesWhatTheMenuTemplateBinds()
    {
        var module = Preparing;
        var entry = new MenuEntry(module);

        entry.Label.ShouldBe(module.Label);
        entry.GroupLabel.ShouldBe(module.GroupLabel);
        entry.DisplayGlyph.ShouldBe(module.DisplayGlyph);
        entry.Tooltip.ShouldBe(module.Tooltip);
        entry.IsAvailable.ShouldBeFalse();
        entry.IsActive.ShouldBeFalse();
    }
}
