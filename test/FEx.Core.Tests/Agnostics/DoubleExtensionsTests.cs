using FEx.Agnostics.Abstractions.Extensions.Numericals;
using Shouldly;
using System;
using System.Globalization;
using Xunit;

namespace FEx.Core.Tests.Agnostics;

public sealed class DoubleExtensionsTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1.2.3x")]
    public void ToDouble_InvalidInput_ThrowsFormatException(string value) =>
        Should.Throw<FormatException>(() => value.ToDouble());

    [Theory]
    [InlineData("en-US")]
    [InlineData("pl-PL")]
    public void ToDouble_AcceptsEitherDecimalSeparatorUnderAnyCulture(string cultureName)
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new(cultureName);

        try
        {
            "1.5".ToDouble().ShouldBe(1.5);
            "1,5".ToDouble().ShouldBe(1.5);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
