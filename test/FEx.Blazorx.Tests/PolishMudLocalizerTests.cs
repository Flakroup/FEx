using Shouldly;
using Xunit;

namespace FEx.Blazorx.Tests;

/// <summary>Known MudBlazor keys resolve to Polish; unknown keys fall through to the English default.</summary>
public sealed class PolishMudLocalizerTests
{
    private readonly PolishMudLocalizer _localizer = new();

    [Fact]
    public void KnownKey_ResolvesToThePolishTranslation()
    {
        var value = _localizer["MudDataGrid.Filter"];

        value.Value.ShouldBe("Filtr");
        value.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void UnknownKey_FallsThroughToTheKeyItself_AndIsFlaggedNotFound()
    {
        var value = _localizer["MudDataGrid.SomeFutureKey"];

        value.Value.ShouldBe("MudDataGrid.SomeFutureKey");
        value.ResourceNotFound.ShouldBeTrue();
    }
}