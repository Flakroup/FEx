using FEx.Agnostics.Abstractions.Extensions;
using Shouldly;
using Xunit;

namespace FEx.Core.Tests.Agnostics;

public sealed class StringExtensionsPhoneNumberTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void ToFormattedPhoneNumber_NullEmptyOrWhiteSpace_ReturnsNull(string? value) =>
        value!.ToFormattedPhoneNumber().ShouldBeNull();

    [Theory]
    [InlineData("+12345", "2345")]
    [InlineData("+48 123 456", "8123456")]
    public void ToFormattedPhoneNumber_PlusPrefix_SkipsTheFirstTwoCharacters(string value, string expected) =>
        value.ToFormattedPhoneNumber().ShouldBe(expected);

    [Theory]
    [InlineData("(123) 456-789", "123456789")]
    [InlineData("tel: 12ab34", "1234")]
    [InlineData("1.2-3", "1.23")]
    public void ToFormattedPhoneNumber_NonDigitNonDotCharacters_AreRemoved(string value, string expected) =>
        value.ToFormattedPhoneNumber().ShouldBe(expected);
}
