using FEx.Agnostics.Abstractions.Utilities;
using Shouldly;
using Xunit;

namespace FEx.Agnostics.Abstractions.Tests;

public sealed class PolishPluralTests
{
    [Theory]
    [InlineData(0, "plików")]
    [InlineData(1, "plik")]
    [InlineData(2, "pliki")]
    [InlineData(4, "pliki")]
    [InlineData(5, "plików")]
    [InlineData(12, "plików")]
    [InlineData(14, "plików")]
    [InlineData(21, "plików")]
    [InlineData(22, "pliki")]
    [InlineData(25, "plików")]
    [InlineData(101, "plików")]
    [InlineData(112, "plików")]
    [InlineData(122, "pliki")]
    [InlineData(-1, "plików")] // only +1 is singular
    [InlineData(-3, "pliki")]
    public void Pick_FollowsThePolishRuleForACount(int count, string expected) =>
        PolishPlural.Pick(count, "plik", "pliki", "plików").ShouldBe(expected);
}
