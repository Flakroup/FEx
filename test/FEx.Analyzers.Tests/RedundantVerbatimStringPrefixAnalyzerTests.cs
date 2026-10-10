using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantVerbatimStringPrefixAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantVerbatimStringPrefixAnalyzer>(source);

    [Fact]
    public Task Reports_a_plain_verbatim_literal() =>
        VerifyAsync("""
            class C
            {
                const string A = {|FEX0007:@|}"abc";
                string B = {|FEX0007:@|}"";
                string D = {|FEX0007:@|}"it's {{braces}} and ~ and DEL";
                byte[] U = {|FEX0007:@|}"abc"u8.ToArray();
                void M(string s) { switch (s) { case {|FEX0007:@|}"x": break; } }
            }
            """);

    [Fact]
    public Task Reports_a_plain_verbatim_interpolated_string_in_either_prefix_order() =>
        VerifyAsync("""
            class C
            {
                string A(int x) => ${|FEX0007:@|}"a {x} b";
                string B(int x) => {|FEX0007:@|}$"a {x} b";
                string D(System.DateTime d) => ${|FEX0007:@|}"{d:HH}";
                string E(int x) => ${|FEX0007:@|}"{x,5}";
            }
            """);

    [Fact]
    public Task Ignores_what_is_in_the_holes_of_an_interpolated_string() =>
        VerifyAsync("""
            class C
            {
                string A(int x) => ${|FEX0007:@|}"{(x > 0 ? "a" : "b")}";
                string B(int x) => ${|FEX0007:@|}"{x /* "quoted" \ */}";
            }
            """);

    [Fact]
    public Task Reports_nested_interpolated_strings_separately() =>
        VerifyAsync("""
            class C
            {
                string A(int x) => ${|FEX0007:@|}"a{${|FEX0007:@|}"b{x}"}";
                string B(int x) => ${|FEX0007:@|}"{x} {{|FEX0007:@|}"q"}";
            }
            """);

    [Theory]
    [InlineData("a\\\\b")]
    [InlineData("say \"\"hi\"\"")]
    [InlineData("a\tb")]
    [InlineData("a\nb")]
    [InlineData("a\r\nb")]
    [InlineData("a\u2028b")]
    [InlineData("a\u0085b")]
    [InlineData("a\u00a0b")]
    [InlineData("a\u200bb")]
    [InlineData("a\ufeffb")]
    [InlineData("a\u00adb")]
    [InlineData("a\ud83d\ude00b")]
    [InlineData("\u2211")]
    [InlineData("\u20ac")]
    [InlineData("\u00b0")]
    [InlineData("\u00b2")]
    [InlineData("\u2160")]
    [InlineData("e\u0301")]
    [InlineData("\u2192")]
    [InlineData("\ue000")]
    [InlineData("\u0001")]
    public Task Keeps_the_prefix_when_the_text_needs_it(string text) =>
        VerifyAsync("class C { string S = @\"" + text + "\"; string T(int x) => $@\"" + text + "{x}\"; }");

    [Theory]
    [InlineData("\u00e9")]
    [InlineData("\u0661")]
    [InlineData("\u00ab")]
    [InlineData("\u2013")]
    [InlineData("\u4e2d")]
    public Task Reports_a_prefix_on_letters_digits_and_punctuation_beyond_ascii(string text) =>
        VerifyAsync("class C { string S = {|FEX0007:@|}\"" + text + "\"; }");

    [Fact]
    public Task Keeps_the_prefix_when_a_format_clause_needs_it() =>
        VerifyAsync(""""
            class C
            {
                string A(System.TimeSpan d) => $@"{d:hh\:mm}";
                string B(int x) => $@"\{x}";
                string D(int x) => $@"say ""{x}""";
                string E(System.DateTime d) => $@"{d:yyyy""x}";
                string F(int x) => $@"{x}\";
                byte[] G() => @"a\b"u8.ToArray();
            }
            """");

    [Fact]
    public Task Ignores_strings_without_the_prefix() =>
        VerifyAsync("""
            class C { string A = "abc"; string B(int x) => $"{x}"; string D = "x"; }
            """);
}
