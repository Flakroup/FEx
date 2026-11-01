using FEx.WPFx.Fluent.Shell;
using Shouldly;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

public sealed class ShellBreadcrumbTests
{
    private static readonly ShellModule Send = new("send", "Rozliczenie miesiąca", "3. Wysyłka do ZUS", "\uE725");
    private static readonly ShellModule Settings = new("settings", string.Empty, "Ustawienia", "\uE713");

    [Fact]
    public void Full_IsAppGroupModule() =>
        ShellBreadcrumb.Segments("Deklarant", Send, compact: false)
            .ShouldBe(["Deklarant", "Rozliczenie miesiąca", "3. Wysyłka do ZUS"]);

    [Fact]
    public void TheAppNameIsTheAppsOwn() =>
        ShellBreadcrumb.Segments("KaperCheck", Send, compact: false)[0].ShouldBe("KaperCheck");

    [Fact]
    public void Compact_IsTheModuleNameOnly() =>
        ShellBreadcrumb.Segments("Deklarant", Send, compact: true).ShouldBe(["3. Wysyłka do ZUS"]);

    [Fact]
    public void SettingsEntry_HasNoGroupSegment() =>
        ShellBreadcrumb.Segments("Deklarant", Settings, compact: false).ShouldBe(["Deklarant", "Ustawienia"]);
}
