using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Shouldly;
using System.Globalization;
using Xunit;

namespace FEx.Blazorx.Tests;

/// <summary>
/// What this class answers, and where the answer is judged from.
/// <para>
/// An unknown key does NOT come back English from here - this class returns the key itself with
/// <c>ResourceNotFound</c> set, and MudBlazor's interceptor is what turns that flag into the built-in
/// English resource. The distinction matters: drop the flag and MudBlazor stops falling back, so every
/// unmatched key renders as a raw identifier on screen.
/// </para>
/// </summary>
public sealed class PolishMudLocalizerTests
{
    private readonly PolishMudLocalizer _localizer = new();

    [Fact]
    public void KnownKey_ResolvesToThePolishTranslation()
    {
        var value = _localizer["MudDataGrid_Filter"];

        value.Value.ShouldBe("Filtr");
        value.ResourceNotFound.ShouldBeFalse();
    }

    /// <summary>
    /// Keys exactly as MudBlazor 9.10.0 requests them (read from its embedded resources), resolved through its own
    /// interceptor under a Polish UI culture. Three differ from a plain dot-to-underscore rewrite: Equals,
    /// IsEmpty and IsNotEmpty.
    /// </summary>
    [Theory]
    [InlineData("MudDataGrid_Filter", "Filtr")]
    [InlineData("MudDataGrid_AddFilter", "Dodaj filtr")]
    [InlineData("MudDataGrid_Contains", "zawiera")]
    [InlineData("MudDataGrid_Equals", "równe")]
    [InlineData("MudDataGrid_NotEquals", "różne od")]
    [InlineData("MudDataGrid_IsEmpty", "jest puste")]
    [InlineData("MudDataGrid_IsNotEmpty", "nie jest puste")]
    [InlineData("MudDataGridPager_RowsPerPage", "Wierszy na stronie")]
    [InlineData("MudTablePager_NextPage", "Następna strona")]
    public void TheRealMudBlazorKeys_ReachMudBlazorsOwnResolution_InPolish(string key, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pl-PL");
        try
        {
            DefaultLocalizationInterceptor interceptor = new(NullLoggerFactory.Instance, _localizer);

            interceptor.Handle(key).Value.ShouldBe(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    /// <summary>
    /// The converter error, asserted through MudBlazor rather than through this dictionary.
    /// <para>
    /// Indexing the localizer directly proves only that the dictionary contains what the same file wrote
    /// three lines earlier - it passes for any key string whatsoever, including one MudBlazor never asks
    /// for. That is not hypothetical here: the <c>MudDataGrid.*</c> entries are spelled with a dot, are
    /// never requested, and this suite stayed green through all of it.
    /// </para>
    /// <para>
    /// So this goes through <see cref="DefaultLocalizationInterceptor"/>, which is the component MudBlazor
    /// actually resolves keys with. The culture matters and is set deliberately: <c>Handle</c> skips a
    /// registered <see cref="MudLocalizer"/> entirely when the UI culture's parent is English, so a test
    /// left on the runner's default would return English for everything and prove nothing.
    /// </para>
    /// </summary>
    [Fact]
    public void TheConverterError_ReachesMudBlazorsOwnResolution_InPolish()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pl-PL");
        try
        {
            DefaultLocalizationInterceptor interceptor = new(NullLoggerFactory.Instance, _localizer);

            interceptor.Handle("Converter_InvalidDateTime").Value.ShouldBe("Niepoprawna data lub godzina");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    /// <summary>An English UI culture bypasses the custom localizer altogether, so the same key answers in
    /// English - stated here because it is the difference between "the entry is missing" and "the entry is
    /// there and the culture turned it off", and the two look identical from the screen.</summary>
    [Fact]
    public void UnderAnEnglishUiCulture_MudBlazorSkipsThisLocalizerEntirely()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");
        try
        {
            DefaultLocalizationInterceptor interceptor = new(NullLoggerFactory.Instance, _localizer);

            interceptor.Handle("Converter_InvalidDateTime").Value.ShouldNotBe("Niepoprawna data lub godzina");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void UnknownKey_FallsThroughToTheKeyItself_AndIsFlaggedNotFound()
    {
        var value = _localizer["MudDataGrid_SomeFutureKey"];

        value.Value.ShouldBe("MudDataGrid_SomeFutureKey");
        value.ResourceNotFound.ShouldBeTrue();
    }
}