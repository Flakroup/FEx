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

    /// <summary>The one key outside the data grid: the message under a text-editable date or time field
    /// whose typed text does not parse. Consumers pin their own wording on top of this - an English
    /// "Not a valid date time" under a Polish label is what this entry removes.</summary>
    [Fact]
    public void TheConverterError_ResolvesToPolish()
    {
        var value = _localizer["Converter_InvalidDateTime"];

        value.Value.ShouldBe("Niepoprawna data lub godzina");
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